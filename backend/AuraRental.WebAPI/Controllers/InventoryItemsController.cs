using AuraRental.Service.DTOs.Catalog;
using AuraRental.Service.DTOs.Common;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/inventory-items")]
[Authorize]
[RequireBranch]
[Idempotent]
public sealed class InventoryItemsController(ICatalogUseCase catalogUseCase) : ApiControllerBase
{
    [HttpPatch("{inventoryItemId:guid}")]
    public async Task<ActionResult<ApiResponse<InventoryItemDto>>> Update(
        Guid inventoryItemId,
        [FromBody] UpdateInventoryItemRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await catalogUseCase.UpdateInventoryItem(inventoryItemId, request, cancellationToken));
}
