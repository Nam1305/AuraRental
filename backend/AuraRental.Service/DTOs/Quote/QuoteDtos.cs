namespace AuraRental.Service.DTOs.Quote;

public sealed record QuoteItemRequest(int InventoryItemId, string PackageCode);

public sealed record CreateQuoteRequest(
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    string DepositPlan,
    IReadOnlyList<QuoteItemRequest> Items);

public sealed record QuoteItemDto(
    int InventoryItemId,
    string AssetCode,
    string ProductName,
    string Size,
    string PackageCode,
    decimal RentalPrice,
    decimal ReplacementValue);

public sealed record QuoteDto(
    int BranchId,
    string Currency,
    IReadOnlyList<QuoteItemDto> Items,
    decimal RentalFee,
    decimal DepositRequired,
    decimal SlotDepositAmount,
    DateTimeOffset ExpiresAt);
