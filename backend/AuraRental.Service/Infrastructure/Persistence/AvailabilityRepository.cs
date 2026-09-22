using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.DTOs.Availability;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class AvailabilityRepository(AuraRentalDbContext context) : IAvailabilityRepository
{
    public async Task<IReadOnlyList<InventoryItem>> FindCandidates(
        Guid branchId,
        string? query,
        string? size,
        int limit,
        CancellationToken cancellationToken)
    {
        var items = context.InventoryItems.Where(item =>
            item.BranchId == branchId &&
            item.Variant.IsActive &&
            item.Variant.Product.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim();
            items = items.Where(item =>
                EF.Functions.ILike(item.AssetCode, $"%{normalizedQuery}%") ||
                EF.Functions.ILike(item.Variant.Product.Name, $"%{normalizedQuery}%") ||
                EF.Functions.ILike(item.Variant.Product.Code, $"%{normalizedQuery}%"));
        }

        if (!string.IsNullOrWhiteSpace(size))
        {
            items = items.Where(item => item.Variant.Size == size.Trim());
        }

        return await items
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Variant)
                .ThenInclude(variant => variant.Product)
            .Include(item => item.Variant)
                .ThenInclude(variant => variant.RentalPrices.Where(price => price.BranchId == branchId))
            .Include(item => item.ReservationItems)
                .ThenInclude(reservationItem => reservationItem.Reservation)
            .Include(item => item.OrderItems)
                .ThenInclude(orderItem => orderItem.Order)
                    .ThenInclude(order => order.Reservation)
            .OrderBy(item => item.Variant.Product.Name)
            .ThenBy(item => item.Variant.Size)
            .ThenBy(item => item.AssetCode)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AreAvailable(
        Guid branchId,
        IReadOnlyCollection<Guid> inventoryItemIds,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        CancellationToken cancellationToken)
    {
        var uniqueIds = inventoryItemIds.Distinct().ToArray();
        if (uniqueIds.Length == 0)
        {
            return false;
        }

        var availableCount = await AvailableQuery(branchId, startAt, endAt)
            .CountAsync(item => uniqueIds.Contains(item.Id), cancellationToken);

        return availableCount == uniqueIds.Length;
    }

    public async Task<IReadOnlyList<InventoryOverviewDto>> GetInventorySummary(
        Guid branchId,
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        var variants = context.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.InventoryItems.Any(item => item.BranchId == branchId));
        if (!string.IsNullOrWhiteSpace(query))
        {
            var value = query.Trim();
            variants = variants.Where(variant =>
                EF.Functions.ILike(variant.Product.Name, $"%{value}%") ||
                EF.Functions.ILike(variant.Product.Code, $"%{value}%"));
        }

        return await variants
            .OrderBy(variant => variant.Product.Name)
            .ThenBy(variant => variant.Size)
            .Take(limit)
            .Select(variant => new InventoryOverviewDto(
                variant.Id,
                variant.Product.Name,
                variant.Size,
                variant.InventoryItems.Count(item => item.BranchId == branchId),
                variant.InventoryItems.Count(item =>
                    item.BranchId == branchId &&
                    item.Status == InventoryStatus.Usable &&
                    !item.ReservationItems.Any(reservationItem =>
                        (reservationItem.Reservation.Status == ReservationStatus.Active ||
                         reservationItem.Reservation.Status == ReservationStatus.Overdue) &&
                        reservationItem.Reservation.RentalStartAt <= DateTimeOffset.UtcNow &&
                        reservationItem.Reservation.RentalEndAt > DateTimeOffset.UtcNow) &&
                    !item.OrderItems.Any(orderItem =>
                        orderItem.Order.Status != OrderStatus.Completed &&
                        orderItem.Order.Status != OrderStatus.Cancelled &&
                        orderItem.Order.Reservation.RentalStartAt <= DateTimeOffset.UtcNow &&
                        orderItem.Order.Reservation.RentalEndAt > DateTimeOffset.UtcNow)),
                variant.InventoryItems.Count(item =>
                    item.BranchId == branchId && item.OrderItems.Any(orderItem => orderItem.Order.Status == OrderStatus.Renting)),
                variant.InventoryItems.Count(item =>
                    item.BranchId == branchId && item.ReservationItems.Any(reservationItem =>
                        reservationItem.Reservation.Status == ReservationStatus.Active ||
                        reservationItem.Reservation.Status == ReservationStatus.Overdue)),
                variant.InventoryItems.Count(item =>
                    item.BranchId == branchId && item.Status == InventoryStatus.Maintenance)))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<InventoryItem> AvailableQuery(
        Guid branchId,
        DateTimeOffset startAt,
        DateTimeOffset endAt) =>
        context.InventoryItems.Where(item =>
            item.BranchId == branchId &&
            item.Status == InventoryStatus.Usable &&
            item.Variant.IsActive &&
            item.Variant.Product.IsActive &&
            !item.ReservationItems.Any(reservationItem =>
                (reservationItem.Reservation.Status == ReservationStatus.Active ||
                 reservationItem.Reservation.Status == ReservationStatus.Overdue) &&
                reservationItem.Reservation.RentalStartAt < endAt &&
                reservationItem.Reservation.RentalEndAt > startAt) &&
            !item.OrderItems.Any(orderItem =>
                orderItem.Order.Status != OrderStatus.Completed &&
                orderItem.Order.Status != OrderStatus.Cancelled &&
                orderItem.Order.Reservation.RentalStartAt < endAt &&
                orderItem.Order.Reservation.RentalEndAt > startAt));
}
