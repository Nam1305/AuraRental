using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class ReturnRepository(AuraRentalDbContext context) : IReturnRepository
{
    public async Task<IReadOnlyList<Order>> GetQueue(
        int branchId,
        IReadOnlyCollection<OrderStatus> statuses,
        int limit,
        CancellationToken cancellationToken)
    {
        var orders = context.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.Items)
            .Include(order => order.Refunds)
            .Where(order => order.BranchId == branchId);
        if (statuses.Count > 0)
        {
            orders = orders.Where(order => statuses.Contains(order.Status));
        }
        else
        {
            orders = orders.Where(order => order.Status == OrderStatus.Inspecting);
        }

        return await orders.OrderBy(order => order.ReturnedAt).Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<Refund?> LockRefund(int refundId, CancellationToken cancellationToken) =>
        await context.Refunds
            .FromSqlInterpolated($"SELECT * FROM aura.refunds WHERE id = {refundId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Refund?> GetRefund(int refundId, int branchId, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Refund> refunds = context.Refunds;
        if (!tracking)
        {
            refunds = refunds.AsNoTracking();
        }

        return refunds
            .Include(refund => refund.Order)
                .ThenInclude(order => order.Branch)
            .Include(refund => refund.Order)
                .ThenInclude(order => order.Reservation)
                    .ThenInclude(reservation => reservation.Payments)
            .Include(refund => refund.Order)
                .ThenInclude(order => order.Items)
                    .ThenInclude(item => item.InventoryItem)
            .SingleOrDefaultAsync(refund => refund.Id == refundId && refund.Order.BranchId == branchId, cancellationToken);
    }

    public Task<Refund?> GetOpenRefundForOrder(int orderId, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Refund> refunds = context.Refunds;
        if (!tracking)
        {
            refunds = refunds.AsNoTracking();
        }

        return refunds
            .Where(refund => refund.OrderId == orderId &&
                             (refund.Status == RefundStatus.Draft || refund.Status == RefundStatus.Submitted))
            .OrderByDescending(refund => refund.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetLatestVersion(int orderId, CancellationToken cancellationToken) =>
        await context.Refunds
            .Where(refund => refund.OrderId == orderId)
            .Select(refund => (int?)refund.Version)
            .MaxAsync(cancellationToken) ?? 0;

    public Task<bool> HasNewerOpenRevision(int orderId, int version, CancellationToken cancellationToken) =>
        context.Refunds.AnyAsync(
            refund => refund.OrderId == orderId &&
                      refund.Version > version &&
                      (refund.Status == RefundStatus.Draft || refund.Status == RefundStatus.Submitted),
            cancellationToken);

    public async Task<IReadOnlyList<Refund>> GetApprovedRefunds(int orderId, CancellationToken cancellationToken) =>
        await context.Refunds
            .Where(refund => refund.OrderId == orderId && refund.Status == RefundStatus.Approved)
            .ToListAsync(cancellationToken);

    public void AddRefund(Refund refund) => context.Refunds.Add(refund);
}
