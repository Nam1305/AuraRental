using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class RentalRepository(AuraRentalDbContext context) : IRentalRepository
{
    public Task<Branch?> GetBranch(Guid branchId, CancellationToken cancellationToken) =>
        context.Branches.AsNoTracking().FirstOrDefaultAsync(branch => branch.Id == branchId && branch.IsActive, cancellationToken);

    public Task<Customer?> GetCustomer(Guid customerId, CancellationToken cancellationToken) =>
        context.Customers.AsNoTracking().FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);

    public async Task<IReadOnlyList<InventoryItem>> LockInventoryItems(
        Guid branchId,
        IReadOnlyCollection<Guid> inventoryItemIds,
        CancellationToken cancellationToken)
    {
        var ids = inventoryItemIds.Distinct().Order().ToArray();
        var lockedItems = await context.InventoryItems
            .FromSqlInterpolated($"SELECT * FROM aura.inventory_items WHERE branch_id = {branchId} AND id = ANY({ids}) ORDER BY id FOR UPDATE")
            .ToListAsync(cancellationToken);

        var variantIds = lockedItems.Select(item => item.VariantId).Distinct().ToArray();
        _ = await context.ProductVariants
            .Include(variant => variant.Product)
            .Include(variant => variant.RentalPrices.Where(price => price.BranchId == branchId))
            .Where(variant => variantIds.Contains(variant.Id))
            .ToListAsync(cancellationToken);

        return lockedItems;
    }

    public async Task<bool> AreInventoryItemsAvailable(
        Guid branchId,
        IReadOnlyCollection<Guid> inventoryItemIds,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Guid? ignoredReservationId,
        CancellationToken cancellationToken)
    {
        var ids = inventoryItemIds.Distinct().ToArray();
        var count = await context.InventoryItems.CountAsync(item =>
            ids.Contains(item.Id) &&
            item.BranchId == branchId &&
            item.Status == InventoryStatus.Usable &&
            item.Variant.IsActive &&
            item.Variant.Product.IsActive &&
            (item.CleaningUntil == null || item.CleaningUntil <= startAt) &&
            !item.ReservationItems.Any(reservationItem =>
                reservationItem.ReservationId != ignoredReservationId &&
                (reservationItem.Reservation.Status == ReservationStatus.Active ||
                 reservationItem.Reservation.Status == ReservationStatus.Overdue) &&
                reservationItem.Reservation.RentalStartAt < endAt &&
                reservationItem.Reservation.RentalEndAt.AddHours(item.CleaningHours) > startAt) &&
            !item.OrderItems.Any(orderItem =>
                orderItem.Order.ReservationId != ignoredReservationId &&
                orderItem.Order.Status != OrderStatus.Completed &&
                orderItem.Order.Status != OrderStatus.Cancelled &&
                orderItem.Order.Reservation.RentalStartAt < endAt &&
                orderItem.Order.Reservation.RentalEndAt.AddHours(item.CleaningHours) > startAt),
            cancellationToken);

        return count == ids.Length;
    }

    public void AddReservation(Reservation reservation) => context.Reservations.Add(reservation);

    public async Task<Reservation?> LockReservation(Guid reservationId, CancellationToken cancellationToken) =>
        await context.Reservations
            .FromSqlInterpolated($"SELECT * FROM aura.reservations WHERE id = {reservationId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Reservation?> GetReservation(
        Guid reservationId,
        Guid? branchId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var query = ReservationGraph(tracking)
            .Where(reservation => reservation.Id == reservationId);
        if (branchId.HasValue)
        {
            query = query.Where(reservation => reservation.BranchId == branchId.Value);
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Reservation?> GetReservationByFormTokenHash(string tokenHash, CancellationToken cancellationToken) =>
        ReservationGraph(false)
            .SingleOrDefaultAsync(reservation => reservation.FormTokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<Reservation>> SearchReservations(
        Guid branchId,
        IReadOnlyCollection<ReservationStatus> statuses,
        DateTimeOffset? deadlineTo,
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        var reservations = ReservationGraph(false).Where(reservation => reservation.BranchId == branchId);
        if (statuses.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            var includeActive = statuses.Contains(ReservationStatus.Active);
            var includeOverdue = statuses.Contains(ReservationStatus.Overdue);
            var remainingStatuses = statuses
                .Where(status => status is not (ReservationStatus.Active or ReservationStatus.Overdue))
                .ToArray();
            reservations = reservations.Where(reservation =>
                remainingStatuses.Contains(reservation.Status) ||
                (includeActive &&
                 reservation.Status == ReservationStatus.Active &&
                 !(reservation.DepositDeadlineAt < now &&
                   reservation.DepositRequired > reservation.Payments
                       .Where(payment => payment.Status == PaymentStatus.Confirmed &&
                                         (payment.Type == PaymentType.SlotDeposit || payment.Type == PaymentType.TargetDeposit))
                       .Sum(payment => payment.Amount))) ||
                (includeOverdue &&
                 (reservation.Status == ReservationStatus.Overdue ||
                  (reservation.Status == ReservationStatus.Active &&
                   reservation.DepositDeadlineAt < now &&
                   reservation.DepositRequired > reservation.Payments
                       .Where(payment => payment.Status == PaymentStatus.Confirmed &&
                                         (payment.Type == PaymentType.SlotDeposit || payment.Type == PaymentType.TargetDeposit))
                       .Sum(payment => payment.Amount)))));
        }

        if (deadlineTo.HasValue)
        {
            reservations = reservations.Where(reservation => reservation.DepositDeadlineAt <= deadlineTo);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var value = query.Trim();
            reservations = reservations.Where(reservation =>
                EF.Functions.ILike(reservation.ReservationNo, $"%{value}%") ||
                EF.Functions.ILike(reservation.Customer.Name, $"%{value}%") ||
                reservation.Customer.Phone.Contains(value));
        }

        return await reservations.OrderByDescending(reservation => reservation.CreatedAt).Take(limit).ToListAsync(cancellationToken);
    }

    public void AddPayment(Payment payment) => context.Payments.Add(payment);

    public Task<Payment?> GetPayment(Guid paymentId, Guid branchId, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Payment> payments = context.Payments;
        if (!tracking)
        {
            payments = payments.AsNoTracking();
        }

        return payments
            .Include(payment => payment.Reservation)
                .ThenInclude(reservation => reservation.Order)
            .Include(payment => payment.Reservation)
                .ThenInclude(reservation => reservation.Payments)
            .SingleOrDefaultAsync(
                payment => payment.Id == paymentId && payment.Reservation.BranchId == branchId,
                cancellationToken);
    }

    public void AddOrder(Order order) => context.Orders.Add(order);

    public async Task<Order?> LockOrder(Guid orderId, CancellationToken cancellationToken) =>
        await context.Orders
            .FromSqlInterpolated($"SELECT * FROM aura.orders WHERE id = {orderId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Order?> GetOrder(Guid orderId, Guid branchId, bool tracking, CancellationToken cancellationToken) =>
        OrderGraph(tracking).SingleOrDefaultAsync(
            order => order.Id == orderId && order.BranchId == branchId,
            cancellationToken);

    public Task<Order?> GetOrderByReservationId(Guid reservationId, bool tracking, CancellationToken cancellationToken) =>
        OrderGraph(tracking).SingleOrDefaultAsync(order => order.ReservationId == reservationId, cancellationToken);

    public async Task<IReadOnlyList<Order>> SearchOrders(
        Guid branchId,
        IReadOnlyCollection<OrderStatus> statuses,
        string? query,
        DateOnly? from,
        DateOnly? to,
        int limit,
        CancellationToken cancellationToken)
    {
        var orders = OrderGraph(false).Where(order => order.BranchId == branchId);
        if (statuses.Count > 0)
        {
            orders = orders.Where(order => statuses.Contains(order.Status));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var value = query.Trim();
            orders = orders.Where(order =>
                EF.Functions.ILike(order.OrderNo, $"%{value}%") ||
                EF.Functions.ILike(order.CustomerName, $"%{value}%") ||
                order.CustomerPhone.Contains(value));
        }

        if (from.HasValue)
        {
            var fromUtc = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUniversalTime();
            orders = orders.Where(order => order.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toExclusiveUtc = new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUniversalTime();
            orders = orders.Where(order => order.CreatedAt < toExclusiveUtc);
        }

        return await orders.OrderByDescending(order => order.CreatedAt).Take(limit).ToListAsync(cancellationToken);
    }

    private IQueryable<Reservation> ReservationGraph(bool tracking)
    {
        IQueryable<Reservation> query = context.Reservations;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query
            .AsSplitQuery()
            .Include(reservation => reservation.Branch)
            .Include(reservation => reservation.Customer)
            .Include(reservation => reservation.Payments)
            .Include(reservation => reservation.Items)
                .ThenInclude(item => item.InventoryItem)
                    .ThenInclude(item => item.Variant)
                        .ThenInclude(variant => variant.Product)
            .Include(reservation => reservation.Order);
    }

    private IQueryable<Order> OrderGraph(bool tracking)
    {
        IQueryable<Order> query = context.Orders;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query
            .AsSplitQuery()
            .Include(order => order.Branch)
            .Include(order => order.Reservation)
                .ThenInclude(reservation => reservation.Payments)
            .Include(order => order.Items)
                .ThenInclude(item => item.InventoryItem)
            .Include(order => order.Refunds);
    }
}
