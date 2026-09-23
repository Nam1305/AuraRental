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
    Task<OrderDetailDto> Get(int orderId, CancellationToken cancellationToken);
    Task<VerifyIdentityDto> VerifyIdentity(int orderId, VerifyIdentityRequest request, CancellationToken cancellationToken);
    Task<CancelOrderDto> Cancel(int orderId, CancelOrderRequest request, CancellationToken cancellationToken);
    Task<OrderTransitionDto> Prepare(int orderId, CancellationToken cancellationToken);
    Task<OrderTransitionDto> StartDelivery(int orderId, StartDeliveryRequest request, CancellationToken cancellationToken);
    Task<OrderTransitionDto> CompleteDelivery(int orderId, CompleteDeliveryRequest request, CancellationToken cancellationToken);
    Task<OrderTransitionDto> StartReturnDelivery(int orderId, StartDeliveryRequest request, CancellationToken cancellationToken);
    Task<OrderTransitionDto> CompleteReturnDelivery(int orderId, CompleteReturnDeliveryRequest request, CancellationToken cancellationToken);
    Task<PaymentActionDto> ConfirmPayment(int paymentId, CancellationToken cancellationToken);
    Task<PaymentActionDto> VoidPayment(int paymentId, PaymentActionRequest request, CancellationToken cancellationToken);
}
