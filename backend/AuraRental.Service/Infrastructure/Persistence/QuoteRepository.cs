using AuraRental.Domain.Entities;
using AuraRental.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class QuoteRepository(AuraRentalDbContext context) : IQuoteRepository
{
    public Task<bool> CustomerExists(int customerId, CancellationToken cancellationToken) =>
        context.Customers.AnyAsync(customer => customer.Id == customerId, cancellationToken);

    public async Task<IReadOnlyList<InventoryItem>> GetInventoryItems(
        int branchId,
        IReadOnlyCollection<int> inventoryItemIds,
        CancellationToken cancellationToken) =>
        await context.InventoryItems
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Variant)
                .ThenInclude(variant => variant.Product)
            .Include(item => item.Variant)
                .ThenInclude(variant => variant.RentalPrices.Where(price => price.BranchId == branchId))
            .Where(item => item.BranchId == branchId && inventoryItemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetSlotDepositAmount(CancellationToken cancellationToken) =>
        await context.Settings
            .AsNoTracking()
            .Where(setting => setting.Id == 1)
            .Select(setting => (decimal?)setting.SlotDepositAmount)
            .SingleOrDefaultAsync(cancellationToken) ?? 100_000m;
}
