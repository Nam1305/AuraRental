using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.DTOs.Catalog;
using AuraRental.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class CatalogRepository(AuraRentalDbContext context) : ICatalogRepository
{
    public async Task<IReadOnlyList<Product>> SearchProducts(
        int branchId,
        string? query,
        string? category,
        bool? active,
        int limit,
        CancellationToken cancellationToken)
    {
        var products = context.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Where(product => product.BranchId == branchId)
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

    public Task<Product?> GetProduct(int branchId, int productId, CancellationToken cancellationToken) =>
        context.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.InventoryItems.Where(item => item.BranchId == branchId))
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.RentalPrices.Where(price => price.BranchId == branchId))
            .FirstOrDefaultAsync(product => product.Id == productId && product.BranchId == branchId, cancellationToken);

    public async Task<IReadOnlyList<LatestRentalCustomerDto>> GetLatestRentalCustomers(
        int branchId,
        IReadOnlyCollection<int> inventoryItemIds,
        CancellationToken cancellationToken)
    {
        if (inventoryItemIds.Count == 0)
        {
            return [];
        }

        var completedOrCurrentRentalItems = await context.OrderItems
            .AsNoTracking()
            .Where(item =>
                inventoryItemIds.Contains(item.InventoryItemId) &&
                item.Order.BranchId == branchId &&
                item.Order.Status != OrderStatus.Cancelled &&
                item.Order.Reservation.RentalStartAt <= DateTimeOffset.UtcNow)
            .OrderByDescending(item => item.Order.Reservation.RentalStartAt)
            .ThenByDescending(item => item.OrderId)
            .Select(item => new LatestRentalCustomerDto(
                item.InventoryItemId,
                item.Order.CustomerId,
                item.OrderId,
                item.Order.OrderNo,
                item.Order.Reservation.RentalStartAt,
                item.Order.Reservation.RentalEndAt,
                item.Order.Customer.Name,
                item.Order.Customer.Phone,
                item.Order.Customer.InstagramHandle,
                item.Order.Customer.TiktokHandle))
            .ToListAsync(cancellationToken);

        return completedOrCurrentRentalItems
            .GroupBy(item => item.InventoryItemId)
            .Select(group => group.First())
            .ToList();
    }

    public Task<Product?> GetProductForUpdate(int branchId, int productId, CancellationToken cancellationToken) =>
        context.Products.FirstOrDefaultAsync(product => product.Id == productId && product.BranchId == branchId, cancellationToken);

    public Task<ProductVariant?> GetVariantForUpdate(
        int variantId,
        int branchId,
        CancellationToken cancellationToken) =>
        context.ProductVariants
            .Include(variant => variant.Product)
            .Include(variant => variant.RentalPrices.Where(price => price.BranchId == branchId))
            .FirstOrDefaultAsync(variant => variant.Id == variantId && variant.Product.BranchId == branchId, cancellationToken);

    public Task<InventoryItem?> GetInventoryItemForUpdate(
        int inventoryItemId,
        int branchId,
        CancellationToken cancellationToken) =>
        context.InventoryItems.FirstOrDefaultAsync(
            item => item.Id == inventoryItemId && item.BranchId == branchId,
            cancellationToken);

    public Task<bool> ProductCodeExists(int branchId, string code, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(product => product.BranchId == branchId && product.Code == code, cancellationToken);

    public Task<bool> VariantSizeExists(int productId, string size, CancellationToken cancellationToken) =>
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
