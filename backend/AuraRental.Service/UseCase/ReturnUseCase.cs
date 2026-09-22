using System.Text.Json;
using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.DTOs.Return;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.Utils;

namespace AuraRental.Service.UseCase;

public sealed class ReturnUseCase(
    IRentalRepository rentalRepository,
    IReturnRepository returnRepository,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext) : IReturnUseCase
{
    public async Task<IReadOnlyList<ReturnQueueItemDto>> GetQueue(
        string? status,
        int limit,
        CancellationToken cancellationToken)
    {
        ValidateLimit(limit);
        var requestedStatuses = string.IsNullOrWhiteSpace(status)
            ? []
            : status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => value.ToUpperInvariant())
                .ToList();
        var allowed = new[] { "INSPECTING", "WAITING_APPROVAL", "DRAFT", "APPROVED" };
        if (requestedStatuses.Any(value => !allowed.Contains(value)))
        {
            throw new ValidationException("INVALID_RETURN_STATUS", "Trạng thái hàng trả không hợp lệ.");
        }

        var orders = await returnRepository.GetQueue(
            requestContext.BranchId,
            [OrderStatus.Inspecting],
            limit,
            cancellationToken);
        var result = orders.Select(order =>
        {
            var latestRefund = order.Refunds.OrderByDescending(refund => refund.Version).FirstOrDefault();
            return new ReturnQueueItemDto(
                order.Id,
                order.OrderNo,
                order.CustomerName,
                order.ReturnedAt,
                order.Items.Count,
                order.Items.Count(item => item.Condition.HasValue),
                order.Items.Count,
                latestRefund is null ? null : ApiText.EnumValue(latestRefund.Status));
        });

        if (requestedStatuses.Count > 0)
        {
            result = result.Where(item =>
                requestedStatuses.Contains("INSPECTING") ||
                (requestedStatuses.Contains("WAITING_APPROVAL") && item.RefundStatus == "SUBMITTED") ||
                (item.RefundStatus is not null && requestedStatuses.Contains(item.RefundStatus)));
        }

        return result.ToList();
    }

    public async Task<InspectionDto> InspectItem(
        Guid orderId,
        Guid orderItemId,
        InspectOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        ValidateInspection(request);
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockOrder(orderId, cancellationToken)
            ?? throw new NotFoundException("ORDER_NOT_FOUND", "Không tìm thấy order.");
        var order = await GetOrderRequired(orderId, true, cancellationToken);
        if (order.Status != OrderStatus.Inspecting)
        {
            throw new ConflictException("ORDER_NOT_INSPECTING", "Order chưa ở bước kiểm tra hàng trả.");
        }

        var item = order.Items.FirstOrDefault(candidate => candidate.Id == orderItemId)
            ?? throw new NotFoundException("ORDER_ITEM_NOT_FOUND", "Không tìm thấy item trong order.");
        var condition = RentalRules.ParseEnum<ItemCondition>(
            request.Condition,
            "INVALID_ITEM_CONDITION",
            "Tình trạng item không hợp lệ.");
        var outcome = RentalRules.ParseEnum<InventoryStatus>(
            request.InventoryOutcome,
            "INVALID_INVENTORY_OUTCOME",
            "Trạng thái kho sau kiểm tra không hợp lệ.");
        ValidateInspectionByCondition(condition, outcome, request);

        item.Condition = condition;
        item.ActualRentalFee = request.ActualRentalFee;
        item.ProcessingFee = condition == ItemCondition.Missing && request.ProcessingFee == 0
            ? item.ReplacementValue
            : request.ProcessingFee;
        item.DamageNote = string.IsNullOrWhiteSpace(request.DamageNote) ? null : request.DamageNote.Trim();
        item.DamagePhotoPaths = request.DamagePhotoPaths.Select(path => path.Trim()).Where(path => path.Length > 0).Distinct().ToArray();
        item.InventoryItem.Status = outcome;
        if (outcome != InventoryStatus.Usable)
        {
            item.InventoryItem.CleaningUntil = null;
        }

        var draft = order.Refunds.FirstOrDefault(refund => refund.Status == RefundStatus.Draft);
        if (draft is not null)
        {
            RefreshRefund(draft, order);
        }

        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        var calculation = Calculate(order);
        return new InspectionDto(
            item.Id,
            ApiText.EnumValue(condition),
            item.ProcessingFee,
            ApiText.EnumValue(outcome),
            calculation);
    }

    public async Task<RefundDto> CreateOrUpdateRefund(
        Guid orderId,
        CreateRefundRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockOrder(orderId, cancellationToken)
            ?? throw new NotFoundException("ORDER_NOT_FOUND", "Không tìm thấy order.");
        var order = await GetOrderRequired(orderId, true, cancellationToken);
        EnsureRefundCanBeCalculated(order);

        var refund = await returnRepository.GetOpenRefundForOrder(order.Id, true, cancellationToken);
        if (refund is not null && refund.Status != RefundStatus.Draft)
        {
            throw new ConflictException("REFUND_ALREADY_SUBMITTED", "Phiếu đã gửi duyệt; cần trả về kiểm tra trước khi sửa.");
        }

        if (refund is null)
        {
            refund = new Refund
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Version = await returnRepository.GetLatestVersion(order.Id, cancellationToken) + 1,
                Status = RefundStatus.Draft,
                CreatedBy = requestContext.UserId,
                CreatedAt = DateTimeOffset.UtcNow,
                ItemsSnapshot = JsonSerializer.SerializeToDocument(Array.Empty<object>())
            };
            returnRepository.AddRefund(refund);
            order.Refunds.Add(refund);
        }

        refund.AdjustmentReason = string.IsNullOrWhiteSpace(request.AdjustmentReason)
            ? refund.AdjustmentReason
            : request.AdjustmentReason.Trim();
        RefreshRefund(refund, order);
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        return ToRefundDto(refund);
    }

    public Task<RefundDto> SubmitRefund(Guid refundId, CancellationToken cancellationToken) =>
        ChangeRefundStatus(
            refundId,
            RefundStatus.Draft,
            refund =>
            {
                refund.Status = RefundStatus.Submitted;
                refund.SubmittedBy = requestContext.UserId;
            },
            cancellationToken);

    public async Task<RefundDto> ReturnForReview(
        Guid refundId,
        ReturnForReviewRequest request,
        CancellationToken cancellationToken)
    {
        EnsureManager();
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException("REVIEW_REASON_REQUIRED", "Cần nhập lý do trả về kiểm tra.");
        }

        return await ChangeRefundStatus(
            refundId,
            RefundStatus.Submitted,
            refund =>
            {
                refund.Status = RefundStatus.Draft;
                refund.AdjustmentReason = $"REVIEW: {request.Reason.Trim()}";
            },
            cancellationToken);
    }

    public async Task<ApproveRefundDto> Approve(
        Guid refundId,
        ApproveRefundRequest request,
        CancellationToken cancellationToken)
    {
        EnsureManager();
        var initial = await GetRefundRequired(refundId, false, cancellationToken);
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockOrder(initial.OrderId, cancellationToken);
        _ = await returnRepository.LockRefund(refundId, cancellationToken);
        var refund = await GetRefundRequired(refundId, true, cancellationToken);
        if (refund.Version != request.ExpectedVersion)
        {
            throw new ConflictException("REFUND_VERSION_CONFLICT", "Phiếu đã thay đổi; vui lòng tải lại dữ liệu.");
        }

        if (refund.Status is not (RefundStatus.Draft or RefundStatus.Submitted))
        {
            throw new ConflictException("REFUND_NOT_APPROVABLE", "Phiếu hiện tại không thể duyệt.");
        }

        EnsureRefundCanBeCalculated(refund.Order);
        RefreshRefund(refund, refund.Order);
        var previousApproved = await returnRepository.GetApprovedRefunds(refund.OrderId, cancellationToken);
        foreach (var previous in previousApproved.Where(item => item.Id != refund.Id))
        {
            previous.Status = RefundStatus.Superseded;
        }

        refund.Status = RefundStatus.Approved;
        refund.ApprovedBy = requestContext.UserId;
        refund.ApprovedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);

        return new ApproveRefundDto(
            refund.Id,
            refund.Version,
            ApiText.EnumValue(refund.Status),
            refund.RefundAmount,
            AdditionalCollection(refund),
            requestContext.UserId,
            refund.ApprovedAt.Value,
            false);
    }

    public async Task<RefundDto> CreateRevision(
        Guid refundId,
        CreateRefundRevisionRequest request,
        CancellationToken cancellationToken)
    {
        EnsureManager();
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException("REVISION_REASON_REQUIRED", "Cần nhập lý do điều chỉnh.");
        }

        var approved = await GetRefundRequired(refundId, false, cancellationToken);
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockOrder(approved.OrderId, cancellationToken);
        _ = await returnRepository.LockRefund(refundId, cancellationToken);
        approved = await GetRefundRequired(refundId, true, cancellationToken);
        if (approved.Status != RefundStatus.Approved || approved.Order.SettledAt.HasValue)
        {
            throw new ConflictException("REFUND_REVISION_NOT_ALLOWED", "Chỉ điều chỉnh phiếu đã duyệt nhưng chưa đối soát.");
        }

        if (await returnRepository.HasNewerOpenRevision(approved.OrderId, approved.Version, cancellationToken))
        {
            throw new ConflictException("REFUND_REVISION_EXISTS", "Đã có một phiên bản điều chỉnh đang mở.");
        }

        var revision = new Refund
        {
            Id = Guid.NewGuid(),
            OrderId = approved.OrderId,
            Version = await returnRepository.GetLatestVersion(approved.OrderId, cancellationToken) + 1,
            Status = RefundStatus.Draft,
            DepositAmount = approved.DepositAmount,
            RentalFee = approved.RentalFee,
            ProcessingFee = approved.ProcessingFee,
            RefundAmount = approved.RefundAmount,
            ItemsSnapshot = JsonDocument.Parse(approved.ItemsSnapshot.RootElement.GetRawText()),
            AdjustmentReason = request.Reason.Trim(),
            CreatedBy = requestContext.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        returnRepository.AddRefund(revision);
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        return ToRefundDto(revision);
    }

    public async Task<SettleRefundDto> Settle(
        Guid refundId,
        SettleRefundRequest request,
        CancellationToken cancellationToken)
    {
        EnsureManager();
        var initial = await GetRefundRequired(refundId, false, cancellationToken);
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockOrder(initial.OrderId, cancellationToken);
        _ = await returnRepository.LockRefund(refundId, cancellationToken);
        var refund = await GetRefundRequired(refundId, true, cancellationToken);
        if (refund.Status != RefundStatus.Approved || refund.Order.SettledAt.HasValue)
        {
            throw new ConflictException("REFUND_NOT_SETTLEABLE", "Phiếu chưa được duyệt hoặc order đã đối soát.");
        }

        if (await returnRepository.HasNewerOpenRevision(refund.OrderId, refund.Version, cancellationToken))
        {
            throw new ConflictException("REFUND_REVISION_OPEN", "Không thể đối soát khi còn phiên bản điều chỉnh đang mở.");
        }

        var additionalCollection = AdditionalCollection(refund);
        if (additionalCollection > 0)
        {
            var collected = refund.Order.Reservation.Payments
                .Where(payment => payment.Type == PaymentType.AdditionalCollection && payment.Status == PaymentStatus.Confirmed)
                .Sum(payment => payment.Amount);
            if (collected < additionalCollection)
            {
                throw new ConflictException("ADDITIONAL_COLLECTION_REQUIRED", $"Cần thu thêm {additionalCollection - collected:N0} VND.");
            }
        }

        Payment? payment = null;
        if (refund.RefundAmount > 0)
        {
            if (string.IsNullOrWhiteSpace(request.Method))
            {
                throw new ValidationException("PAYMENT_METHOD_REQUIRED", "Phương thức hoàn tiền là bắt buộc.");
            }

            payment = new Payment
            {
                Id = Guid.NewGuid(),
                ReservationId = refund.Order.ReservationId,
                RefundId = refund.Id,
                Type = PaymentType.Refund,
                Amount = refund.RefundAmount,
                Method = request.Method.Trim(),
                Status = PaymentStatus.Confirmed,
                TransactionRef = request.TransactionRef?.Trim(),
                ProofPath = request.ProofPath?.Trim(),
                RecordedBy = requestContext.UserId,
                ConfirmedBy = requestContext.UserId,
                PaidAt = request.PaidAt.ToUniversalTime()
            };
            rentalRepository.AddPayment(payment);
        }

        var settledAt = DateTimeOffset.UtcNow;
        refund.Order.SettledBy = requestContext.UserId;
        refund.Order.SettledAt = settledAt;
        refund.Order.Status = OrderStatus.Completed;
        foreach (var item in refund.Order.Items)
        {
            switch (item.Condition)
            {
                case ItemCondition.Good:
                    item.InventoryItem.Status = InventoryStatus.Usable;
                    item.InventoryItem.CleaningUntil = settledAt.AddHours(item.InventoryItem.CleaningHours);
                    break;
                case ItemCondition.Damaged:
                    item.InventoryItem.Status = InventoryStatus.Maintenance;
                    item.InventoryItem.CleaningUntil = null;
                    break;
                case ItemCondition.Missing:
                    item.InventoryItem.Status = InventoryStatus.Lost;
                    item.InventoryItem.CleaningUntil = null;
                    break;
            }
        }

        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        var settlementType = refund.RefundAmount > 0 ? "REFUND" : additionalCollection > 0 ? "ADDITIONAL_COLLECTION" : "ZERO";
        return new SettleRefundDto(
            settlementType,
            refund.RefundAmount > 0 ? refund.RefundAmount : additionalCollection,
            payment?.Id,
            ApiText.EnumValue(refund.Order.Status),
            settledAt,
            refund.Order.Items.Select(item => new SettledInventoryDto(
                item.InventoryItemId,
                ApiText.EnumValue(item.InventoryItem.Status),
                item.InventoryItem.CleaningUntil)).ToList());
    }

    private async Task<RefundDto> ChangeRefundStatus(
        Guid refundId,
        RefundStatus requiredStatus,
        Action<Refund> change,
        CancellationToken cancellationToken)
    {
        var initial = await GetRefundRequired(refundId, false, cancellationToken);
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockOrder(initial.OrderId, cancellationToken);
        _ = await returnRepository.LockRefund(refundId, cancellationToken);
        var refund = await GetRefundRequired(refundId, true, cancellationToken);
        if (refund.Status != requiredStatus)
        {
            throw new ConflictException("REFUND_TRANSITION_NOT_ALLOWED", $"Phiếu đang ở trạng thái {ApiText.EnumValue(refund.Status)}.");
        }

        change(refund);
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        return ToRefundDto(refund);
    }

    private async Task<Order> GetOrderRequired(Guid orderId, bool tracking, CancellationToken cancellationToken) =>
        await rentalRepository.GetOrder(orderId, requestContext.BranchId, tracking, cancellationToken)
            ?? throw new NotFoundException("ORDER_NOT_FOUND", "Không tìm thấy order tại chi nhánh này.");

    private async Task<Refund> GetRefundRequired(Guid refundId, bool tracking, CancellationToken cancellationToken) =>
        await returnRepository.GetRefund(refundId, requestContext.BranchId, tracking, cancellationToken)
            ?? throw new NotFoundException("REFUND_NOT_FOUND", "Không tìm thấy phiếu đối soát tại chi nhánh này.");

    private static void ValidateInspection(InspectOrderItemRequest request)
    {
        if (request.ActualRentalFee < 0 || request.ProcessingFee < 0)
        {
            throw new ValidationException("INVALID_MONEY", "Phí thuê và phí xử lý không được âm.");
        }
    }

    private static void ValidateInspectionByCondition(
        ItemCondition condition,
        InventoryStatus outcome,
        InspectOrderItemRequest request)
    {
        if (condition == ItemCondition.Good && (request.ProcessingFee != 0 || outcome != InventoryStatus.Usable))
        {
            throw new ValidationException("GOOD_ITEM_INVALID", "Item tốt phải có phí xử lý 0 và trả về USABLE.");
        }

        if (condition == ItemCondition.Damaged &&
            (string.IsNullOrWhiteSpace(request.DamageNote) || request.DamagePhotoPaths.Count == 0 || outcome != InventoryStatus.Maintenance))
        {
            throw new ValidationException("DAMAGE_EVIDENCE_REQUIRED", "Item hư cần ghi chú, ảnh và trạng thái MAINTENANCE.");
        }

        if (condition == ItemCondition.Missing && outcome != InventoryStatus.Lost)
        {
            throw new ValidationException("MISSING_ITEM_MUST_BE_LOST", "Item mất phải chuyển trạng thái LOST.");
        }
    }

    private static void EnsureRefundCanBeCalculated(Order order)
    {
        if (order.Status != OrderStatus.Inspecting || order.Items.Any(item => !item.Condition.HasValue || !item.ActualRentalFee.HasValue))
        {
            throw new ConflictException("INSPECTION_INCOMPLETE", "Cần hoàn tất kiểm tra tất cả item trước khi tạo phiếu.");
        }
    }

    private static RefundCalculationDto Calculate(Order order)
    {
        var deposit = RentalRules.ConfirmedDeposit(order.Reservation.Payments);
        var rental = order.Items.Sum(item => item.ActualRentalFee ?? 0);
        var processing = order.Items.Sum(item => item.ProcessingFee);
        return new RefundCalculationDto(
            deposit,
            rental,
            processing,
            Math.Max(0, deposit - rental - processing),
            Math.Max(0, rental + processing - deposit));
    }

    private static void RefreshRefund(Refund refund, Order order)
    {
        var calculation = Calculate(order);
        refund.DepositAmount = calculation.DepositConfirmed;
        refund.RentalFee = calculation.RentalFee;
        refund.ProcessingFee = calculation.ProcessingFee;
        refund.RefundAmount = calculation.RefundAmount;
        refund.ItemsSnapshot = JsonSerializer.SerializeToDocument(order.Items.Select(ToSnapshot).ToList());
    }

    private static RefundSnapshotItemDto ToSnapshot(OrderItem item) => new(
        item.Id,
        item.ProductName,
        item.Size,
        item.AssetCode,
        item.Condition.HasValue ? ApiText.EnumValue(item.Condition.Value) : string.Empty,
        item.ActualRentalFee ?? 0,
        item.ProcessingFee,
        item.DamageNote,
        item.DamagePhotoPaths);

    private static RefundDto ToRefundDto(Refund refund) => new(
        refund.Id,
        refund.OrderId,
        refund.Version,
        ApiText.EnumValue(refund.Status),
        refund.DepositAmount,
        refund.RentalFee,
        refund.ProcessingFee,
        refund.RefundAmount,
        AdditionalCollection(refund),
        JsonSerializer.Deserialize<List<RefundSnapshotItemDto>>(refund.ItemsSnapshot.RootElement.GetRawText()) ?? [],
        refund.AdjustmentReason);

    private static decimal AdditionalCollection(Refund refund) =>
        Math.Max(0, refund.RentalFee + refund.ProcessingFee - refund.DepositAmount);

    private void EnsureManager()
    {
        if (requestContext.Role != UserRole.Manager)
        {
            throw new ForbiddenException("MANAGER_REQUIRED", "Thao tác này chỉ dành cho manager.");
        }
    }

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > 100)
        {
            throw new ValidationException("INVALID_LIMIT", "Limit phải nằm trong khoảng 1 đến 100.");
        }
    }
}
