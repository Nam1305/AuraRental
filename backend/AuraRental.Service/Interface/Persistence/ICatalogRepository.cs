using AuraRental.Domain.Entities;

namespace AuraRental.Service.Interface.Persistence;

public interface ICatalogRepository
{
    Task<IReadOnlyList<Product>> SearchProducts(
        Guid branchId,
        string? query,
        string? category,
        bool? active,
        int limit,
        CancellationToken cancellationToken);

    Task<Product?> GetProduct(Guid branchId, Guid productId, CancellationToken cancellationToken);
    Task<Product?> GetProductForUpdate(Guid branchId, Guid productId, CancellationToken cancellationToken);
    Task<ProductVariant?> GetVariantForUpdate(Guid variantId, Guid branchId, CancellationToken cancellationToken);
    Task<InventoryItem?> GetInventoryItemForUpdate(Guid inventoryItemId, Guid branchId, CancellationToken cancellationToken);
    Task<bool> ProductCodeExists(Guid branchId, string code, CancellationToken cancellationToken);
    Task<bool> VariantSizeExists(Guid productId, string size, CancellationToken cancellationToken);
    Task<bool> AssetCodeExists(IReadOnlyCollection<string> assetCodes, CancellationToken cancellationToken);
    void AddProduct(Product product);
    void AddVariant(ProductVariant variant);
    void AddInventoryItems(IEnumerable<InventoryItem> items);
    void RemoveRentalPrices(IEnumerable<BranchRentalPrice> prices);
    void AddRentalPrices(IEnumerable<BranchRentalPrice> prices);
}
