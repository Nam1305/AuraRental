using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Operations;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/reports")]
[Authorize]
public sealed class ReportsController(IOperationsUseCase operationsUseCase) : ApiControllerBase
{
    [HttpGet("summary")]
    [RequireBranch]
    public async Task<ActionResult<ApiResponse<ReportSummaryDto>>> GetSummary(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        ResponseData(await operationsUseCase.GetReport(from, to, cancellationToken));

    [HttpGet("branches")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportSummaryDto>>>> GetBranches(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        ResponseData(await operationsUseCase.GetBranchReports(from, to, cancellationToken));
}
