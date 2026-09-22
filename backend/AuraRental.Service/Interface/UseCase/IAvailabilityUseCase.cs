using AuraRental.Service.DTOs.Availability;

namespace AuraRental.Service.Interface.UseCase;

public interface IAvailabilityUseCase
{
    Task<AvailabilityResultDto> Search(
        string? query,
        string? size,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        int limit,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryOverviewDto>> GetInventorySummary(
        string? query,
        int limit,
        CancellationToken cancellationToken);
}
