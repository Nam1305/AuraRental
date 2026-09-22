namespace AuraRental.Service.DTOs.Order;

public sealed record OrderListItemDto(
    Guid Id,
    string OrderNo,
    string Status,
    Guid CustomerId,
    string CustomerName,
    string PhoneMasked,
    string ItemSummary,
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    decimal DepositRequired,
    decimal DepositConfirmed,
    decimal DepositRemaining,
    DateTimeOffset CreatedAt);

public sealed record OrderCustomerDto(
    Guid Id,
    string NameSnapshot,
    string PhoneSnapshot,
    string DeliveryAddressSnapshot);

public sealed record OrderItemDto(
    Guid OrderItemId,
    Guid InventoryItemId,
    string ProductName,
    string Size,
    string AssetCode,
    string PackageCode,
    decimal RentalPrice,
    string? Condition,
    decimal ProcessingFee,
    string? DamageNote,
    IReadOnlyList<string> DamagePhotoPaths);

public sealed record OrderPaymentDto(
    Guid Id,
    string Type,
    decimal Amount,
    string Method,
    string Status,
    string? TransactionRef,
    DateTimeOffset? PaidAt);

public sealed record IdentityVerificationDto(
    bool Required,
    bool Verified,
    Guid? VerifiedBy,
    DateTimeOffset? VerifiedAt);

public sealed record DeliveryDto(string Status, string? TrackingCode, DateTimeOffset? CompletedAt);

public sealed record OrderDetailDto(
    Guid Id,
    string OrderNo,
    string Status,
    Guid BranchId,
    string BranchCode,
    string BranchName,
    OrderCustomerDto Customer,
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    IReadOnlyList<OrderItemDto> Items,
    decimal DepositRequired,
    decimal DepositConfirmed,
    decimal DepositRemaining,
    IdentityVerificationDto IdentityVerification,
    DeliveryDto Delivery,
    DeliveryDto ReturnDelivery,
    IReadOnlyList<OrderPaymentDto> Payments,
    IReadOnlyList<string> AllowedActions);

public sealed record VerifyIdentityRequest(bool Verified);

public sealed record VerifyIdentityDto(
    bool Verified,
    Guid? VerifiedBy,
    DateTimeOffset? VerifiedAt,
    string OrderStatus);

public sealed record CancelOrderRequest(string Reason, string PaymentDecision);
public sealed record CancelOrderDto(Guid OrderId, string Status, string CancellationReason, bool InventoryReleased, bool RequiresPaymentResolution);

public sealed record StartDeliveryRequest(string? TrackingCode, DateTimeOffset StartedAt);
public sealed record CompleteDeliveryRequest(DateTimeOffset DeliveredAt);
public sealed record CompleteReturnDeliveryRequest(DateTimeOffset ReturnedAt);
public sealed record OrderTransitionDto(Guid OrderId, string Status, string DeliveryStatus, string ReturnDeliveryStatus);

public sealed record PaymentActionRequest(string? Reason);
public sealed record PaymentActionDto(Guid PaymentId, string Status, decimal DepositConfirmed, decimal DepositRemaining, string? OrderStatus);
