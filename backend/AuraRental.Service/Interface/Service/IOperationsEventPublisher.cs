namespace AuraRental.Service.Interface.Service;

public sealed record OrderCreatedEvent(
    string Type,
    int BranchId,
    DateTimeOffset OccurredAt,
    int OrderId,
    string OrderNo,
    string CustomerName);

public interface IOperationsEventPublisher
{
    Task PublishOrderCreated(OrderCreatedEvent notification, CancellationToken cancellationToken);
}
