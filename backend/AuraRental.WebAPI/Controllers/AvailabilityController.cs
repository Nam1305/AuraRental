using AuraRental.Service.DTOs.Availability;
using AuraRental.Service.DTOs.Common;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/availability")]
[Authorize]
[RequireBranch]
public sealed class AvailabilityController(IAvailabilityUseCase availabilityUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AvailabilityResultDto>>> Get(
        [FromQuery] string? query,
        [FromQuery] string? size,
        [FromQuery] DateTimeOffset startAt,
        [FromQuery] DateTimeOffset endAt,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ResponseData(await availabilityUseCase.Search(query, size, startAt, endAt, limit, cancellationToken));
}
