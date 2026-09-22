using AuraRental.Service.DTOs.Operations;

namespace AuraRental.Service.Interface.UseCase;

public interface IOperationsUseCase
{
    Task<DashboardDto> GetDashboard(DateOnly date, CancellationToken cancellationToken);
    Task<ReportSummaryDto> GetReport(DateOnly from, DateOnly to, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportSummaryDto>> GetBranchReports(DateOnly from, DateOnly to, CancellationToken cancellationToken);
}
