namespace AuraRental.Service.DTOs.PublicForm;

public sealed record PublicFormBranchDto(string Name, string Address);

public sealed record PublicFormItemDto(
    string ProductName,
    string Size,
    string AssetCode,
    string PackageCode,
    string PackageLabel,
    decimal RentalPrice);

public sealed record PublicRentalFormDto(
    string ReservationNo,
    PublicFormBranchDto Branch,
    IReadOnlyList<PublicFormItemDto> Items,
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    string DepositPlan,
    decimal DepositConfirmed,
    decimal DepositRemaining,
    bool OtpRequired);

public sealed record SubmitPublicRentalFormRequest(
    string Otp,
    string CustomerName,
    string CustomerPhone,
    string DeliveryAddress);

public sealed record SubmitPublicRentalFormDto(
    Guid OrderId,
    string OrderNo,
    string Status,
    string BranchName,
    decimal DepositRemaining,
    string Message);
