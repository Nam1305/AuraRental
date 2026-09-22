using AuraRental.Service.DTOs.Availability;
using AuraRental.Service.DTOs.Catalog;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.Utils;

namespace AuraRental.Service.UseCase;

public sealed class AvailabilityUseCase(
    IAvailabilityRepository availabilityRepository,
    IRequestContext requestContext) : IAvailabilityUseCase
{
    private static readonly TimeSpan MaximumRentalPeriod = TimeSpan.FromDays(30);

    public async Task<AvailabilityResultDto> Search(
        string? query,
        string? size,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        int limit,
        CancellationToken cancellationToken)
    {
        if (endAt <= startAt)
        {
            throw new ValidationException("INVALID_TIME_RANGE", "Thời gian trả phải sau thời gian nhận.");
        }

        if (endAt - startAt > MaximumRentalPeriod)
        {
            throw new ValidationException("RENTAL_PERIOD_TOO_LONG", "Khoảng thuê tối đa là 30 ngày.");
        }

        if (limit is < 1 or > 100)
        {
            throw new ValidationException("INVALID_LIMIT", "Limit phải nằm trong khoảng 1 đến 100.");
        }

        var items = await availabilityRepository.FindAvailable(
            requestContext.BranchId,
            query,
            size,
            startAt,
            endAt,
            limit,
            cancellationToken);

        var groups = items
            .GroupBy(item => new
            {
                item.Variant.ProductId,
                VariantId = item.VariantId,
                item.Variant.Product.Name,
                item.Variant.Size,
                item.Variant.Measurements
            })
            .Select(group =>
            {
                var first = group.First();
                var inventoryItems = group
                    .Select(item => new AvailableInventoryItemDto(
                        item.Id,
                        item.AssetCode,
                        ApiText.EnumValue(item.Status),
                        true))
                    .ToList();

                return new AvailabilityGroupDto(
                    group.Key.ProductId,
                    group.Key.VariantId,
                    group.Key.Name,
                    group.Key.Size,
                    group.Key.Measurements,
                    inventoryItems.Count,
                    first.Variant.RentalPrices
                        .OrderBy(price => price.Price)
                        .Select(price => new RentalPriceDto(price.PackageCode, ApiText.PackageLabel(price.PackageCode), price.Price))
                        .ToList(),
                    inventoryItems);
            })
            .ToList();

        return new AvailabilityResultDto(new AvailabilityCriteriaDto(startAt, endAt), groups);
    }

    public async Task<IReadOnlyList<InventoryOverviewDto>> GetInventorySummary(
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100)
        {
            throw new ValidationException("INVALID_LIMIT", "Limit phải nằm trong khoảng 1 đến 100.");
        }

        return await availabilityRepository.GetInventorySummary(
            requestContext.BranchId,
            query,
            limit,
            cancellationToken);
    }
}
