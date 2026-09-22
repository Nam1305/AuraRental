using AuraRental.Service.DTOs.Admin;
using AuraRental.Service.DTOs.Common;
using AuraRental.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuraRental.WebAPI.Security;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/settings")]
[Authorize]
public sealed class SettingsController(IAdminUseCase adminUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<SettingsDto>>> Get(CancellationToken cancellationToken) =>
        ResponseData(await adminUseCase.GetSettings(cancellationToken));

    [HttpPut]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<SettingsDto>>> Update(
        [FromBody] UpdateSettingsRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await adminUseCase.UpdateSettings(request, cancellationToken));
}
