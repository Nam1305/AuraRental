using AuraRental.Service.DTOs.Catalog;

namespace AuraRental.Service.DTOs.Availability;

public sealed record AvailabilityCriteriaDto(DateTimeOffset StartAt, DateTimeOffset EndAt);

public sealed record AvailableInventoryItemDto(
    int InventoryItemId,
    string AssetCode,
    string Status,
    bool AvailableForWholePeriod,
    string AvailabilityStatus,
    string? AvailabilityNote,
    DateTimeOffset? BusyUntil,
    string? ReferenceNo);

public sealed record AvailabilityGroupDto(
    int ProductId,
    int VariantId,
    string ProductName,
    IReadOnlyList<string> ImagePaths,
    string Size,
    string? Measurements,
    int AvailableCount,
    IReadOnlyList<RentalPriceDto> Prices,
    IReadOnlyList<AvailableInventoryItemDto> Items);

public sealed record AvailabilityResultDto(
    AvailabilityCriteriaDto Criteria,
    IReadOnlyList<AvailabilityGroupDto> Groups);

public sealed record InventoryOverviewDto(
    int VariantId,
    string ProductName,
    string Size,
    int Total,
    int UsableNow,
    int Renting,
    int Reserved,
    int Maintenance);
