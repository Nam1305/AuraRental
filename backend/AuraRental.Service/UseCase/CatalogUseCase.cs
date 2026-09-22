using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.DTOs.Catalog;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.Utils;

namespace AuraRental.Service.UseCase;

public sealed class CatalogUseCase(
    ICatalogRepository catalogRepository,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext) : ICatalogUseCase
{
    public async Task<IReadOnlyList<ProductListItemDto>> GetProducts(
        string? query,
        string? category,
        bool? active,
        int limit,
        CancellationToken cancellationToken)
    {
        ValidateLimit(limit);
        var products = await catalogRepository.SearchProducts(
            requestContext.BranchId,
            query,
            category,
            active,
            limit,
            cancellationToken);

        return products.Select(product =>
        {
            var variants = product.Variants.Where(variant => variant.IsActive).ToList();
            var inventory = variants.SelectMany(variant => variant.InventoryItems).ToList();
            var prices = variants.SelectMany(variant => variant.RentalPrices).ToList();

            return new ProductListItemDto(
                product.Id,
                product.Code,
                product.Name,
                product.Category,
                product.IsActive,
                product.ImagePaths.FirstOrDefault(),
                variants.Select(variant => variant.Size).Distinct().Order().ToList(),
                inventory.Count(item => item.Status != InventoryStatus.Retired && item.Status != InventoryStatus.Lost),
                inventory.Count(item => item.Status == InventoryStatus.Usable),
                prices.Count == 0 ? null : prices.Min(price => price.Price),
                prices.Select(price => price.PackageCode).Distinct().Order().ToList());
        }).ToList();
    }

    public async Task<ProductDetailDto> GetProduct(Guid productId, CancellationToken cancellationToken)
    {
        var product = await catalogRepository.GetProduct(requestContext.BranchId, productId, cancellationToken)
            ?? throw new NotFoundException("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");

        return new ProductDetailDto(
            product.Id,
            product.Code,
            product.Name,
            product.Category,
            product.Color,
            product.Material,
            product.Description,
            product.ImagePaths,
            product.IsActive,
            product.Variants.OrderBy(variant => variant.Size).Select(ToVariant).ToList());
    }

    public async Task<ProductDetailDto> CreateProduct(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        ValidateProduct(request.Code, request.Name, request.Category, request.Variants);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await catalogRepository.ProductCodeExists(requestContext.BranchId, code, cancellationToken))
        {
            throw new ConflictException("PRODUCT_CODE_EXISTS", "Mã sản phẩm đã tồn tại tại chi nhánh này.");
        }

        var assetCodes = request.Variants.SelectMany(variant => variant.InventoryItems)
            .Select(item => item.AssetCode.Trim().ToUpperInvariant()).ToArray();
        ValidateAssetCodes(assetCodes);
        if (await catalogRepository.AssetCodeExists(assetCodes, cancellationToken))
        {
            throw new ConflictException("ASSET_CODE_EXISTS", "Có mã vật lý đã tồn tại.");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BranchId = requestContext.BranchId,
            Code = code,
            Name = request.Name.Trim(),
            Category = request.Category.Trim().ToUpperInvariant(),
            Color = request.Color?.Trim(),
            Material = request.Material?.Trim(),
            Description = request.Description?.Trim(),
            ImagePaths = NormalizePaths(request.ImagePaths),
            Variants = request.Variants.Select(CreateVariantEntity).ToList()
        };
        catalogRepository.AddProduct(product);
        await unitOfWork.SaveChanges(cancellationToken);
        return await GetProduct(product.Id, cancellationToken);
    }

    public async Task<ProductDetailDto> UpdateProduct(
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        EnsureManager();
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Category))
        {
            throw new ValidationException("PRODUCT_FIELDS_REQUIRED", "Tên và loại sản phẩm là bắt buộc.");
        }

        var product = await catalogRepository.GetProductForUpdate(requestContext.BranchId, productId, cancellationToken)
            ?? throw new NotFoundException("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");
        product.Name = request.Name.Trim();
        product.Category = request.Category.Trim().ToUpperInvariant();
        product.Color = request.Color?.Trim();
        product.Material = request.Material?.Trim();
        product.Description = request.Description?.Trim();
        product.ImagePaths = NormalizePaths(request.ImagePaths);
        product.IsActive = request.IsActive;
        await unitOfWork.SaveChanges(cancellationToken);
        return await GetProduct(product.Id, cancellationToken);
    }

    public async Task<ProductVariantDto> AddVariant(
        Guid productId,
        CreateProductVariantInput request,
        CancellationToken cancellationToken)
    {
        ValidateVariant(request);
        _ = await catalogRepository.GetProductForUpdate(requestContext.BranchId, productId, cancellationToken)
            ?? throw new NotFoundException("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");
        var size = request.Size.Trim().ToUpperInvariant();
        if (await catalogRepository.VariantSizeExists(productId, size, cancellationToken))
        {
            throw new ConflictException("VARIANT_SIZE_EXISTS", "Size này đã tồn tại trong sản phẩm.");
        }

        var codes = request.InventoryItems.Select(item => item.AssetCode.Trim().ToUpperInvariant()).ToArray();
        ValidateAssetCodes(codes);
        if (await catalogRepository.AssetCodeExists(codes, cancellationToken))
        {
            throw new ConflictException("ASSET_CODE_EXISTS", "Có mã vật lý đã tồn tại.");
        }

        var variant = CreateVariantEntity(request);
        variant.ProductId = productId;
        catalogRepository.AddVariant(variant);
        await unitOfWork.SaveChanges(cancellationToken);
        var savedProduct = await catalogRepository.GetProduct(requestContext.BranchId, productId, cancellationToken)
            ?? throw new InvalidOperationException("Variant was created but could not be reloaded.");
        return ToVariant(savedProduct.Variants.Single(item => item.Id == variant.Id));
    }

    public async Task<RentalPriceSetDto> ReplaceRentalPrices(
        Guid variantId,
        ReplaceRentalPricesRequest request,
        CancellationToken cancellationToken)
    {
        ValidatePrices(request.Prices);
        var variant = await catalogRepository.GetVariantForUpdate(variantId, requestContext.BranchId, cancellationToken)
            ?? throw new NotFoundException("VARIANT_NOT_FOUND", "Không tìm thấy variant.");
        catalogRepository.RemoveRentalPrices(variant.RentalPrices);
        var prices = CreatePrices(variant.Id, request.Prices);
        catalogRepository.AddRentalPrices(prices);
        await unitOfWork.SaveChanges(cancellationToken);
        return new RentalPriceSetDto(
            variant.Id,
            requestContext.BranchId,
            prices.OrderBy(price => price.Price).Select(ToPrice).ToList(),
            true);
    }

    public async Task<IReadOnlyList<InventoryItemDto>> AddInventoryItems(
        Guid variantId,
        AddInventoryItemsRequest request,
        CancellationToken cancellationToken)
    {
        _ = await catalogRepository.GetVariantForUpdate(variantId, requestContext.BranchId, cancellationToken)
            ?? throw new NotFoundException("VARIANT_NOT_FOUND", "Không tìm thấy variant.");
        var codes = request.Items.Select(item => item.AssetCode.Trim().ToUpperInvariant()).ToArray();
        ValidateAssetCodes(codes);
        if (await catalogRepository.AssetCodeExists(codes, cancellationToken))
        {
            throw new ConflictException("ASSET_CODE_EXISTS", "Có mã vật lý đã tồn tại.");
        }

        var items = request.Items.Select(item => new InventoryItem
        {
            Id = Guid.NewGuid(),
            VariantId = variantId,
            BranchId = requestContext.BranchId,
            AssetCode = item.AssetCode.Trim().ToUpperInvariant(),
            Status = InventoryStatus.Usable
        }).ToList();
        catalogRepository.AddInventoryItems(items);
        await unitOfWork.SaveChanges(cancellationToken);
        return items.Select(ToInventoryItem).ToList();
    }

    public async Task<InventoryItemDto> UpdateInventoryItem(
        Guid inventoryItemId,
        UpdateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await catalogRepository.GetInventoryItemForUpdate(
                inventoryItemId,
                requestContext.BranchId,
                cancellationToken)
            ?? throw new NotFoundException("INVENTORY_NOT_FOUND", "Không tìm thấy mã đồ tại chi nhánh này.");
        item.Status = RentalRules.ParseEnum<InventoryStatus>(
            request.Status,
            "INVALID_INVENTORY_STATUS",
            "Trạng thái kho không hợp lệ.");
        await unitOfWork.SaveChanges(cancellationToken);
        return ToInventoryItem(item);
    }

    private static ProductVariantDto ToVariant(ProductVariant variant)
    {
        var inventory = variant.InventoryItems;
        return new ProductVariantDto(
            variant.Id,
            variant.Size,
            variant.Measurements,
            variant.ReplacementValue,
            variant.RentalPrices
                .OrderBy(price => price.Price)
                .Select(price => new RentalPriceDto(price.PackageCode, ApiText.PackageLabel(price.PackageCode), price.Price))
                .ToList(),
            new InventorySummaryDto(
                inventory.Count,
                inventory.Count(item => item.Status == InventoryStatus.Usable),
                inventory.Count(item => item.Status == InventoryStatus.Maintenance),
                inventory.Count(item => item.Status == InventoryStatus.Lost),
                inventory.Count(item => item.Status == InventoryStatus.Retired)),
            inventory.OrderBy(item => item.AssetCode).Select(ToInventoryItem).ToList());
    }

    private ProductVariant CreateVariantEntity(CreateProductVariantInput request)
    {
        ValidateVariant(request);
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            Size = request.Size.Trim().ToUpperInvariant(),
            Measurements = request.Measurements?.Trim(),
            ReplacementValue = request.ReplacementValue,
            RentalPrices = [],
            InventoryItems = []
        };
        foreach (var price in CreatePrices(variant.Id, request.Prices))
        {
            variant.RentalPrices.Add(price);
        }

        foreach (var item in request.InventoryItems)
        {
            variant.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                VariantId = variant.Id,
                BranchId = requestContext.BranchId,
                AssetCode = item.AssetCode.Trim().ToUpperInvariant(),
                Status = InventoryStatus.Usable
            });
        }

        return variant;
    }

    private List<BranchRentalPrice> CreatePrices(Guid variantId, IReadOnlyList<RentalPriceInput> inputs) =>
        inputs.Select(input => new BranchRentalPrice
        {
            Id = Guid.NewGuid(),
            BranchId = requestContext.BranchId,
            VariantId = variantId,
            PackageCode = input.PackageCode.Trim().ToUpperInvariant(),
            Price = input.Price
        }).ToList();

    private static void ValidateProduct(
        string code,
        string name,
        string category,
        IReadOnlyList<CreateProductVariantInput> variants)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
        {
            throw new ValidationException("PRODUCT_FIELDS_REQUIRED", "Mã, tên và loại sản phẩm là bắt buộc.");
        }

        if (variants.Count == 0 || variants.Select(item => item.Size.Trim().ToUpperInvariant()).Distinct().Count() != variants.Count)
        {
            throw new ValidationException("INVALID_VARIANTS", "Sản phẩm cần ít nhất một size và không được trùng size.");
        }
    }

    private static void ValidateVariant(CreateProductVariantInput request)
    {
        if (string.IsNullOrWhiteSpace(request.Size) || request.ReplacementValue <= 0)
        {
            throw new ValidationException("INVALID_VARIANT", "Size là bắt buộc và giá trị thay thế phải lớn hơn 0.");
        }

        ValidatePrices(request.Prices);
    }

    private static void ValidatePrices(IReadOnlyList<RentalPriceInput> prices)
    {
        var packages = prices.Select(item => item.PackageCode.Trim().ToUpperInvariant()).ToList();
        if (prices.Count == 0 || !packages.Contains("1D") || packages.Distinct().Count() != packages.Count || prices.Any(item => item.Price <= 0))
        {
            throw new ValidationException("INVALID_RENTAL_PRICES", "Bảng giá phải có 1D, không trùng gói và mọi giá phải lớn hơn 0.");
        }
    }

    private static void ValidateAssetCodes(IReadOnlyCollection<string> codes)
    {
        if (codes.Count == 0 || codes.Any(string.IsNullOrWhiteSpace) || codes.Distinct().Count() != codes.Count)
        {
            throw new ValidationException("INVALID_ASSET_CODES", "Cần ít nhất một mã vật lý và không được trùng mã.");
        }
    }

    private static string[] NormalizePaths(IEnumerable<string> paths) =>
        paths.Select(path => path.Trim()).Where(path => path.Length > 0).Distinct().ToArray();

    private static RentalPriceDto ToPrice(BranchRentalPrice price) =>
        new(price.PackageCode, ApiText.PackageLabel(price.PackageCode), price.Price);

    private static InventoryItemDto ToInventoryItem(InventoryItem item) =>
        new(item.Id, item.AssetCode, ApiText.EnumValue(item.Status));

    private void EnsureManager()
    {
        if (requestContext.Role != UserRole.Manager)
        {
            throw new ForbiddenException("MANAGER_REQUIRED", "Thao tác này chỉ dành cho manager.");
        }
    }

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > 100)
        {
            throw new ValidationException("INVALID_LIMIT", "Limit phải nằm trong khoảng 1 đến 100.");
        }
    }
}
