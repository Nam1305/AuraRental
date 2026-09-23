using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Return;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/refunds")]
[Authorize]
[RequireBranch]
public sealed class RefundReceiptsController(IReturnUseCase returnUseCase) : ApiControllerBase
{
    [HttpGet("{refundId:int}/receipt")]
    public async Task<ActionResult<ApiResponse<RefundReceiptDto>>> GetReceipt(
        int refundId,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.GetReceipt(refundId, cancellationToken));
}
