using AuraRental.Service.Interface.Service;
using AuraRental.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AuraRental.WebAPI.Services;

public sealed class SignalROperationsEventPublisher(
    IHubContext<OperationsHub> hubContext,
    ILogger<SignalROperationsEventPublisher> logger) : IOperationsEventPublisher
{
    public async Task PublishOrderCreated(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients.Group(OperationsHub.GroupName(notification.BranchId))
                .SendAsync("OrderCreated", notification, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to publish OrderCreated for order {OrderId}", notification.OrderId);
        }
    }
}
