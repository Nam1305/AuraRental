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

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> Get(
        Guid orderId,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.Get(orderId, cancellationToken));

    [HttpPost("{orderId:guid}/identity-verification")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<VerifyIdentityDto>>> VerifyIdentity(
        Guid orderId,
        [FromBody] VerifyIdentityRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.VerifyIdentity(orderId, request, cancellationToken));

    [HttpPost("{orderId:guid}/cancel")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<CancelOrderDto>>> Cancel(
        Guid orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.Cancel(orderId, request, cancellationToken));

    [HttpPost("{orderId:guid}/prepare")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<OrderTransitionDto>>> Prepare(
        Guid orderId,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.Prepare(orderId, cancellationToken));

    [HttpPost("{orderId:guid}/delivery/start")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<OrderTransitionDto>>> StartDelivery(
        Guid orderId,
        [FromBody] StartDeliveryRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.StartDelivery(orderId, request, cancellationToken));

    [HttpPost("{orderId:guid}/delivery/complete")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<OrderTransitionDto>>> CompleteDelivery(
        Guid orderId,
        [FromBody] CompleteDeliveryRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.CompleteDelivery(orderId, request, cancellationToken));

    [HttpPost("{orderId:guid}/return-delivery/start")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<OrderTransitionDto>>> StartReturnDelivery(
        Guid orderId,
        [FromBody] StartDeliveryRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.StartReturnDelivery(orderId, request, cancellationToken));

    [HttpPost("{orderId:guid}/return-delivery/complete")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<OrderTransitionDto>>> CompleteReturnDelivery(
        Guid orderId,
        [FromBody] CompleteReturnDeliveryRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await orderUseCase.CompleteReturnDelivery(orderId, request, cancellationToken));

    [HttpPut("{orderId:guid}/items/{orderItemId:guid}/inspection")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<InspectionDto>>> InspectItem(
        Guid orderId,
        Guid orderItemId,
        [FromBody] InspectOrderItemRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await returnUseCase.InspectItem(orderId, orderItemId, request, cancellationToken));

    [HttpPost("{orderId:guid}/refunds")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<RefundDto>>> CreateRefund(
        Guid orderId,
        [FromBody] CreateRefundRequest request,
        CancellationToken cancellationToken)
    {
        var refund = await returnUseCase.CreateOrUpdateRefund(orderId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ResponseData(refund));
    }
}
