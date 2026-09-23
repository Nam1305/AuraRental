using AuraRental.Domain.Enums;
using AuraRental.Service.DTOs.Operations;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;

namespace AuraRental.Service.UseCase;

public sealed class OperationsUseCase(
    IOperationsRepository operationsRepository,
    IUserRepository userRepository,
    IRequestContext requestContext) : IOperationsUseCase
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public async Task<DashboardDto> GetDashboard(DateOnly date, CancellationToken cancellationToken)
    {
        var branch = await operationsRepository.GetBranch(requestContext.BranchId, cancellationToken)
            ?? throw new NotFoundException("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh.");
        var (fromUtc, toUtc) = ToUtcRange(date, date);
        var counts = await operationsRepository.GetDashboardCounts(
            branch.Id,
            fromUtc,
            toUtc,
            cancellationToken);
        var tasks = await operationsRepository.GetDashboardTasks(branch.Id, fromUtc, toUtc, cancellationToken);
        return new DashboardDto(
            date,
            branch.Id,
            branch.Code,
            branch.Name,
            new DashboardCountersDto(
                counts.PendingDeposit,
                counts.ActiveReservations,
                counts.ReturnsDue,
                counts.RefundsWaitingApproval),
            tasks.FirstOrDefault(),
            tasks);
    }

    public async Task<ReportSummaryDto> GetReport(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(from, to);
        var branch = await operationsRepository.GetBranch(requestContext.BranchId, cancellationToken)
            ?? throw new NotFoundException("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh.");
        var (fromUtc, toUtc) = ToUtcRange(from, to);
        return await operationsRepository.GetReport(branch, from, to, fromUtc, toUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<ReportSummaryDto>> GetBranchReports(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        if (requestContext.Role != UserRole.Manager)
        {
            throw new ForbiddenException("MANAGER_REQUIRED", "Chỉ manager được xem báo cáo nhiều chi nhánh.");
        }

        ValidatePeriod(from, to);
        var branches = await userRepository.GetAllBranches(cancellationToken);
        var (fromUtc, toUtc) = ToUtcRange(from, to);
        var reports = new List<ReportSummaryDto>(branches.Count);
        foreach (var branch in branches)
        {
            reports.Add(await operationsRepository.GetReport(branch, from, to, fromUtc, toUtc, cancellationToken));
        }

        return reports;
    }

    private static (DateTimeOffset FromUtc, DateTimeOffset ToExclusiveUtc) ToUtcRange(DateOnly from, DateOnly to)
    {
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), VietnamOffset).ToUniversalTime();
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), VietnamOffset).ToUniversalTime();
        return (start, end);
    }

    private static void ValidatePeriod(DateOnly from, DateOnly to)
    {
        if (to < from || to.DayNumber - from.DayNumber > 366)
        {
            throw new ValidationException("INVALID_REPORT_PERIOD", "Khoảng báo cáo không hợp lệ hoặc vượt quá 366 ngày.");
        }
    }
}
