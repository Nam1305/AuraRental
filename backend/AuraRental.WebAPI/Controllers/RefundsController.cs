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
    [HttpPost("{refundId:guid}/submit")]
    public async Task<ActionResult<ApiResponse<RefundDto>>> Submit(
        Guid refundId,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.SubmitRefund(refundId, cancellationToken));

    [HttpPost("{refundId:guid}/return-for-review")]
    public async Task<ActionResult<ApiResponse<RefundDto>>> ReturnForReview(
        Guid refundId,
        [FromBody] ReturnForReviewRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.ReturnForReview(refundId, request, cancellationToken));

    [HttpPost("{refundId:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ApproveRefundDto>>> Approve(
        Guid refundId,
        [FromBody] ApproveRefundRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.Approve(refundId, request, cancellationToken));

    [HttpPost("{refundId:guid}/revisions")]
    public async Task<ActionResult<ApiResponse<RefundDto>>> CreateRevision(
        Guid refundId,
        [FromBody] CreateRefundRevisionRequest request,
        CancellationToken cancellationToken)
    {
        var revision = await returnUseCase.CreateRevision(refundId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ResponseData(revision));
    }

    [HttpPost("{refundId:guid}/settle")]
    public async Task<ActionResult<ApiResponse<SettleRefundDto>>> Settle(
        Guid refundId,
        [FromBody] SettleRefundRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.Settle(refundId, request, cancellationToken));
}
