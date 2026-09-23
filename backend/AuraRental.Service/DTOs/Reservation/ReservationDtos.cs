namespace AuraRental.Service.DTOs.Reservation;

public sealed record ReservationItemRequest(int InventoryItemId, string PackageCode);

public sealed record ReceivedPaymentRequest(
    string Type,
    decimal Amount,
    string Method,
    string? TransactionRef,
    string? ProofPath,
    DateTimeOffset PaidAt);

public sealed record CreateReservationRequest(
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    string DepositPlan,
    IReadOnlyList<ReservationItemRequest> Items,
    ReceivedPaymentRequest ReceivedPayment);

public sealed record ReservationBranchDto(int Id, string Code, string Name);
public sealed record ReservationCustomerDto(int Id, string Name, string Phone);

public sealed record ReservationDepositDto(
    string Plan,
    decimal Required,
    decimal ConfirmedReceived,
    decimal Remaining);

public sealed record ReservationItemDto(
    int InventoryItemId,
    string AssetCode,
    string ProductName,
    string Size,
    string PackageCode,
    decimal RentalPrice);

public sealed record CustomerFormCredentialDto(string Url, string Otp, DateTimeOffset ExpiresAt);

public sealed record ReservationDto(
    int Id,
    string ReservationNo,
    string Status,
    ReservationBranchDto Branch,
    ReservationCustomerDto? Customer,
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    ReservationDepositDto Deposit,
    IReadOnlyList<ReservationItemDto> Items,
    CustomerFormCredentialDto? CustomerForm,
    string FormStatus,
    DateTimeOffset CreatedAt);

public sealed record ReservationListItemDto(
    int Id,
    string ReservationNo,
    string Status,
    int? CustomerId,
    string CustomerName,
    string PhoneMasked,
    string ItemSummary,
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    decimal DepositConfirmed,
    decimal DepositRemaining,
    string FormStatus);

public sealed record ReissueOtpRequest(string Reason);
public sealed record ReissueOtpDto(string FormUrl, string Otp, DateTimeOffset ExpiresAt);
public sealed record CancelReservationRequest(string Reason, string PaymentDecision);
public sealed record CancelReservationDto(int ReservationId, string Status, bool RequiresPaymentResolution);

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
    int PaymentId,
    string Status,
    decimal DepositConfirmed,
    decimal DepositRemaining,
    string? OrderStatus);
