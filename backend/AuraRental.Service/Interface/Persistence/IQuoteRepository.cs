using AuraRental.Domain.Entities;

namespace AuraRental.Service.Interface.Persistence;

public interface IQuoteRepository
{
    Task<bool> CustomerExists(int customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryItem>> GetInventoryItems(
        int branchId,
        IReadOnlyCollection<int> inventoryItemIds,
        CancellationToken cancellationToken);
    Task<decimal> GetSlotDepositAmount(CancellationToken cancellationToken);
}
