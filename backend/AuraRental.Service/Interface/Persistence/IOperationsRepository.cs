using AuraRental.Domain.Entities;
using AuraRental.Service.DTOs.Operations;

namespace AuraRental.Service.Interface.Persistence;

public sealed record DashboardCounts(int PendingDeposit, int ActiveReservations, int ReturnsDue, int RefundsWaitingApproval);

public interface IOperationsRepository
{
    Task<Branch?> GetBranch(Guid branchId, CancellationToken cancellationToken);
    Task<DashboardCounts> GetDashboardCounts(
        Guid branchId,
        DateTimeOffset dayStartUtc,
        DateTimeOffset dayEndUtc,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardTaskDto>> GetDashboardTasks(
        Guid branchId,
        DateTimeOffset dayStartUtc,
        DateTimeOffset dayEndUtc,
        CancellationToken cancellationToken);
    Task<ReportSummaryDto> GetReport(
        Branch branch,
        DateOnly from,
        DateOnly to,
        DateTimeOffset fromUtc,
        DateTimeOffset toExclusiveUtc,
        CancellationToken cancellationToken);
}
