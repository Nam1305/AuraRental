using AuraRental.Service.DTOs.Catalog;
using AuraRental.Service.DTOs.Common;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/product-variants")]
[Authorize]
[RequireBranch]
[Idempotent]
public sealed class ProductVariantsController(ICatalogUseCase catalogUseCase) : ApiControllerBase
{
    [HttpPut("{variantId:int}/rental-prices")]
    public async Task<ActionResult<ApiResponse<RentalPriceSetDto>>> ReplaceRentalPrices(
        int variantId,
        [FromBody] ReplaceRentalPricesRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await catalogUseCase.ReplaceRentalPrices(variantId, request, cancellationToken));

    [HttpPost("{variantId:int}/inventory-items")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InventoryItemDto>>>> AddInventoryItems(
        int variantId,
        [FromBody] AddInventoryItemsRequest request,
        CancellationToken cancellationToken)
    {
        var items = await catalogUseCase.AddInventoryItems(variantId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ResponseData(items));
    }
}
