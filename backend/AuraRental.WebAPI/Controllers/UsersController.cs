using AuraRental.Service.DTOs.Admin;
using AuraRental.Service.DTOs.Common;
using AuraRental.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuraRental.WebAPI.Security;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(IAdminUseCase adminUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserListItemDto>>>> Search(
        [FromQuery] string? query,
        [FromQuery] bool? active,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ResponseData(await adminUseCase.SearchUsers(query, active, limit, cancellationToken));

    [HttpPut("{userId:guid}/branches")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<UserListItemDto>>> ReplaceBranches(
        Guid userId,
        [FromBody] ReplaceUserBranchesRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await adminUseCase.ReplaceUserBranches(userId, request, cancellationToken));
}
