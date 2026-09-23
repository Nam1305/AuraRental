namespace AuraRental.Service.DTOs.Order;

public sealed record OrderListItemDto(
    int Id,
    string OrderNo,
    string Status,
    int CustomerId,
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
    int Id,
    string NameSnapshot,
    string PhoneSnapshot,
    string DeliveryAddressSnapshot);

public sealed record OrderItemDto(
    int OrderItemId,
    int InventoryItemId,
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
    int Id,
    string Type,
    decimal Amount,
    string Method,
    string Status,
    string? TransactionRef,
    DateTimeOffset? PaidAt);

public sealed record IdentityVerificationDto(
    bool Required,
    bool Verified,
    int? VerifiedBy,
    DateTimeOffset? VerifiedAt);

public sealed record DeliveryDto(string Status, string? TrackingCode, DateTimeOffset? CompletedAt);

public sealed record OrderDetailDto(
    int Id,
    int ReservationId,
    string OrderNo,
    string Status,
    int BranchId,
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
    int? VerifiedBy,
    DateTimeOffset? VerifiedAt,
    string OrderStatus);

public sealed record CancelOrderRequest(string Reason, string PaymentDecision);
public sealed record CancelOrderDto(int OrderId, string Status, string CancellationReason, bool InventoryReleased, bool RequiresPaymentResolution);

public sealed record StartDeliveryRequest(string? TrackingCode, DateTimeOffset StartedAt);
public sealed record CompleteDeliveryRequest(DateTimeOffset DeliveredAt);
public sealed record CompleteReturnDeliveryRequest(DateTimeOffset ReturnedAt);
public sealed record OrderTransitionDto(int OrderId, string Status, string DeliveryStatus, string ReturnDeliveryStatus);

public sealed record PaymentActionRequest(string? Reason);
public sealed record PaymentActionDto(int PaymentId, string Status, decimal DepositConfirmed, decimal DepositRemaining, string? OrderStatus);
