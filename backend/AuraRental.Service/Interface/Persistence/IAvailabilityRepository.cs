using AuraRental.Domain.Entities;
using AuraRental.Service.DTOs.Availability;

namespace AuraRental.Service.Interface.Persistence;

public interface IAvailabilityRepository
{
    Task<IReadOnlyList<InventoryItem>> FindCandidates(
        int branchId,
        string? query,
        string? size,
        int limit,
        CancellationToken cancellationToken);

    Task<bool> AreAvailable(
        int branchId,
        IReadOnlyCollection<int> inventoryItemIds,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryOverviewDto>> GetInventorySummary(
        int branchId,
        string? query,
        int limit,
        CancellationToken cancellationToken);
}
