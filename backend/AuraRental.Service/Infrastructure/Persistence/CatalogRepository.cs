using AuraRental.Domain.Entities;
using AuraRental.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class CatalogRepository(AuraRentalDbContext context) : ICatalogRepository
{
    public async Task<IReadOnlyList<Product>> SearchProducts(
        Guid branchId,
        string? query,
        string? category,
        bool? active,
        int limit,
        CancellationToken cancellationToken)
    {
        var products = context.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(product => product.Variants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.InventoryItems.Where(item => item.BranchId == branchId))
            .Include(product => product.Variants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.RentalPrices.Where(price => price.BranchId == branchId))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim();
            products = products.Where(product =>
                EF.Functions.ILike(product.Name, $"%{normalizedQuery}%") ||
                EF.Functions.ILike(product.Code, $"%{normalizedQuery}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            products = products.Where(product => product.Category == category.Trim());
        }

        if (active.HasValue)
        {
            products = products.Where(product => product.IsActive == active.Value);
        }

        return await products
            .OrderBy(product => product.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> GetProduct(Guid branchId, Guid productId, CancellationToken cancellationToken) =>
        context.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.InventoryItems.Where(item => item.BranchId == branchId))
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.RentalPrices.Where(price => price.BranchId == branchId))
            .FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);

    public Task<Product?> GetProductForUpdate(Guid productId, CancellationToken cancellationToken) =>
        context.Products.FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);

    public Task<ProductVariant?> GetVariantForUpdate(
        Guid variantId,
        Guid branchId,
        CancellationToken cancellationToken) =>
        context.ProductVariants
            .Include(variant => variant.Product)
            .Include(variant => variant.RentalPrices.Where(price => price.BranchId == branchId))
            .FirstOrDefaultAsync(variant => variant.Id == variantId, cancellationToken);

    public Task<InventoryItem?> GetInventoryItemForUpdate(
        Guid inventoryItemId,
        Guid branchId,
        CancellationToken cancellationToken) =>
        context.InventoryItems.FirstOrDefaultAsync(
            item => item.Id == inventoryItemId && item.BranchId == branchId,
            cancellationToken);

    public Task<bool> ProductCodeExists(string code, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(product => product.Code == code, cancellationToken);

    public Task<bool> VariantSizeExists(Guid productId, string size, CancellationToken cancellationToken) =>
        context.ProductVariants.AnyAsync(
            variant => variant.ProductId == productId && variant.Size == size,
            cancellationToken);

    public Task<bool> AssetCodeExists(IReadOnlyCollection<string> assetCodes, CancellationToken cancellationToken) =>
        context.InventoryItems.AnyAsync(item => assetCodes.Contains(item.AssetCode), cancellationToken);

    public void AddProduct(Product product) => context.Products.Add(product);
    public void AddVariant(ProductVariant variant) => context.ProductVariants.Add(variant);
    public void AddInventoryItems(IEnumerable<InventoryItem> items) => context.InventoryItems.AddRange(items);
    public void RemoveRentalPrices(IEnumerable<BranchRentalPrice> prices) => context.BranchRentalPrices.RemoveRange(prices);
    public void AddRentalPrices(IEnumerable<BranchRentalPrice> prices) => context.BranchRentalPrices.AddRange(prices);
}
