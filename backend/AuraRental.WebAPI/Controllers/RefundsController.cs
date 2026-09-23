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
[Idempotent]
public sealed class RefundsController(IReturnUseCase returnUseCase) : ApiControllerBase
{
    [HttpPost("{refundId:int}/submit")]
    public async Task<ActionResult<ApiResponse<RefundDto>>> Submit(
        int refundId,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.SubmitRefund(refundId, cancellationToken));

    [HttpPost("{refundId:int}/return-for-review")]
    public async Task<ActionResult<ApiResponse<RefundDto>>> ReturnForReview(
        int refundId,
        [FromBody] ReturnForReviewRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.ReturnForReview(refundId, request, cancellationToken));

    [HttpPost("{refundId:int}/approve")]
    public async Task<ActionResult<ApiResponse<ApproveRefundDto>>> Approve(
        int refundId,
        [FromBody] ApproveRefundRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.Approve(refundId, request, cancellationToken));

    [HttpPost("{refundId:int}/revisions")]
    public async Task<ActionResult<ApiResponse<RefundDto>>> CreateRevision(
        int refundId,
        [FromBody] CreateRefundRevisionRequest request,
        CancellationToken cancellationToken)
    {
        var revision = await returnUseCase.CreateRevision(refundId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ResponseData(revision));
    }

    [HttpPost("{refundId:int}/settle")]
    public async Task<ActionResult<ApiResponse<SettleRefundDto>>> Settle(
        int refundId,
        [FromBody] SettleRefundRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.Settle(refundId, request, cancellationToken));
}
