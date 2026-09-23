using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Order;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.DTOs.Return;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/orders")]
[Authorize]
[RequireBranch]
public sealed class OrdersController(IOrderUseCase orderUseCase, IReturnUseCase returnUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderListItemDto>>>> Search(
        [FromQuery] string? status,
        [FromQuery] string? query,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ResponseData(await orderUseCase.Search(status, query, from, to, limit, cancellationToken));

    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> Get(
        int orderId,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.Get(orderId, cancellationToken));

    [HttpPost("{orderId:int}/identity-verification")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<VerifyIdentityDto>>> VerifyIdentity(
        int orderId,
        [FromBody] VerifyIdentityRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.VerifyIdentity(orderId, request, cancellationToken));

    [HttpPost("{orderId:int}/cancel")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<CancelOrderDto>>> Cancel(
        int orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.Cancel(orderId, request, cancellationToken));

    [HttpPost("{orderId:int}/prepare")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<OrderTransitionDto>>> Prepare(
        int orderId,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.Prepare(orderId, cancellationToken));

    [HttpPost("{orderId:int}/delivery/start")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<OrderTransitionDto>>> StartDelivery(
        int orderId,
        [FromBody] StartDeliveryRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.StartDelivery(orderId, request, cancellationToken));

    [HttpPost("{orderId:int}/delivery/complete")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<OrderTransitionDto>>> CompleteDelivery(
        int orderId,
        [FromBody] CompleteDeliveryRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.CompleteDelivery(orderId, request, cancellationToken));

    [HttpPost("{orderId:int}/return-delivery/start")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<OrderTransitionDto>>> StartReturnDelivery(
        int orderId,
        [FromBody] StartDeliveryRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.StartReturnDelivery(orderId, request, cancellationToken));

    [HttpPost("{orderId:int}/return-delivery/complete")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<OrderTransitionDto>>> CompleteReturnDelivery(
        int orderId,
        [FromBody] CompleteReturnDeliveryRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.CompleteReturnDelivery(orderId, request, cancellationToken));

    [HttpPut("{orderId:int}/items/{orderItemId:int}/inspection")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<InspectionDto>>> InspectItem(
        int orderId,
        int orderItemId,
        [FromBody] InspectOrderItemRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.InspectItem(orderId, orderItemId, request, cancellationToken));

    [HttpPost("{orderId:int}/refunds")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<RefundDto>>> CreateRefund(
        int orderId,
        [FromBody] CreateRefundRequest request,
        CancellationToken cancellationToken)
    {
        var refund = await returnUseCase.CreateOrUpdateRefund(orderId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ResponseData(refund));
    }
}
