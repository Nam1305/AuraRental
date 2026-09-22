using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Quote;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/quotes")]
[Authorize]
[RequireBranch]
public sealed class QuotesController(IQuoteUseCase quoteUseCase) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<QuoteDto>>> Create(
        [FromBody] CreateQuoteRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await quoteUseCase.Create(request, cancellationToken));
}
