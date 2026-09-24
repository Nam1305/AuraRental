using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.DTOs.PublicForm;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.Utils;

namespace AuraRental.Service.UseCase;

public sealed class PublicRentalFormUseCase(
    IRentalRepository rentalRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ISecureTokenService secureTokenService,
    IOperationsEventPublisher eventPublisher) : IPublicRentalFormUseCase
{
    public async Task<PublicRentalFormDto> Get(string token, CancellationToken cancellationToken)
    {
        var reservation = await FindByToken(token, cancellationToken);
        EnsureFormCanBeOpened(reservation);
        return ToPublicForm(reservation);
    }

    public async Task<SubmitPublicRentalFormDto> Submit(
        string token,
        SubmitPublicRentalFormRequest request,
        CancellationToken cancellationToken)
    {
        ValidateSubmitRequest(request);
        var tokenHash = secureTokenService.Hash(token);
        var initialReservation = await rentalRepository.GetReservationByFormTokenHash(tokenHash, cancellationToken)
            ?? throw new NotFoundException("RENTAL_FORM_NOT_FOUND", "Link thuê đồ không tồn tại.");

        await using var transaction = await unitOfWork.BeginTransaction(cancellationToken);
        _ = await rentalRepository.LockReservation(initialReservation.Id, cancellationToken)
            ?? throw new NotFoundException("RENTAL_FORM_NOT_FOUND", "Link thuê đồ không tồn tại.");
        var reservation = await rentalRepository.GetReservation(
                initialReservation.Id,
                null,
                true,
                cancellationToken)
            ?? throw new NotFoundException("RENTAL_FORM_NOT_FOUND", "Link thuê đồ không tồn tại.");

        if (reservation.Order is not null)
        {
            await transaction.Commit(cancellationToken);
            return ToSubmitResult(reservation.Order, reservation);
        }

        EnsureFormCanBeOpened(reservation);
        if (reservation.FormTokenHash is null || !secureTokenService.Verify(token, reservation.FormTokenHash))
        {
            throw new NotFoundException("RENTAL_FORM_NOT_FOUND", "Link thuê đồ không tồn tại.");
        }

        if (reservation.OtpHash is null || !secureTokenService.Verify(request.Otp.Trim(), reservation.OtpHash))
        {
            throw new ValidationException("OTP_INVALID", "Mã OTP không đúng.");
        }

        var inventoryIds = reservation.Items.Select(item => item.InventoryItemId).Order().ToArray();
        _ = await rentalRepository.LockInventoryItems(reservation.BranchId, inventoryIds, cancellationToken);
        if (!await rentalRepository.AreInventoryItemsAvailable(
                reservation.BranchId,
                inventoryIds,
                reservation.RentalStartAt,
                reservation.RentalEndAt,
                reservation.Id,
                cancellationToken))
        {
            throw new ConflictException(
                "RESERVATION_NO_LONGER_AVAILABLE",
                "Mã đồ không còn khả dụng. Vui lòng liên hệ shop để được hỗ trợ.");
        }

        var phone = NormalizePhone(request.CustomerPhone);
        var customer = await customerRepository.GetByPhone(phone, cancellationToken);
        if (customer is null)
        {
            customer = new Customer
            {
                Name = request.CustomerName.Trim(),
                Phone = phone,
                InstagramHandle = NormalizeSocialHandle(request.InstagramHandle),
                TiktokHandle = NormalizeSocialHandle(request.TiktokHandle),
                Address = request.DeliveryAddress.Trim()
            };
            await customerRepository.Add(customer, cancellationToken);
            // PostgreSQL assigns the identity key on save. Persist the new customer
            // before using its ID as the required foreign key on the new order.
            // This remains atomic because the surrounding reservation transaction
            // has not committed yet.
            await unitOfWork.SaveChanges(cancellationToken);
        }
        else
        {
            customer.InstagramHandle = NormalizeSocialHandle(request.InstagramHandle) ?? customer.InstagramHandle;
            customer.TiktokHandle = NormalizeSocialHandle(request.TiktokHandle) ?? customer.TiktokHandle;
        }

        var depositRemaining = RentalRules.DepositRemaining(reservation);
        var status = depositRemaining > 0
            ? OrderStatus.PendingDeposit
            : reservation.DepositPlan == "FIFTY_WITH_ID"
                ? OrderStatus.PendingVerification
                : OrderStatus.Confirmed;
        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            OrderNo = RentalRules.CreateNumber(reservation.Branch.Code),
            CustomerId = customer.Id,
            ReservationId = reservation.Id,
            BranchId = reservation.BranchId,
            Status = status,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = phone,
            DeliveryAddress = request.DeliveryAddress.Trim(),
            CreatedAt = now,
            Items = reservation.Items.Select(item => new OrderItem
            {
                InventoryItemId = item.InventoryItemId,
                ProductName = item.InventoryItem.Variant.Product.Name,
                Size = item.InventoryItem.Variant.Size,
                AssetCode = item.InventoryItem.AssetCode,
                PackageCode = item.PackageCode,
                ReplacementValue = item.ReplacementValue,
                RentalPrice = item.RentalPrice,
                OneDayPrice = item.OneDayPrice,
                ExtraDayRate = item.ExtraDayRate
            }).ToList()
        };

        reservation.Status = ReservationStatus.ConvertedToOrder;
        reservation.CustomerId = customer.Id;
        reservation.OtpUsedAt = now;
        reservation.Order = order;
        rentalRepository.AddOrder(order);
        await unitOfWork.SaveChanges(cancellationToken);
        await transaction.Commit(cancellationToken);

        await eventPublisher.PublishOrderCreated(new OrderCreatedEvent(
            "ORDER_CREATED",
            order.BranchId,
            now,
            order.Id,
            order.OrderNo,
            order.CustomerName), cancellationToken);

        return ToSubmitResult(order, reservation);
    }

    private async Task<Reservation> FindByToken(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new NotFoundException("RENTAL_FORM_NOT_FOUND", "Link thuê đồ không tồn tại.");
        }

        return await rentalRepository.GetReservationByFormTokenHash(secureTokenService.Hash(token), cancellationToken)
            ?? throw new NotFoundException("RENTAL_FORM_NOT_FOUND", "Link thuê đồ không tồn tại.");
    }

    private static void EnsureFormCanBeOpened(Reservation reservation)
    {
        if (reservation.Order is not null)
        {
            return;
        }

        if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.ConvertedToOrder)
        {
            throw new ConflictException("RESERVATION_NO_LONGER_AVAILABLE", "Reservation không còn hiệu lực.");
        }

        if (reservation.OtpExpiresAt <= DateTimeOffset.UtcNow || reservation.OtpUsedAt.HasValue)
        {
            throw new GoneException("FORM_EXPIRED", "Link hoặc OTP đã hết hạn. Vui lòng liên hệ shop.");
        }
    }

    private static PublicRentalFormDto ToPublicForm(Reservation reservation)
    {
        var confirmed = RentalRules.ConfirmedDeposit(reservation.Payments);
        return new PublicRentalFormDto(
            reservation.ReservationNo,
            new PublicFormBranchDto(reservation.Branch.Name, reservation.Branch.Address),
            reservation.Items.Select(item => new PublicFormItemDto(
                item.InventoryItem.Variant.Product.Name,
                item.InventoryItem.Variant.Size,
                item.InventoryItem.AssetCode,
                item.PackageCode,
                ApiText.PackageLabel(item.PackageCode),
                RentalRules.RentalFee(item, reservation.RentalStartAt, reservation.RentalEndAt))).ToList(),
            reservation.RentalStartAt,
            reservation.RentalEndAt,
            reservation.DepositPlan,
            confirmed,
            Math.Max(0, reservation.DepositRequired - confirmed),
            true);
    }

    private static SubmitPublicRentalFormDto ToSubmitResult(Order order, Reservation reservation) => new(
        order.Id,
        order.OrderNo,
        ApiText.EnumValue(order.Status),
        reservation.Branch.Name,
        RentalRules.DepositRemaining(reservation),
        "Shop đã nhận thông tin và sẽ liên hệ xác nhận.");

    private static void ValidateSubmitRequest(SubmitPublicRentalFormRequest request)
    {
        if (request.Otp?.Trim().Length != 6 || !request.Otp.All(char.IsDigit))
        {
            throw new ValidationException("OTP_INVALID", "Mã OTP phải gồm 6 chữ số.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new ValidationException("CUSTOMER_NAME_REQUIRED", "Tên khách hàng là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            throw new ValidationException("DELIVERY_ADDRESS_REQUIRED", "Địa chỉ giao nhận là bắt buộc.");
        }
    }

    private static string NormalizePhone(string value)
    {
        try
        {
            return PhoneNumberNormalizer.NormalizeVietnamese(value);
        }
        catch (ArgumentException)
        {
            throw new ValidationException("INVALID_PHONE", "Số điện thoại Việt Nam không hợp lệ.");
        }
    }

    private static string? NormalizeSocialHandle(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimStart('@');
}
