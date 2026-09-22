namespace AuraRental.Service.DTOs.Quote;

public sealed record QuoteItemRequest(Guid InventoryItemId, string PackageCode);

public sealed record CreateQuoteRequest(
    Guid CustomerId,
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    string DepositPlan,
    IReadOnlyList<QuoteItemRequest> Items);

public sealed record QuoteItemDto(
    Guid InventoryItemId,
    string AssetCode,
    string ProductName,
    string Size,
    string PackageCode,
    decimal RentalPrice,
    decimal ReplacementValue);

public sealed record QuoteDto(
    Guid BranchId,
    string Currency,
    IReadOnlyList<QuoteItemDto> Items,
    decimal RentalFee,
    decimal DepositRequired,
    decimal SlotDepositAmount,
    DateTimeOffset ExpiresAt);
