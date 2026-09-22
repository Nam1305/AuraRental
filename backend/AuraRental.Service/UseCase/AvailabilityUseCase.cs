using AuraRental.Service.DTOs.Availability;
using AuraRental.Service.DTOs.Catalog;
using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
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

        var items = await availabilityRepository.FindCandidates(
            requestContext.BranchId,
            query,
            size,
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
                    .Select(item => ToAvailabilityItem(item, startAt, endAt))
                    .OrderByDescending(item => item.AvailableForWholePeriod)
                    .ThenBy(item => item.AssetCode)
                    .ToList();

                return new AvailabilityGroupDto(
                    group.Key.ProductId,
                    group.Key.VariantId,
                    group.Key.Name,
                    group.Key.Size,
                    group.Key.Measurements,
                    inventoryItems.Count(item => item.AvailableForWholePeriod),
                    first.Variant.RentalPrices
                        .OrderBy(price => price.Price)
                        .Select(price => new RentalPriceDto(price.PackageCode, ApiText.PackageLabel(price.PackageCode), price.Price))
                        .ToList(),
                    inventoryItems);
            })
            .ToList();

        return new AvailabilityResultDto(new AvailabilityCriteriaDto(startAt, endAt), groups);
    }

    private static AvailableInventoryItemDto ToAvailabilityItem(
        InventoryItem item,
        DateTimeOffset startAt,
        DateTimeOffset endAt)
    {
        if (item.Status != InventoryStatus.Usable)
        {
            var note = item.Status switch
            {
                InventoryStatus.Maintenance => "Đang bảo trì",
                InventoryStatus.Lost => "Đang thất lạc",
                InventoryStatus.Retired => "Đã ngừng sử dụng",
                _ => "Không sẵn sàng"
            };

            return Unavailable(item, ApiText.EnumValue(item.Status), note);
        }

        var orderConflict = item.OrderItems
            .Select(orderItem => orderItem.Order)
            .Where(order =>
                order.Status != OrderStatus.Completed &&
                order.Status != OrderStatus.Cancelled &&
                order.Reservation.RentalStartAt < endAt &&
                order.Reservation.RentalEndAt > startAt)
            .OrderBy(order => order.Reservation.RentalStartAt)
            .FirstOrDefault();

        if (orderConflict is not null)
        {
            var status = orderConflict.Status == OrderStatus.Renting ? "RENTING" : "ORDERED";
            var note = orderConflict.Status == OrderStatus.Renting ? "Đang cho thuê" : "Đã có đơn trong lịch này";
            return Unavailable(
                item,
                status,
                note,
                orderConflict.Reservation.RentalEndAt,
                orderConflict.OrderNo);
        }

        var reservationConflict = item.ReservationItems
            .Select(reservationItem => reservationItem.Reservation)
            .Where(reservation =>
                (reservation.Status == ReservationStatus.Active || reservation.Status == ReservationStatus.Overdue) &&
                reservation.RentalStartAt < endAt &&
                reservation.RentalEndAt > startAt)
            .OrderBy(reservation => reservation.RentalStartAt)
            .FirstOrDefault();

        if (reservationConflict is not null)
        {
            return Unavailable(
                item,
                "RESERVED",
                "Đã được giữ chỗ trong lịch này",
                reservationConflict.RentalEndAt,
                reservationConflict.ReservationNo);
        }

        return new AvailableInventoryItemDto(
            item.Id,
            item.AssetCode,
            ApiText.EnumValue(item.Status),
            true,
            "AVAILABLE",
            null,
            null,
            null);
    }

    private static AvailableInventoryItemDto Unavailable(
        InventoryItem item,
        string availabilityStatus,
        string note,
        DateTimeOffset? busyUntil = null,
        string? referenceNo = null) =>
        new(
            item.Id,
            item.AssetCode,
            ApiText.EnumValue(item.Status),
            false,
            availabilityStatus,
            note,
            busyUntil,
            referenceNo);

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
