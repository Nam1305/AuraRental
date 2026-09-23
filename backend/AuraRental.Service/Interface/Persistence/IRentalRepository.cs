using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;

namespace AuraRental.Service.Interface.Persistence;

public interface IRentalRepository
{
    Task<Branch?> GetBranch(Guid branchId, CancellationToken cancellationToken);
    Task<Customer?> GetCustomer(Guid customerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryItem>> LockInventoryItems(
        Guid branchId,
        IReadOnlyCollection<Guid> inventoryItemIds,
        CancellationToken cancellationToken);

    Task<bool> AreInventoryItemsAvailable(
        Guid branchId,
        IReadOnlyCollection<Guid> inventoryItemIds,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Guid? ignoredReservationId,
        CancellationToken cancellationToken);

    void AddReservation(Reservation reservation);
    Task<Reservation?> LockReservation(Guid reservationId, CancellationToken cancellationToken);
    Task<Reservation?> GetReservation(
        Guid reservationId,
        Guid? branchId,
        bool tracking,
        CancellationToken cancellationToken);
    Task<Reservation?> GetReservationByFormTokenHash(string tokenHash, CancellationToken cancellationToken);
    Task<IReadOnlyList<Reservation>> SearchReservations(
        Guid branchId,
        IReadOnlyCollection<ReservationStatus> statuses,
        string? query,
        int limit,
        CancellationToken cancellationToken);

    void AddPayment(Payment payment);
    Task<Payment?> GetPayment(Guid paymentId, Guid branchId, bool tracking, CancellationToken cancellationToken);

    void AddOrder(Order order);
    Task<Order?> LockOrder(Guid orderId, CancellationToken cancellationToken);
    Task<Order?> GetOrder(Guid orderId, Guid branchId, bool tracking, CancellationToken cancellationToken);
    Task<Order?> GetOrderByReservationId(Guid reservationId, bool tracking, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> SearchOrders(
        Guid branchId,
        IReadOnlyCollection<OrderStatus> statuses,
        string? query,
        DateOnly? from,
        DateOnly? to,
        int limit,
        CancellationToken cancellationToken);
}
