using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.DTOs.Reservation;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.Utils;
using Microsoft.Extensions.Configuration;

namespace AuraRental.Service.UseCase;

public sealed class ReservationUseCase(
    IRentalRepository rentalRepository,
    IQuoteRepository quoteRepository,
    IAdminRepository adminRepository,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    ISecureTokenService secureTokenService,
    IConfiguration configuration) : IReservationUseCase
{
    public async Task<ReservationDto> Create(CreateReservationRequest request, CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);
        var branch = await rentalRepository.GetBranch(requestContext.BranchId, cancellationToken)
            ?? throw new NotFoundException("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh.");

        var credentials = secureTokenService.GenerateCustomerFormCredential();
        var inventoryIds = request.Items.Select(item => item.InventoryItemId).Distinct().Order().ToArray();
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        var inventory = await rentalRepository.LockInventoryItems(branch.Id, inventoryIds, cancellationToken);
        if (inventory.Count != inventoryIds.Length)
        {
            throw new NotFoundException("INVENTORY_NOT_FOUND", "Có mã đồ không thuộc chi nhánh đang chọn.");
        }

        if (!await rentalRepository.AreInventoryItemsAvailable(
                branch.Id,
                inventoryIds,
                request.RentalStartAt,
                request.RentalEndAt,
                null,
                cancellationToken))
        {
            throw new ConflictException("INVENTORY_NOT_AVAILABLE", "Có mã đồ vừa được giữ bởi đơn khác.");
        }

        var itemRequests = request.Items.ToDictionary(item => item.InventoryItemId);
        var settings = await adminRepository.GetSettings(false, cancellationToken) ?? new Setting();
        var reservationItems = inventory.Select(item => CreateReservationItem(item, itemRequests[item.Id], settings.ExtraDayRate)).ToList();
        var depositRequired = reservationItems.Sum(item => item.ReplacementValue) * RentalRules.GetDepositRatio(request.DepositPlan);
        var paymentType = RentalRules.ParseEnum<PaymentType>(
            request.ReceivedPayment.Type,
            "INVALID_PAYMENT_TYPE",
            "Giao dịch đầu tiên phải là SLOT_DEPOSIT hoặc TARGET_DEPOSIT.");
        ValidateInitialPayment(paymentType, request.ReceivedPayment.Amount, depositRequired,
            await quoteRepository.GetSlotDepositAmount(cancellationToken));

        var now = DateTimeOffset.UtcNow;
        var reservation = new Reservation
        {
            ReservationNo = RentalRules.CreateNumber(branch.Code, "R"),
            BranchId = branch.Id,
            Status = ReservationStatus.Active,
            RentalStartAt = request.RentalStartAt.ToUniversalTime(),
            RentalEndAt = request.RentalEndAt.ToUniversalTime(),
            DepositPlan = request.DepositPlan.ToUpperInvariant(),
            DepositRequired = depositRequired,
            OtpHash = secureTokenService.Hash(credentials.Otp),
            FormTokenHash = secureTokenService.Hash(credentials.Token),
            OtpExpiresAt = credentials.ExpiresAt,
            CreatedBy = requestContext.UserId,
            CreatedAt = now,
            Items = reservationItems,
            Payments =
            [
                new Payment
                {
                    Type = paymentType,
                    Amount = request.ReceivedPayment.Amount,
                    Method = RequireText(request.ReceivedPayment.Method, "PAYMENT_METHOD_REQUIRED", "Phương thức thanh toán là bắt buộc."),
                    Status = PaymentStatus.Confirmed,
                    // The shop does not need to type a banking reference while creating a hold.
                    // Use one consistent, traceable reference for every initial payment.
                    TransactionRef = RentalRules.CreateTransactionReference(branch.Code),
                    ProofPath = request.ReceivedPayment.ProofPath?.Trim(),
                    RecordedBy = requestContext.UserId,
                    ConfirmedBy = requestContext.UserId,
                    PaidAt = request.ReceivedPayment.PaidAt.ToUniversalTime()
                }
            ]
        };

        rentalRepository.AddReservation(reservation);
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);

        var saved = await rentalRepository.GetReservation(reservation.Id, branch.Id, false, cancellationToken)
            ?? throw new InvalidOperationException("Reservation was created but could not be reloaded.");
        return ToDto(saved, new CustomerFormCredentialDto(
            BuildFormUrl(credentials.Token),
            credentials.Otp,
            credentials.ExpiresAt));
    }

    public async Task<IReadOnlyList<ReservationListItemDto>> Search(
        string? status,
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        ValidateLimit(limit);
        var statuses = RentalRules.ParseEnumList<ReservationStatus>(
            status,
            "INVALID_RESERVATION_STATUS",
            "Trạng thái reservation không hợp lệ.");
        var reservations = await rentalRepository.SearchReservations(
            requestContext.BranchId,
            statuses,
            query,
            limit,
            cancellationToken);

        return reservations.Select(reservation => new ReservationListItemDto(
            reservation.Id,
            reservation.ReservationNo,
            ApiText.EnumValue(reservation.Status),
            reservation.CustomerId,
            reservation.Customer?.Name ?? "Chờ khách điền form",
            reservation.Customer is null ? "Chưa có SĐT" : RentalRules.MaskPhone(reservation.Customer.Phone),
            string.Join(", ", reservation.Items.Select(ItemSummary)),
            reservation.RentalStartAt,
            reservation.RentalEndAt,
            RentalRules.ConfirmedDeposit(reservation.Payments),
            RentalRules.DepositRemaining(reservation),
            reservation.Order is null ? "NOT_SUBMITTED" : "SUBMITTED")).ToList();
    }

    public async Task<ReservationDto> Get(int reservationId, CancellationToken cancellationToken)
    {
        var reservation = await GetRequired(reservationId, false, cancellationToken);
        return ToDto(reservation, null);
    }

    public async Task<ReissueOtpDto> ReissueOtp(
        int reservationId,
        ReissueOtpRequest request,
        CancellationToken cancellationToken)
    {
        _ = RequireText(request.Reason, "REISSUE_REASON_REQUIRED", "Cần nhập lý do cấp lại OTP.");
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockReservation(reservationId, cancellationToken)
            ?? throw new NotFoundException("RESERVATION_NOT_FOUND", "Không tìm thấy reservation.");
        var reservation = await GetRequired(reservationId, true, cancellationToken);
        EnsureEditable(reservation);

        var credentials = secureTokenService.GenerateCustomerFormCredential();
        reservation.OtpHash = secureTokenService.Hash(credentials.Otp);
        reservation.FormTokenHash = secureTokenService.Hash(credentials.Token);
        reservation.OtpExpiresAt = credentials.ExpiresAt;
        reservation.OtpUsedAt = null;
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);

        return new ReissueOtpDto(BuildFormUrl(credentials.Token), credentials.Otp, credentials.ExpiresAt);
    }

    public async Task<ReservationDto> UpdateRentalSelection(
        int reservationId,
        UpdateRentalSelectionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RentalEndAt <= request.RentalStartAt || request.Items.Count == 0 ||
            request.Items.Select(item => item.InventoryItemId).Distinct().Count() != request.Items.Count)
        {
            throw new ValidationException("INVALID_RENTAL_SELECTION", "Lịch thuê hoặc danh sách mã đồ không hợp lệ.");
        }

        _ = RequireText(request.Reason, "SELECTION_CHANGE_REASON_REQUIRED", "Cần nhập lý do đổi lịch hoặc mã đồ.");
        var initial = await GetRequired(reservationId, false, cancellationToken);
        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockReservation(reservationId, cancellationToken)
            ?? throw new NotFoundException("RESERVATION_NOT_FOUND", "Không tìm thấy reservation.");
        var reservation = await GetRequired(reservationId, true, cancellationToken);
        EnsureEditable(reservation);

        var requestedIds = request.Items.Select(item => item.InventoryItemId).Distinct().ToArray();
        var allLockIds = reservation.Items.Select(item => item.InventoryItemId).Concat(requestedIds).Distinct().Order().ToArray();
        var locked = await rentalRepository.LockInventoryItems(requestContext.BranchId, allLockIds, cancellationToken);
        var selectedInventory = locked.Where(item => requestedIds.Contains(item.Id)).ToList();
        if (selectedInventory.Count != requestedIds.Length)
        {
            throw new NotFoundException("INVENTORY_NOT_FOUND", "Có mã đồ không thuộc chi nhánh đang chọn.");
        }

        if (!await rentalRepository.AreInventoryItemsAvailable(
                requestContext.BranchId,
                requestedIds,
                request.RentalStartAt,
                request.RentalEndAt,
                reservation.Id,
                cancellationToken))
        {
            throw new ConflictException("INVENTORY_NOT_AVAILABLE", "Có mã đồ không còn trống trong lịch mới.");
        }

        var requests = request.Items.ToDictionary(item => item.InventoryItemId);
        reservation.Items.Clear();
        var settings = await adminRepository.GetSettings(false, cancellationToken) ?? new Setting();
        foreach (var item in selectedInventory.Select(item => CreateReservationItem(item, requests[item.Id], settings.ExtraDayRate)))
        {
            reservation.Items.Add(item);
        }

        reservation.RentalStartAt = request.RentalStartAt.ToUniversalTime();
        reservation.RentalEndAt = request.RentalEndAt.ToUniversalTime();
        reservation.DepositRequired = reservation.Items.Sum(item => item.ReplacementValue) * RentalRules.GetDepositRatio(reservation.DepositPlan);
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);
        return ToDto(reservation, null);
    }

    public async Task<CancelReservationDto> Cancel(
        int reservationId,
        CancelReservationRequest request,
        CancellationToken cancellationToken)
    {
        var reason = RequireText(request.Reason, "CANCELLATION_REASON_REQUIRED", "Cần nhập lý do hủy.");
        var paymentDecision = request.PaymentDecision.ToUpperInvariant();
        if (paymentDecision is not ("KEEP_DEPOSIT" or "REVIEW_SEPARATELY"))
        {
            throw new ValidationException("INVALID_PAYMENT_DECISION", "Quyết định tiền cọc không hợp lệ.");
        }

        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockReservation(reservationId, cancellationToken)
            ?? throw new NotFoundException("RESERVATION_NOT_FOUND", "Không tìm thấy reservation.");
        var reservation = await GetRequired(reservationId, true, cancellationToken);
        EnsureEditable(reservation);
        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancellationReason = reason;
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);

        return new CancelReservationDto(
            reservation.Id,
            ApiText.EnumValue(reservation.Status),
            RentalRules.ConfirmedDeposit(reservation.Payments) > 0);
    }

    public async Task<RecordPaymentDto> RecordPayment(
        int reservationId,
        RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("PAYMENT_AMOUNT_INVALID", "Số tiền phải lớn hơn 0.");
        }

        var paymentType = RentalRules.ParseEnum<PaymentType>(
            request.Type,
            "INVALID_PAYMENT_TYPE",
            "Loại giao dịch không hợp lệ.");
        if (paymentType is not (PaymentType.TargetDeposit or PaymentType.AdditionalCollection))
        {
            throw new ValidationException("INVALID_PAYMENT_TYPE", "Endpoint này chỉ ghi cọc đích hoặc khoản thu thêm.");
        }

        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockReservation(reservationId, cancellationToken)
            ?? throw new NotFoundException("RESERVATION_NOT_FOUND", "Không tìm thấy reservation.");
        var reservation = await GetRequired(reservationId, true, cancellationToken);
        if (reservation.Status == ReservationStatus.Cancelled)
        {
            throw new ConflictException("RESERVATION_CANCELLED", "Không thể ghi tiền cho reservation đã hủy.");
        }

        var remainingBefore = RentalRules.DepositRemaining(reservation);
        if (paymentType == PaymentType.TargetDeposit && request.ConfirmNow && request.Amount > remainingBefore)
        {
            throw new ValidationException("PAYMENT_AMOUNT_EXCEEDS_REMAINING", "Số tiền cọc vượt quá phần còn lại.");
        }

        var payment = new Payment
        {
            ReservationId = reservation.Id,
            Type = paymentType,
            Amount = request.Amount,
            Method = RequireText(request.Method, "PAYMENT_METHOD_REQUIRED", "Phương thức thanh toán là bắt buộc."),
            Status = request.ConfirmNow ? PaymentStatus.Confirmed : PaymentStatus.Recorded,
            TransactionRef = request.TransactionRef?.Trim(),
            ProofPath = request.ProofPath?.Trim(),
            RecordedBy = requestContext.UserId,
            ConfirmedBy = request.ConfirmNow ? requestContext.UserId : null,
            PaidAt = request.PaidAt.ToUniversalTime(),
            Note = request.Note?.Trim()
        };
        reservation.Payments.Add(payment);
        // The reservation is loaded through a row-lock query before its graph is hydrated.
        // Explicitly mark a new payment as Added; relationship discovery alone may treat a
        // Explicitly mark a new database-generated integer ID as Added.
        rentalRepository.AddPayment(payment);
        UpdateOrderAfterDeposit(reservation);
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);

        return new RecordPaymentDto(
            payment.Id,
            ApiText.EnumValue(payment.Status),
            RentalRules.ConfirmedDeposit(reservation.Payments),
            RentalRules.DepositRemaining(reservation),
            reservation.Order is null ? null : ApiText.EnumValue(reservation.Order.Status));
    }

    private async Task<Reservation> GetRequired(int reservationId, bool tracking, CancellationToken cancellationToken) =>
        await rentalRepository.GetReservation(reservationId, requestContext.BranchId, tracking, cancellationToken)
            ?? throw new NotFoundException("RESERVATION_NOT_FOUND", "Không tìm thấy reservation tại chi nhánh này.");

    private static ReservationItem CreateReservationItem(
        InventoryItem inventoryItem,
        ReservationItemRequest request,
        decimal extraDayRate)
    {
        var selectedPrice = inventoryItem.Variant.RentalPrices.FirstOrDefault(price =>
            price.PackageCode.Equals(request.PackageCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException(
                "PACKAGE_NOT_OFFERED_AT_BRANCH",
                $"Gói {request.PackageCode} không áp dụng cho mã {inventoryItem.AssetCode}.");
        var oneDayPrice = inventoryItem.Variant.RentalPrices.FirstOrDefault(price => price.PackageCode == "1D")
            ?? throw new ValidationException("ONE_DAY_PRICE_REQUIRED", $"Mã {inventoryItem.AssetCode} chưa có giá 1D.");

        return new ReservationItem
        {
            InventoryItemId = inventoryItem.Id,
            PackageCode = selectedPrice.PackageCode,
            ReplacementValue = inventoryItem.Variant.ReplacementValue,
            RentalPrice = selectedPrice.Price,
            OneDayPrice = oneDayPrice.Price,
            ExtraDayRate = extraDayRate
        };
    }

    private static void ValidateCreateRequest(CreateReservationRequest request)
    {
        if (request.RentalEndAt <= request.RentalStartAt)
        {
            throw new ValidationException("INVALID_TIME_RANGE", "Thời gian trả phải sau thời gian nhận.");
        }

        if (request.Items.Count == 0 || request.Items.Select(item => item.InventoryItemId).Distinct().Count() != request.Items.Count)
        {
            throw new ValidationException("INVALID_RESERVATION_ITEMS", "Reservation cần ít nhất một mã đồ và không được trùng mã.");
        }

        _ = RentalRules.GetDepositRatio(request.DepositPlan);
    }

    private static void ValidateInitialPayment(
        PaymentType paymentType,
        decimal amount,
        decimal depositRequired,
        decimal slotDepositAmount)
    {
        if (amount <= 0)
        {
            throw new ValidationException("PAYMENT_AMOUNT_INVALID", "Số tiền cọc phải lớn hơn 0.");
        }

        if (paymentType == PaymentType.SlotDeposit)
        {
            if (amount != slotDepositAmount)
            {
                throw new ValidationException("PAYMENT_AMOUNT_INVALID", $"Cọc giữ chỗ phải đúng {slotDepositAmount:N0} VND.");
            }

            return;
        }

        if (paymentType != PaymentType.TargetDeposit || amount > depositRequired)
        {
            throw new ValidationException("PAYMENT_AMOUNT_INVALID", "Cọc ban đầu phải không vượt quá số tiền cọc yêu cầu.");
        }
    }

    private ReservationDto ToDto(Reservation reservation, CustomerFormCredentialDto? customerForm)
    {
        var confirmed = RentalRules.ConfirmedDeposit(reservation.Payments);
        return new ReservationDto(
            reservation.Id,
            reservation.ReservationNo,
            ApiText.EnumValue(reservation.Status),
            new ReservationBranchDto(reservation.Branch.Id, reservation.Branch.Code, reservation.Branch.Name),
            reservation.Customer is null
                ? null
                : new ReservationCustomerDto(reservation.Customer.Id, reservation.Customer.Name, reservation.Customer.Phone),
            reservation.RentalStartAt,
            reservation.RentalEndAt,
            new ReservationDepositDto(
                reservation.DepositPlan,
                reservation.DepositRequired,
                confirmed,
                Math.Max(0, reservation.DepositRequired - confirmed)),
            reservation.Items.Select(item => new ReservationItemDto(
                item.InventoryItemId,
                item.InventoryItem.AssetCode,
                item.InventoryItem.Variant.Product.Name,
                item.InventoryItem.Variant.Size,
                item.PackageCode,
                RentalRules.RentalFee(item, reservation.RentalStartAt, reservation.RentalEndAt))).ToList(),
            customerForm,
            reservation.Order is null ? "NOT_SUBMITTED" : "SUBMITTED",
            reservation.CreatedAt);
    }

    private static string ItemSummary(ReservationItem item) =>
        $"{item.InventoryItem.Variant.Product.Name} {item.InventoryItem.Variant.Size} · {item.InventoryItem.AssetCode}";

    private static void EnsureEditable(Reservation reservation)
    {
        if (reservation.Status != ReservationStatus.Active || reservation.Order is not null)
        {
            throw new ConflictException("RESERVATION_NOT_EDITABLE", "Reservation không còn có thể chỉnh sửa.");
        }
    }

    private static void UpdateOrderAfterDeposit(Reservation reservation)
    {
        var order = reservation.Order;
        if (order is null || order.Status is OrderStatus.Cancelled or OrderStatus.Completed)
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
        else
        {
            order.Status = OrderStatus.Confirmed;
        }
    }

    private string BuildFormUrl(string token) =>
        $"{configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173"}/r/{token}";

    private static string RequireText(string? value, string code, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new ValidationException(code, message) : value.Trim();

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > 100)
        {
            throw new ValidationException("INVALID_LIMIT", "Limit phải nằm trong khoảng 1 đến 100.");
        }
    }
}
