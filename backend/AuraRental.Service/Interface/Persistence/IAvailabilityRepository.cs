using AuraRental.Domain.Entities;
using AuraRental.Service.DTOs.Availability;

namespace AuraRental.Service.Interface.Persistence;

public interface IAvailabilityRepository
{
    Task<IReadOnlyList<InventoryItem>> FindAvailable(
        Guid branchId,
        string? query,
        string? size,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        int limit,
        CancellationToken cancellationToken);

    Task<bool> AreAvailable(
        Guid branchId,
        IReadOnlyCollection<Guid> inventoryItemIds,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryOverviewDto>> GetInventorySummary(
        Guid branchId,
        string? query,
        int limit,
        CancellationToken cancellationToken);
}
