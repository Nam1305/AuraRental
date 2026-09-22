using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.DTOs.Order;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.Utils;

namespace AuraRental.Service.UseCase;

public sealed class OrderUseCase(
    IRentalRepository rentalRepository,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext) : IOrderUseCase
{
    public async Task<IReadOnlyList<OrderListItemDto>> Search(
        string? status,
        string? query,
        DateOnly? from,
        DateOnly? to,
        int limit,
        CancellationToken cancellationToken)
    {
        ValidateLimit(limit);
        if (from.HasValue && to.HasValue && to < from)
        {
            throw new ValidationException("INVALID_DATE_RANGE", "Ngày kết thúc phải từ ngày bắt đầu trở đi.");
        }

        var statuses = RentalRules.ParseEnumList<OrderStatus>(
            status,
            "INVALID_ORDER_STATUS",
            "Trạng thái order không hợp lệ.");
        var orders = await rentalRepository.SearchOrders(
            requestContext.BranchId,
            statuses,
            query,
            from,
            to,
            limit,
            cancellationToken);

        return orders.Select(order =>
        {
            var confirmed = RentalRules.ConfirmedDeposit(order.Reservation.Payments);
            return new OrderListItemDto(
                order.Id,
                order.OrderNo,
                ApiText.EnumValue(order.Status),
                order.CustomerId,
                order.CustomerName,
                RentalRules.MaskPhone(order.CustomerPhone),
                string.Join(", ", order.Items.Select(item => $"{item.ProductName} · {item.Size}")),
                order.Reservation.RentalStartAt,
                order.Reservation.RentalEndAt,
                order.Reservation.DepositRequired,
                confirmed,
                Math.Max(0, order.Reservation.DepositRequired - confirmed),
                order.CreatedAt);
        }).ToList();
    }

    public async Task<OrderDetailDto> Get(Guid orderId, CancellationToken cancellationToken) =>
        ToDetail(await GetRequired(orderId, false, cancellationToken));

    public async Task<VerifyIdentityDto> VerifyIdentity(
        Guid orderId,
        VerifyIdentityRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Verified)
        {
            throw new ValidationException("IDENTITY_VERIFICATION_REQUIRED", "Chỉ gửi action này sau khi đã kiểm tra CCCD.");
        }

        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockOrder(orderId, cancellationToken)
            ?? throw new NotFoundException("ORDER_NOT_FOUND", "Không tìm thấy order.");
        var order = await GetRequired(orderId, true, cancellationToken);
        if (order.Reservation.DepositPlan != "FIFTY_WITH_ID")
        {
            throw new ValidationException("IDENTITY_NOT_REQUIRED", "Gói cọc này không yêu cầu kiểm tra CCCD.");
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed or OrderStatus.Renting or OrderStatus.Inspecting)
        {
            throw TransitionError(order);
        }

        order.IdentityVerifiedBy = requestContext.UserId;
        order.IdentityVerifiedAt = DateTimeOffset.UtcNow;
        order.Status = RentalRules.DepositRemaining(order.Reservation) == 0
            ? OrderStatus.Confirmed
            : OrderStatus.PendingDeposit;
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        return new VerifyIdentityDto(
            true,
            order.IdentityVerifiedBy,
            order.IdentityVerifiedAt,
            ApiText.EnumValue(order.Status));
    }

    public async Task<CancelOrderDto> Cancel(
        Guid orderId,
        CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException("CANCELLATION_REASON_REQUIRED", "Cần nhập lý do hủy order.");
        }

        if (request.PaymentDecision.ToUpperInvariant() is not ("KEEP_DEPOSIT" or "REVIEW_SEPARATELY"))
        {
            throw new ValidationException("INVALID_PAYMENT_DECISION", "Quyết định xử lý tiền không hợp lệ.");
        }

        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockOrder(orderId, cancellationToken)
            ?? throw new NotFoundException("ORDER_NOT_FOUND", "Không tìm thấy order.");
        var order = await GetRequired(orderId, true, cancellationToken);
        if (order.Status == OrderStatus.Cancelled)
        {
            return ToCancelResult(order);
        }

        if (order.Status is OrderStatus.Renting or OrderStatus.Inspecting or OrderStatus.Completed)
        {
            throw TransitionError(order);
        }

        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = request.Reason.Trim();
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        return ToCancelResult(order);
    }

    public Task<OrderTransitionDto> Prepare(Guid orderId, CancellationToken cancellationToken) =>
        Transition(orderId, OrderStatus.Confirmed, order => order.Status = OrderStatus.Preparing, cancellationToken);

    public Task<OrderTransitionDto> StartDelivery(
        Guid orderId,
        StartDeliveryRequest request,
        CancellationToken cancellationToken) =>
        Transition(orderId, OrderStatus.Preparing, order =>
        {
            order.DeliveryStatus = "IN_TRANSIT";
            order.DeliveryTrackingCode = request.TrackingCode?.Trim();
        }, cancellationToken);

    public Task<OrderTransitionDto> CompleteDelivery(
        Guid orderId,
        CompleteDeliveryRequest request,
        CancellationToken cancellationToken) =>
        Transition(orderId, OrderStatus.Preparing, order =>
        {
            if (order.DeliveryStatus != "IN_TRANSIT")
            {
                throw new ConflictException("DELIVERY_NOT_STARTED", "Cần bắt đầu giao hàng trước khi xác nhận hoàn tất.");
            }

            order.DeliveryStatus = "DONE";
            order.DeliveredAt = request.DeliveredAt.ToUniversalTime();
            order.Status = OrderStatus.Renting;
        }, cancellationToken);

    public Task<OrderTransitionDto> StartReturnDelivery(
        Guid orderId,
        StartDeliveryRequest request,
        CancellationToken cancellationToken) =>
        Transition(orderId, OrderStatus.Renting, order =>
        {
            order.ReturnDeliveryStatus = "IN_TRANSIT";
            order.ReturnTrackingCode = request.TrackingCode?.Trim();
        }, cancellationToken);

    public Task<OrderTransitionDto> CompleteReturnDelivery(
        Guid orderId,
        CompleteReturnDeliveryRequest request,
        CancellationToken cancellationToken) =>
        Transition(orderId, OrderStatus.Renting, order =>
        {
            if (order.ReturnDeliveryStatus != "IN_TRANSIT")
            {
                throw new ConflictException("RETURN_DELIVERY_NOT_STARTED", "Cần bắt đầu nhận đồ trả trước khi xác nhận hoàn tất.");
            }

            order.ReturnDeliveryStatus = "DONE";
            order.ReturnedAt = request.ReturnedAt.ToUniversalTime();
            order.Status = OrderStatus.Inspecting;
        }, cancellationToken);

    public async Task<PaymentActionDto> ConfirmPayment(Guid paymentId, CancellationToken cancellationToken)
    {
        var initial = await rentalRepository.GetPayment(paymentId, requestContext.BranchId, false, cancellationToken)
            ?? throw new NotFoundException("PAYMENT_NOT_FOUND", "Không tìm thấy giao dịch.");
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockReservation(initial.ReservationId, cancellationToken);
        var payment = await rentalRepository.GetPayment(paymentId, requestContext.BranchId, true, cancellationToken)
            ?? throw new NotFoundException("PAYMENT_NOT_FOUND", "Không tìm thấy giao dịch.");
        if (payment.Status == PaymentStatus.Confirmed)
        {
            return ToPaymentAction(payment);
        }

        if (payment.Status == PaymentStatus.Voided)
        {
            throw new ConflictException("PAYMENT_VOIDED", "Không thể xác nhận giao dịch đã void.");
        }

        if (payment.Type == PaymentType.TargetDeposit && payment.Amount > RentalRules.DepositRemaining(payment.Reservation))
        {
            throw new ConflictException("PAYMENT_AMOUNT_EXCEEDS_REMAINING", "Số tiền vượt quá cọc còn lại hiện tại.");
        }

        payment.Status = PaymentStatus.Confirmed;
        payment.ConfirmedBy = requestContext.UserId;
        UpdateOrderAfterPayment(payment.Reservation);
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        return ToPaymentAction(payment);
    }

    public async Task<PaymentActionDto> VoidPayment(
        Guid paymentId,
        PaymentActionRequest request,
        CancellationToken cancellationToken)
    {
        if (requestContext.Role != UserRole.Manager)
        {
            throw new ForbiddenException("MANAGER_REQUIRED", "Chỉ manager được void giao dịch.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException("VOID_REASON_REQUIRED", "Cần nhập lý do void giao dịch.");
        }

        var initial = await rentalRepository.GetPayment(paymentId, requestContext.BranchId, false, cancellationToken)
            ?? throw new NotFoundException("PAYMENT_NOT_FOUND", "Không tìm thấy giao dịch.");
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockReservation(initial.ReservationId, cancellationToken);
        var payment = await rentalRepository.GetPayment(paymentId, requestContext.BranchId, true, cancellationToken)
            ?? throw new NotFoundException("PAYMENT_NOT_FOUND", "Không tìm thấy giao dịch.");
        if (payment.RefundId.HasValue || payment.Reservation.Order?.SettledAt is not null)
        {
            throw new ConflictException("PAYMENT_ALREADY_SETTLED", "Không thể void giao dịch đã dùng để đối soát.");
        }

        payment.Status = PaymentStatus.Voided;
        payment.Note = $"{payment.Note} | VOID: {request.Reason.Trim()}".Trim(' ', '|');
        UpdateOrderAfterPayment(payment.Reservation);
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        return ToPaymentAction(payment);
    }

    private async Task<OrderTransitionDto> Transition(
        Guid orderId,
        OrderStatus requiredStatus,
        Action<Order> change,
        CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockOrder(orderId, cancellationToken)
            ?? throw new NotFoundException("ORDER_NOT_FOUND", "Không tìm thấy order.");
        var order = await GetRequired(orderId, true, cancellationToken);
        if (order.Status != requiredStatus)
        {
            throw TransitionError(order);
        }

        change(order);
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        return ToTransition(order);
    }

    private async Task<Order> GetRequired(Guid orderId, bool tracking, CancellationToken cancellationToken) =>
        await rentalRepository.GetOrder(orderId, requestContext.BranchId, tracking, cancellationToken)
            ?? throw new NotFoundException("ORDER_NOT_FOUND", "Không tìm thấy order tại chi nhánh này.");

    private static OrderDetailDto ToDetail(Order order)
    {
        var confirmed = RentalRules.ConfirmedDeposit(order.Reservation.Payments);
        return new OrderDetailDto(
            order.Id,
            order.ReservationId,
            order.OrderNo,
            ApiText.EnumValue(order.Status),
            order.Branch.Id,
            order.Branch.Code,
            order.Branch.Name,
            new OrderCustomerDto(order.CustomerId, order.CustomerName, order.CustomerPhone, order.DeliveryAddress),
            order.Reservation.RentalStartAt,
            order.Reservation.RentalEndAt,
            order.Items.Select(item => new OrderItemDto(
                item.Id,
                item.InventoryItemId,
                item.ProductName,
                item.Size,
                item.AssetCode,
                item.PackageCode,
                item.ActualRentalFee ?? RentalFee(item, order.Reservation),
                item.Condition.HasValue ? ApiText.EnumValue(item.Condition.Value) : null,
                item.ProcessingFee,
                item.DamageNote,
                item.DamagePhotoPaths)).ToList(),
            order.Reservation.DepositRequired,
            confirmed,
            Math.Max(0, order.Reservation.DepositRequired - confirmed),
            new IdentityVerificationDto(
                order.Reservation.DepositPlan == "FIFTY_WITH_ID",
                order.IdentityVerifiedAt.HasValue,
                order.IdentityVerifiedBy,
                order.IdentityVerifiedAt),
            new DeliveryDto(order.DeliveryStatus, order.DeliveryTrackingCode, order.DeliveredAt),
            new DeliveryDto(order.ReturnDeliveryStatus, order.ReturnTrackingCode, order.ReturnedAt),
            order.Reservation.Payments.OrderBy(payment => payment.PaidAt).Select(payment => new OrderPaymentDto(
                payment.Id,
                ApiText.EnumValue(payment.Type),
                payment.Amount,
                payment.Method,
                ApiText.EnumValue(payment.Status),
                payment.TransactionRef,
                payment.PaidAt)).ToList(),
            AllowedActions(order));
    }

    private static IReadOnlyList<string> AllowedActions(Order order)
    {
        var actions = new List<string>();
        if (RentalRules.DepositRemaining(order.Reservation) > 0 && order.Status != OrderStatus.Cancelled)
        {
            actions.Add("RECORD_TARGET_DEPOSIT");
        }

        if (order.Reservation.DepositPlan == "FIFTY_WITH_ID" && !order.IdentityVerifiedAt.HasValue &&
            order.Status is OrderStatus.PendingDeposit or OrderStatus.PendingVerification)
        {
            actions.Add("VERIFY_IDENTITY");
        }

        actions.AddRange(order.Status switch
        {
            OrderStatus.Confirmed => ["PREPARE", "CANCEL"],
            OrderStatus.Preparing => ["START_DELIVERY", "COMPLETE_DELIVERY", "CANCEL"],
            OrderStatus.Renting => ["START_RETURN", "COMPLETE_RETURN"],
            OrderStatus.Inspecting => ["INSPECT_ITEMS", "CREATE_REFUND"],
            OrderStatus.PendingDeposit or OrderStatus.PendingVerification => ["CANCEL"],
            _ => []
        });
        return actions;
    }

    private static decimal RentalFee(OrderItem item, Reservation reservation)
    {
        var extraDays = Math.Max(0, (int)Math.Ceiling((reservation.RentalEndAt - reservation.RentalStartAt).TotalDays - 3));
        return item.RentalPrice + Math.Ceiling(extraDays * item.OneDayPrice * item.ExtraDayRate);
    }

    private static CancelOrderDto ToCancelResult(Order order) => new(
        order.Id,
        ApiText.EnumValue(order.Status),
        order.CancellationReason ?? string.Empty,
        true,
        RentalRules.ConfirmedDeposit(order.Reservation.Payments) > 0);

    private static OrderTransitionDto ToTransition(Order order) => new(
        order.Id,
        ApiText.EnumValue(order.Status),
        order.DeliveryStatus,
        order.ReturnDeliveryStatus);

    private static ConflictException TransitionError(Order order) => new(
        "ORDER_TRANSITION_NOT_ALLOWED",
        $"Không thể thực hiện thao tác khi order đang ở trạng thái {ApiText.EnumValue(order.Status)}. " +
        $"Các thao tác hợp lệ: {string.Join(", ", AllowedActions(order))}.");

    private static void UpdateOrderAfterPayment(Reservation reservation)
    {
        if (reservation.Order is not { } order || order.Status is OrderStatus.Cancelled or OrderStatus.Completed)
        {
            return;
        }

        if (RentalRules.DepositRemaining(reservation) > 0)
        {
            order.Status = OrderStatus.PendingDeposit;
        }
        else if (reservation.DepositPlan == "FIFTY_WITH_ID" && !order.IdentityVerifiedAt.HasValue)
        {
            order.Status = OrderStatus.PendingVerification;
        }
        else if (order.Status is OrderStatus.PendingDeposit or OrderStatus.PendingVerification)
        {
            order.Status = OrderStatus.Confirmed;
        }
    }

    private static PaymentActionDto ToPaymentAction(Payment payment) => new(
        payment.Id,
        ApiText.EnumValue(payment.Status),
        RentalRules.ConfirmedDeposit(payment.Reservation.Payments),
        RentalRules.DepositRemaining(payment.Reservation),
        payment.Reservation.Order is null ? null : ApiText.EnumValue(payment.Reservation.Order.Status));

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > 100)
        {
            throw new ValidationException("INVALID_LIMIT", "Limit phải nằm trong khoảng 1 đến 100.");
        }
    }
}
