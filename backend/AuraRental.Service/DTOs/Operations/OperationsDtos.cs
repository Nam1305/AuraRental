namespace AuraRental.Service.DTOs.Operations;

public sealed record DashboardCountersDto(
    int PendingDeposit,
    int ActiveReservations,
    int ReturnsDue,
    int RefundsWaitingApproval);

public sealed record DashboardTaskDto(
    string Type,
    Guid ReferenceId,
    string CustomerName,
    string Label,
    DateTimeOffset? At);

public sealed record DashboardDto(
    DateOnly Date,
    Guid BranchId,
    string BranchCode,
    string BranchName,
    DashboardCountersDto Counters,
    DashboardTaskDto? NextTask,
    IReadOnlyList<DashboardTaskDto> Tasks);

public sealed record ReportPeriodDto(DateOnly From, DateOnly To);
public sealed record ReportOrderDto(int Created, int Completed, int Cancelled, int Active);
public sealed record ReportMoneyDto(
    decimal DepositConfirmed,
    decimal RentalFee,
    decimal ProcessingFee,
    decimal RefundPaid,
    decimal AdditionalCollection);
public sealed record ReportInventoryDto(int Usable, int Maintenance, int Lost, decimal UtilizationRate);

public sealed record ReportSummaryDto(
    ReportPeriodDto Period,
    Guid BranchId,
    string BranchCode,
    string BranchName,
    ReportOrderDto Orders,
    ReportMoneyDto Money,
    ReportInventoryDto Inventory);
