using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Return;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/returns")]
[Authorize]
[RequireBranch]
public sealed class ReturnsController(IReturnUseCase returnUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReturnQueueItemDto>>>> Get(
        [FromQuery] string? status,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ResponseData(await returnUseCase.GetQueue(status, limit, cancellationToken));
}
