using AuraRental.Domain.Entities;
using AuraRental.Service.DTOs.Catalog;

namespace AuraRental.Service.Interface.Persistence;

public interface ICatalogRepository
{
    Task<IReadOnlyList<Product>> SearchProducts(
        int branchId,
        string? query,
        string? category,
        bool? active,
        int limit,
        CancellationToken cancellationToken);

    Task<Product?> GetProduct(int branchId, int productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LatestRentalCustomerDto>> GetLatestRentalCustomers(
        int branchId,
        IReadOnlyCollection<int> inventoryItemIds,
        CancellationToken cancellationToken);
    Task<Product?> GetProductForUpdate(int branchId, int productId, CancellationToken cancellationToken);
    Task<ProductVariant?> GetVariantForUpdate(int variantId, int branchId, CancellationToken cancellationToken);
    Task<InventoryItem?> GetInventoryItemForUpdate(int inventoryItemId, int branchId, CancellationToken cancellationToken);
    Task<bool> ProductCodeExists(int branchId, string code, CancellationToken cancellationToken);
    Task<bool> VariantSizeExists(int productId, string size, CancellationToken cancellationToken);
    Task<bool> AssetCodeExists(IReadOnlyCollection<string> assetCodes, CancellationToken cancellationToken);
    void AddProduct(Product product);
    void AddVariant(ProductVariant variant);
    void AddInventoryItems(IEnumerable<InventoryItem> items);
    void RemoveRentalPrices(IEnumerable<BranchRentalPrice> prices);
    void AddRentalPrices(IEnumerable<BranchRentalPrice> prices);
}
