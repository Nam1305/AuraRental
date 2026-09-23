using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;

namespace AuraRental.Service.Interface.Persistence;

public interface IRentalRepository
{
    Task<Branch?> GetBranch(int branchId, CancellationToken cancellationToken);
    Task<Customer?> GetCustomer(int customerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryItem>> LockInventoryItems(
        int branchId,
        IReadOnlyCollection<int> inventoryItemIds,
        CancellationToken cancellationToken);

    Task<bool> AreInventoryItemsAvailable(
        int branchId,
        IReadOnlyCollection<int> inventoryItemIds,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        int? ignoredReservationId,
        CancellationToken cancellationToken);

    void AddReservation(Reservation reservation);
    Task<Reservation?> LockReservation(int reservationId, CancellationToken cancellationToken);
    Task<Reservation?> GetReservation(
        int reservationId,
        int? branchId,
        bool tracking,
        CancellationToken cancellationToken);
    Task<Reservation?> GetReservationByFormTokenHash(string tokenHash, CancellationToken cancellationToken);
    Task<IReadOnlyList<Reservation>> SearchReservations(
        int branchId,
        IReadOnlyCollection<ReservationStatus> statuses,
        string? query,
        int limit,
        CancellationToken cancellationToken);

    void AddPayment(Payment payment);
    Task<Payment?> GetPayment(int paymentId, int branchId, bool tracking, CancellationToken cancellationToken);

    void AddOrder(Order order);
    Task<Order?> LockOrder(int orderId, CancellationToken cancellationToken);
    Task<Order?> GetOrder(int orderId, int branchId, bool tracking, CancellationToken cancellationToken);
    Task<Order?> GetOrderByReservationId(int reservationId, bool tracking, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> SearchOrders(
        int branchId,
        IReadOnlyCollection<OrderStatus> statuses,
        string? query,
        DateOnly? from,
        DateOnly? to,
        int limit,
        CancellationToken cancellationToken);
}
