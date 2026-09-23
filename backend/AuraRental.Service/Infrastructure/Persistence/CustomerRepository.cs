using AuraRental.Domain.Entities;
using AuraRental.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class CustomerRepository(AuraRentalDbContext context) : ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> Search(
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        var customers = context.Customers
            .AsNoTracking()
            .Include(customer => customer.Orders)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim();
            customers = customers.Where(customer =>
                EF.Functions.ILike(customer.Name, $"%{normalizedQuery}%") ||
                customer.Phone.Contains(normalizedQuery) ||
                (customer.InstagramHandle != null && EF.Functions.ILike(customer.InstagramHandle, $"%{normalizedQuery}%")));
        }

        return await customers
            .OrderBy(customer => customer.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<Customer?> Get(int customerId, CancellationToken cancellationToken) =>
        context.Customers.FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);

    public Task<Customer?> GetByPhone(string normalizedPhone, CancellationToken cancellationToken) =>
        context.Customers.AsNoTracking().FirstOrDefaultAsync(customer => customer.Phone == normalizedPhone, cancellationToken);

    public Task Add(Customer customer, CancellationToken cancellationToken) =>
        context.Customers.AddAsync(customer, cancellationToken).AsTask();

    public async Task SaveChanges(CancellationToken cancellationToken) =>
        _ = await context.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetOrderHistory(
        int customerId,
        int userId,
        string? status,
        int limit,
        CancellationToken cancellationToken)
    {
        var orders = context.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.Branch)
            .Include(order => order.Reservation)
            .Include(order => order.Items)
            .Include(order => order.Refunds)
            .Where(order =>
                order.CustomerId == customerId &&
                order.Branch.UserBranches.Any(access => access.UserId == userId));

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AuraRental.Domain.Enums.OrderStatus>(status.Replace("_", string.Empty, StringComparison.Ordinal), true, out var parsedStatus))
        {
            orders = orders.Where(order => order.Status == parsedStatus);
        }

        return await orders
            .OrderByDescending(order => order.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
