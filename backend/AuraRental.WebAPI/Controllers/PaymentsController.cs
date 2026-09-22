using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Order;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/payments")]
[Authorize]
[RequireBranch]
[Idempotent]
public sealed class PaymentsController(IOrderUseCase orderUseCase) : ApiControllerBase
{
    [HttpPost("{paymentId:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<PaymentActionDto>>> Confirm(
        Guid paymentId,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.ConfirmPayment(paymentId, cancellationToken));

    [HttpPost("{paymentId:guid}/void")]
    public async Task<ActionResult<ApiResponse<PaymentActionDto>>> Void(
        Guid paymentId,
        [FromBody] PaymentActionRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.VoidPayment(paymentId, request, cancellationToken));
}
