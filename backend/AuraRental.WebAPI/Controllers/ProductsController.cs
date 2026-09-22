using AuraRental.Service.DTOs.Catalog;
using AuraRental.Service.DTOs.Common;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/products")]
[Authorize]
[RequireBranch]
public sealed class ProductsController(ICatalogUseCase catalogUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductListItemDto>>>> Get(
        [FromQuery] string? query,
        [FromQuery] string? category,
        [FromQuery] bool? active,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ResponseData(await catalogUseCase.GetProducts(query, category, active, limit, cancellationToken));

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetById(
        Guid productId,
        CancellationToken cancellationToken) =>
        ResponseData(await catalogUseCase.GetProduct(productId, cancellationToken));

    [HttpPost]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await catalogUseCase.CreateProduct(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { productId = product.Id }, ResponseData(product));
    }

    [HttpPatch("{productId:guid}")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> Update(
        Guid productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await catalogUseCase.UpdateProduct(productId, request, cancellationToken));

    [HttpPost("{productId:guid}/variants")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<ProductVariantDto>>> AddVariant(
        Guid productId,
        [FromBody] CreateProductVariantInput request,
        CancellationToken cancellationToken)
    {
        var variant = await catalogUseCase.AddVariant(productId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ResponseData(variant));
    }
}
