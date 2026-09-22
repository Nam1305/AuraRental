using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;

namespace AuraRental.Service.Interface.Persistence;

public interface IReturnRepository
{
    Task<IReadOnlyList<Order>> GetQueue(
        Guid branchId,
        IReadOnlyCollection<OrderStatus> statuses,
        int limit,
        CancellationToken cancellationToken);
    Task<Refund?> LockRefund(Guid refundId, CancellationToken cancellationToken);
    Task<Refund?> GetRefund(Guid refundId, Guid branchId, bool tracking, CancellationToken cancellationToken);
    Task<Refund?> GetOpenRefundForOrder(Guid orderId, bool tracking, CancellationToken cancellationToken);
    Task<int> GetLatestVersion(Guid orderId, CancellationToken cancellationToken);
    Task<bool> HasNewerOpenRevision(Guid orderId, int version, CancellationToken cancellationToken);
    Task<IReadOnlyList<Refund>> GetApprovedRefunds(Guid orderId, CancellationToken cancellationToken);
    void AddRefund(Refund refund);
}
