using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;

namespace AuraRental.Service.Interface.Persistence;

public interface IReturnRepository
{
    Task<IReadOnlyList<Order>> GetQueue(
        int branchId,
        IReadOnlyCollection<OrderStatus> statuses,
        int limit,
        CancellationToken cancellationToken);
    Task<Refund?> LockRefund(int refundId, CancellationToken cancellationToken);
    Task<Refund?> GetRefund(int refundId, int branchId, bool tracking, CancellationToken cancellationToken);
    Task<Refund?> GetOpenRefundForOrder(int orderId, bool tracking, CancellationToken cancellationToken);
    Task<int> GetLatestVersion(int orderId, CancellationToken cancellationToken);
    Task<bool> HasNewerOpenRevision(int orderId, int version, CancellationToken cancellationToken);
    Task<IReadOnlyList<Refund>> GetApprovedRefunds(int orderId, CancellationToken cancellationToken);
    void AddRefund(Refund refund);
}
