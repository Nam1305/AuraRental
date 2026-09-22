using AuraRental.Service.DTOs.Customer;

namespace AuraRental.Service.Interface.UseCase;

public interface ICustomerUseCase
{
    Task<IReadOnlyList<CustomerListItemDto>> Search(string? query, int limit, CancellationToken cancellationToken);
    Task<CustomerDto> Get(Guid customerId, CancellationToken cancellationToken);
    Task<CustomerDto> Create(CreateCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerDto> Update(Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomerOrderHistoryDto>> GetOrderHistory(
        Guid customerId,
        string? status,
        int limit,
        CancellationToken cancellationToken);
}
