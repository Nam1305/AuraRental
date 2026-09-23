using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.DTOs.Operations;
using AuraRental.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class OperationsRepository(AuraRentalDbContext context) : IOperationsRepository
{
    public Task<Branch?> GetBranch(Guid branchId, CancellationToken cancellationToken) =>
        context.Branches.AsNoTracking().FirstOrDefaultAsync(branch => branch.Id == branchId, cancellationToken);

    public async Task<DashboardCounts> GetDashboardCounts(
        Guid branchId,
        DateTimeOffset dayStartUtc,
        DateTimeOffset dayEndUtc,
        CancellationToken cancellationToken)
    {
        var pendingDeposit = await context.Orders.CountAsync(
            order => order.BranchId == branchId && order.Status == OrderStatus.PendingDeposit,
            cancellationToken);
        var activeReservations = await context.Reservations.CountAsync(
            reservation => reservation.BranchId == branchId &&
                           reservation.Status == ReservationStatus.Active,
            cancellationToken);
        var returnsDue = await context.Orders.CountAsync(order =>
            order.BranchId == branchId &&
            order.Status == OrderStatus.Renting &&
            order.Reservation.RentalEndAt >= dayStartUtc &&
            order.Reservation.RentalEndAt < dayEndUtc,
            cancellationToken);
        var waitingApproval = await context.Refunds.CountAsync(
            refund => refund.Order.BranchId == branchId && refund.Status == RefundStatus.Submitted,
            cancellationToken);
        return new DashboardCounts(pendingDeposit, activeReservations, returnsDue, waitingApproval);
    }

    public async Task<IReadOnlyList<DashboardTaskDto>> GetDashboardTasks(
        Guid branchId,
        DateTimeOffset dayStartUtc,
        DateTimeOffset dayEndUtc,
        CancellationToken cancellationToken)
    {
        var deliveryTasks = await context.Orders
            .AsNoTracking()
            .Where(order =>
                order.BranchId == branchId &&
                order.Status == OrderStatus.Preparing &&
                order.Reservation.RentalStartAt >= dayStartUtc &&
                order.Reservation.RentalStartAt < dayEndUtc)
            .Select(order => new DashboardTaskDto(
                "DELIVERY",
                order.Id,
                order.CustomerName,
                "Giao đồ",
                order.Reservation.RentalStartAt))
            .ToListAsync(cancellationToken);

        var returnTasks = await context.Orders
            .AsNoTracking()
            .Where(order =>
                order.BranchId == branchId &&
                order.Status == OrderStatus.Renting &&
                order.Reservation.RentalEndAt >= dayStartUtc &&
                order.Reservation.RentalEndAt < dayEndUtc)
            .Select(order => new DashboardTaskDto(
                "RETURN",
                order.Id,
                order.CustomerName,
                "Nhận đồ trả",
                order.Reservation.RentalEndAt))
            .ToListAsync(cancellationToken);

        return deliveryTasks.Concat(returnTasks).OrderBy(task => task.At).Take(20).ToList();
    }

    public async Task<ReportSummaryDto> GetReport(
        Branch branch,
        DateOnly from,
        DateOnly to,
        DateTimeOffset fromUtc,
        DateTimeOffset toExclusiveUtc,
        CancellationToken cancellationToken)
    {
        var periodOrders = context.Orders.Where(order =>
            order.BranchId == branch.Id && order.CreatedAt >= fromUtc && order.CreatedAt < toExclusiveUtc);
        var created = await periodOrders.CountAsync(cancellationToken);
        var completed = await periodOrders.CountAsync(order => order.Status == OrderStatus.Completed, cancellationToken);
        var cancelled = await periodOrders.CountAsync(order => order.Status == OrderStatus.Cancelled, cancellationToken);
        var active = await context.Orders.CountAsync(order =>
            order.BranchId == branch.Id && order.Status != OrderStatus.Completed && order.Status != OrderStatus.Cancelled,
            cancellationToken);

        var depositConfirmed = await context.Payments
            .Where(payment =>
                payment.Reservation.BranchId == branch.Id &&
                payment.Status == PaymentStatus.Confirmed &&
                (payment.Type == PaymentType.SlotDeposit || payment.Type == PaymentType.TargetDeposit) &&
                payment.PaidAt >= fromUtc && payment.PaidAt < toExclusiveUtc)
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0;
        var refundPaid = await MoneyByType(branch.Id, PaymentType.Refund, fromUtc, toExclusiveUtc, cancellationToken);
        var additional = await MoneyByType(branch.Id, PaymentType.AdditionalCollection, fromUtc, toExclusiveUtc, cancellationToken);
        var rentalFee = await context.OrderItems
            .Where(item => item.Order.BranchId == branch.Id && item.Order.SettledAt >= fromUtc && item.Order.SettledAt < toExclusiveUtc)
            .SumAsync(item => (decimal?)(item.ActualRentalFee ?? item.RentalPrice), cancellationToken) ?? 0;
        var processingFee = await context.OrderItems
            .Where(item => item.Order.BranchId == branch.Id && item.Order.SettledAt >= fromUtc && item.Order.SettledAt < toExclusiveUtc)
            .SumAsync(item => (decimal?)item.ProcessingFee, cancellationToken) ?? 0;

        var usable = await context.InventoryItems.CountAsync(
            item => item.BranchId == branch.Id && item.Status == InventoryStatus.Usable,
            cancellationToken);
        var maintenance = await context.InventoryItems.CountAsync(
            item => item.BranchId == branch.Id && item.Status == InventoryStatus.Maintenance,
            cancellationToken);
        var lost = await context.InventoryItems.CountAsync(
            item => item.BranchId == branch.Id && item.Status == InventoryStatus.Lost,
            cancellationToken);
        var totalOperational = usable + maintenance;
        var busy = await context.OrderItems
            .Where(item =>
                item.Order.BranchId == branch.Id &&
                item.Order.Status != OrderStatus.Completed &&
                item.Order.Status != OrderStatus.Cancelled)
            .Select(item => item.InventoryItemId)
            .Distinct()
            .CountAsync(cancellationToken);
        var utilization = totalOperational == 0 ? 0 : Math.Round((decimal)busy / totalOperational, 4);

        return new ReportSummaryDto(
            new ReportPeriodDto(from, to),
            branch.Id,
            branch.Code,
            branch.Name,
            new ReportOrderDto(created, completed, cancelled, active),
            new ReportMoneyDto(depositConfirmed, rentalFee, processingFee, refundPaid, additional),
            new ReportInventoryDto(usable, maintenance, lost, utilization));
    }

    private async Task<decimal> MoneyByType(
        Guid branchId,
        PaymentType type,
        DateTimeOffset fromUtc,
        DateTimeOffset toExclusiveUtc,
        CancellationToken cancellationToken) =>
        await context.Payments
            .Where(payment =>
                payment.Reservation.BranchId == branchId &&
                payment.Type == type &&
                payment.Status == PaymentStatus.Confirmed &&
                payment.PaidAt >= fromUtc && payment.PaidAt < toExclusiveUtc)
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0;
}
