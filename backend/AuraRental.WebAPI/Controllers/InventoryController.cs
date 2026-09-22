using AuraRental.Service.DTOs.Availability;
using AuraRental.Service.DTOs.Common;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/inventory")]
[Authorize]
[RequireBranch]
public sealed class InventoryController(IAvailabilityUseCase availabilityUseCase) : ApiControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InventoryOverviewDto>>>> GetSummary(
        [FromQuery] string? query,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ResponseData(await availabilityUseCase.GetInventorySummary(query, limit, cancellationToken));
}
