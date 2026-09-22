using AuraRental.Domain.Entities;

namespace AuraRental.Service.Interface.Persistence;

public interface IQuoteRepository
{
    Task<bool> CustomerExists(Guid customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryItem>> GetInventoryItems(
        Guid branchId,
        IReadOnlyCollection<Guid> inventoryItemIds,
        CancellationToken cancellationToken);
    Task<decimal> GetSlotDepositAmount(CancellationToken cancellationToken);
}
