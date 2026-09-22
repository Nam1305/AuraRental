using AuraRental.Service.DTOs.Catalog;

namespace AuraRental.Service.Interface.UseCase;

public interface ICatalogUseCase
{
    Task<IReadOnlyList<ProductListItemDto>> GetProducts(
        string? query,
        string? category,
        bool? active,
        int limit,
        CancellationToken cancellationToken);

    Task<ProductDetailDto> GetProduct(Guid productId, CancellationToken cancellationToken);
    Task<ProductDetailDto> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken);
    Task<ProductDetailDto> UpdateProduct(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken);
    Task<ProductVariantDto> AddVariant(Guid productId, CreateProductVariantInput request, CancellationToken cancellationToken);
    Task<RentalPriceSetDto> ReplaceRentalPrices(Guid variantId, ReplaceRentalPricesRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryItemDto>> AddInventoryItems(Guid variantId, AddInventoryItemsRequest request, CancellationToken cancellationToken);
    Task<InventoryItemDto> UpdateInventoryItem(Guid inventoryItemId, UpdateInventoryItemRequest request, CancellationToken cancellationToken);
}
