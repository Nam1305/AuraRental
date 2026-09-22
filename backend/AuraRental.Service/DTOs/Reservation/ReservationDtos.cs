namespace AuraRental.Service.DTOs.Reservation;

public sealed record ReservationItemRequest(Guid InventoryItemId, string PackageCode);

public sealed record ReceivedPaymentRequest(
    string Type,
    decimal Amount,
    string Method,
    string? TransactionRef,
    string? ProofPath,
    DateTimeOffset PaidAt);

public sealed record CreateReservationRequest(
    Guid CustomerId,
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    string DepositPlan,
    DateTimeOffset? DepositDeadlineAt,
    IReadOnlyList<ReservationItemRequest> Items,
    ReceivedPaymentRequest ReceivedPayment);

public sealed record ReservationBranchDto(Guid Id, string Code, string Name);
public sealed record ReservationCustomerDto(Guid Id, string Name, string Phone);

public sealed record ReservationDepositDto(
    string Plan,
    decimal Required,
    decimal ConfirmedReceived,
    decimal Remaining,
    DateTimeOffset? DeadlineAt);

public sealed record ReservationItemDto(
    Guid InventoryItemId,
    string AssetCode,
    string ProductName,
    string Size,
    string PackageCode,
    decimal RentalPrice);

public sealed record CustomerFormCredentialDto(string Url, string Otp, DateTimeOffset ExpiresAt);

public sealed record ReservationDto(
    Guid Id,
    string ReservationNo,
    string Status,
    ReservationBranchDto Branch,
    ReservationCustomerDto Customer,
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    ReservationDepositDto Deposit,
    IReadOnlyList<ReservationItemDto> Items,
    CustomerFormCredentialDto? CustomerForm,
    string FormStatus,
    DateTimeOffset CreatedAt);

public sealed record ReservationListItemDto(
    Guid Id,
    string ReservationNo,
    string Status,
    Guid CustomerId,
    string CustomerName,
    string PhoneMasked,
    string ItemSummary,
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    decimal DepositConfirmed,
    decimal DepositRemaining,
    DateTimeOffset? DepositDeadlineAt,
    string FormStatus);

public sealed record ReissueOtpRequest(string Reason);
public sealed record ReissueOtpDto(string FormUrl, string Otp, DateTimeOffset ExpiresAt);
public sealed record ExtendReservationDeadlineRequest(DateTimeOffset DepositDeadlineAt, string Reason);
public sealed record CancelReservationRequest(string Reason, string PaymentDecision);
public sealed record CancelReservationDto(Guid ReservationId, string Status, bool RequiresPaymentResolution);

public sealed record UpdateRentalSelectionRequest(
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    IReadOnlyList<ReservationItemRequest> Items,
    string Reason);

public sealed record RecordPaymentRequest(
    string Type,
    decimal Amount,
    string Method,
    string? TransactionRef,
    string? ProofPath,
    DateTimeOffset PaidAt,
    bool ConfirmNow,
    string? Note);

public sealed record RecordPaymentDto(
    Guid PaymentId,
    string Status,
    decimal DepositConfirmed,
    decimal DepositRemaining,
    string? OrderStatus);
