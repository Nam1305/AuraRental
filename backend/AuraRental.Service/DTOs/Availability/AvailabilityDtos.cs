using AuraRental.Service.DTOs.Catalog;

namespace AuraRental.Service.DTOs.Availability;

public sealed record AvailabilityCriteriaDto(DateTimeOffset StartAt, DateTimeOffset EndAt);

public sealed record AvailableInventoryItemDto(
    Guid InventoryItemId,
    string AssetCode,
    string Status,
    bool AvailableForWholePeriod);

public sealed record AvailabilityGroupDto(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string Size,
    string? Measurements,
    int AvailableCount,
    IReadOnlyList<RentalPriceDto> Prices,
    IReadOnlyList<AvailableInventoryItemDto> Items);

public sealed record AvailabilityResultDto(
    AvailabilityCriteriaDto Criteria,
    IReadOnlyList<AvailabilityGroupDto> Groups);

public sealed record InventoryOverviewDto(
    Guid VariantId,
    string ProductName,
    string Size,
    int Total,
    int UsableNow,
    int Renting,
    int Reserved,
    int Cleaning,
    int Maintenance);
