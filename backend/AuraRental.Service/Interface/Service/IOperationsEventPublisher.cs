namespace AuraRental.Service.Interface.Service;

public sealed record OrderCreatedEvent(
    string Type,
    Guid BranchId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string OrderNo,
    string CustomerName);

public interface IOperationsEventPublisher
{
    Task PublishOrderCreated(OrderCreatedEvent notification, CancellationToken cancellationToken);
}
