using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Identity;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.DTOs.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuraRental.WebAPI.Security;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/branches")]
[Authorize]
public sealed class BranchesController(IAdminUseCase adminUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BranchSummaryDto>>>> Get(
        CancellationToken cancellationToken) =>
        ResponseData(await adminUseCase.GetBranches(cancellationToken));

    [HttpPost]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<BranchSummaryDto>>> Create(
        [FromBody] CreateBranchRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await adminUseCase.CreateBranch(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ResponseData(branch));
    }

    [HttpPatch("{branchId:int}")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<BranchSummaryDto>>> Update(
        int branchId,
        [FromBody] UpdateBranchRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await adminUseCase.UpdateBranch(branchId, request, cancellationToken));
}
