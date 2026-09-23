using AuraRental.Domain.Entities;

namespace AuraRental.Service.Interface.Persistence;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> Search(string? query, int limit, CancellationToken cancellationToken);
    Task<Customer?> Get(int customerId, CancellationToken cancellationToken);
    Task<Customer?> GetByPhone(string normalizedPhone, CancellationToken cancellationToken);
    Task Add(Customer customer, CancellationToken cancellationToken);
    Task SaveChanges(CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetOrderHistory(
        int customerId,
        int userId,
        string? status,
        int limit,
        CancellationToken cancellationToken);
}
