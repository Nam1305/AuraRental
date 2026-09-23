namespace AuraRental.Service.DTOs.Catalog;

public sealed record RentalPriceDto(string PackageCode, string Label, decimal Price);

public sealed record ProductListItemDto(
    int Id,
    string Code,
    string Name,
    string Category,
    bool IsActive,
    string? CoverImagePath,
    IReadOnlyList<string> Sizes,
    int ActiveInventoryCount,
    int AvailableNowCount,
    decimal? PriceFrom,
    IReadOnlyList<string> PackageCodes);

public sealed record InventorySummaryDto(int Total, int Usable, int Maintenance, int Lost, int Retired);

public sealed record ProductVariantDto(
    int Id,
    string Size,
    string? Measurements,
    decimal ReplacementValue,
    IReadOnlyList<RentalPriceDto> Prices,
    InventorySummaryDto InventorySummary,
    IReadOnlyList<InventoryItemDto> InventoryItems);

public sealed record ProductDetailDto(
    int Id,
    string Code,
    string Name,
    string Category,
    string? Color,
    string? Material,
    string? Description,
    IReadOnlyList<string> ImagePaths,
    bool IsActive,
    IReadOnlyList<ProductVariantDto> Variants);

public sealed record RentalPriceInput(string PackageCode, decimal Price);
public sealed record InventoryItemInput(string AssetCode);

public sealed record CreateProductVariantInput(
    string Size,
    string? Measurements,
    decimal ReplacementValue,
    IReadOnlyList<RentalPriceInput> Prices,
    IReadOnlyList<InventoryItemInput> InventoryItems);

public sealed record CreateProductRequest(
    string Code,
    string Name,
    string Category,
    string? Color,
    string? Material,
    string? Description,
    IReadOnlyList<string> ImagePaths,
    IReadOnlyList<CreateProductVariantInput> Variants);

public sealed record UpdateProductRequest(
    string Name,
    string Category,
    string? Color,
    string? Material,
    string? Description,
    IReadOnlyList<string> ImagePaths,
    bool IsActive);

public sealed record ReplaceRentalPricesRequest(IReadOnlyList<RentalPriceInput> Prices);
public sealed record RentalPriceSetDto(
    int VariantId,
    int BranchId,
    IReadOnlyList<RentalPriceDto> Prices,
    bool EffectiveForNewReservationsOnly);

public sealed record AddInventoryItemsRequest(IReadOnlyList<InventoryItemInput> Items);
public sealed record InventoryItemDto(int Id, string AssetCode, string Status);
public sealed record UpdateInventoryItemRequest(string Status);
