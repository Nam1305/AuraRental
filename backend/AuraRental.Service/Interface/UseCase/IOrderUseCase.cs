using AuraRental.Service.DTOs.Order;

namespace AuraRental.Service.Interface.UseCase;

public interface IOrderUseCase
{
    Task<IReadOnlyList<OrderListItemDto>> Search(
        string? status,
        string? query,
        DateOnly? from,
        DateOnly? to,
        int limit,
        CancellationToken cancellationToken);
    Task<OrderDetailDto> Get(Guid orderId, CancellationToken cancellationToken);
    Task<VerifyIdentityDto> VerifyIdentity(Guid orderId, VerifyIdentityRequest request, CancellationToken cancellationToken);
    Task<CancelOrderDto> Cancel(Guid orderId, CancelOrderRequest request, CancellationToken cancellationToken);
    Task<OrderTransitionDto> Prepare(Guid orderId, CancellationToken cancellationToken);
    Task<OrderTransitionDto> StartDelivery(Guid orderId, StartDeliveryRequest request, CancellationToken cancellationToken);
    Task<OrderTransitionDto> CompleteDelivery(Guid orderId, CompleteDeliveryRequest request, CancellationToken cancellationToken);
    Task<OrderTransitionDto> StartReturnDelivery(Guid orderId, StartDeliveryRequest request, CancellationToken cancellationToken);
    Task<OrderTransitionDto> CompleteReturnDelivery(Guid orderId, CompleteReturnDeliveryRequest request, CancellationToken cancellationToken);
    Task<PaymentActionDto> ConfirmPayment(Guid paymentId, CancellationToken cancellationToken);
    Task<PaymentActionDto> VoidPayment(Guid paymentId, PaymentActionRequest request, CancellationToken cancellationToken);
}
