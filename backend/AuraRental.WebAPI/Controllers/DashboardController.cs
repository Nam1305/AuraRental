using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Operations;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/dashboard")]
[Authorize]
[RequireBranch]
public sealed class DashboardController(IOperationsUseCase operationsUseCase) : ApiControllerBase
{
    [HttpGet("today")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetToday(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken) =>
        ResponseData(await operationsUseCase.GetDashboard(
            date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)),
            cancellationToken));
}
