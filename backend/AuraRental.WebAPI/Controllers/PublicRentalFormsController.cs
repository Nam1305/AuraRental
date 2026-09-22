using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.PublicForm;
using AuraRental.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AuraRental.WebAPI.Security;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/public/rental-forms")]
[AllowAnonymous]
[EnableRateLimiting("public-form")]
public sealed class PublicRentalFormsController(IPublicRentalFormUseCase publicRentalFormUseCase) : ApiControllerBase
{
    [HttpGet("{token}")]
    public async Task<ActionResult<ApiResponse<PublicRentalFormDto>>> Get(
        string token,
        CancellationToken cancellationToken) =>
        ResponseData(await publicRentalFormUseCase.Get(token, cancellationToken));

    [HttpPost("{token}/submit")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<SubmitPublicRentalFormDto>>> Submit(
        string token,
        [FromBody] SubmitPublicRentalFormRequest request,
        CancellationToken cancellationToken)
    {
        var result = await publicRentalFormUseCase.Submit(token, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ResponseData(result));
    }
}
