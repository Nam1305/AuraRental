using AuraRental.Service.DTOs.Auth;
using AuraRental.Service.DTOs.Common;
using AuraRental.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthUseCase authUseCase) : ApiControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await authUseCase.Login(request, cancellationToken));
}
