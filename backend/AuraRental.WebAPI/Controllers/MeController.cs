using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Identity;
using AuraRental.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/me")]
[Authorize]
public sealed class MeController(IIdentityUseCase identityUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Get(CancellationToken cancellationToken) =>
        ResponseData(await identityUseCase.GetCurrentUser(cancellationToken));
}
