using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.DTOs.Customer;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.Utils;

namespace AuraRental.Service.UseCase;

public sealed class CustomerUseCase(
    ICustomerRepository customerRepository,
    IRequestContext requestContext) : ICustomerUseCase
{
    public async Task<IReadOnlyList<CustomerListItemDto>> Search(
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        ValidateLimit(limit);
        var customers = await customerRepository.Search(query, limit, cancellationToken);
        return customers.Select(customer => new CustomerListItemDto(
            customer.Id,
            customer.Name,
            customer.Phone,
            customer.InstagramHandle,
            customer.Address,
            customer.Orders.Count(order => order.Status == OrderStatus.Completed),
            customer.Orders.Count == 0 ? null : customer.Orders.Max(order => order.CreatedAt))).ToList();
    }

    public async Task<CustomerDto> Get(Guid customerId, CancellationToken cancellationToken) =>
        ToDto(await GetRequired(customerId, cancellationToken));

    public async Task<CustomerDto> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        ValidateName(request.Name);
        var phone = NormalizePhone(request.Phone);
        var existing = await customerRepository.GetByPhone(phone, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException(
                "CUSTOMER_PHONE_EXISTS",
                $"Số điện thoại đã thuộc khách hàng {existing.Name} ({existing.Id}).");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Phone = phone,
            InstagramHandle = NormalizeInstagram(request.InstagramHandle),
            Address = request.Address?.Trim()
        };

        await customerRepository.Add(customer, cancellationToken);
        await customerRepository.SaveChanges(cancellationToken);
        return ToDto(customer);
    }

    public async Task<CustomerDto> Update(
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ValidateName(request.Name);
        var customer = await GetRequired(customerId, cancellationToken);
        customer.Name = request.Name.Trim();
        customer.InstagramHandle = NormalizeInstagram(request.InstagramHandle);
        customer.Address = request.Address?.Trim();
        await customerRepository.SaveChanges(cancellationToken);
        return ToDto(customer);
    }

    public async Task<IReadOnlyList<CustomerOrderHistoryDto>> GetOrderHistory(
        Guid customerId,
        string? status,
        int limit,
        CancellationToken cancellationToken)
    {
        ValidateLimit(limit);
        _ = await GetRequired(customerId, cancellationToken);
        var orders = await customerRepository.GetOrderHistory(
            customerId,
            requestContext.UserId,
            status,
            limit,
            cancellationToken);

        return orders.Select(order => new CustomerOrderHistoryDto(
            order.Id,
            order.OrderNo,
            order.Branch.Code,
            order.Branch.Name,
            ApiText.EnumValue(order.Status),
            order.Reservation.RentalStartAt,
            order.Reservation.RentalEndAt,
            order.Items.Select(item => new CustomerOrderItemDto(item.ProductName, item.Size, item.AssetCode)).ToList(),
            order.Items.Sum(item => item.ActualRentalFee ?? item.RentalPrice),
            order.Items.Sum(item => item.ProcessingFee),
            order.CreatedAt)).ToList();
    }

    private async Task<Customer> GetRequired(Guid customerId, CancellationToken cancellationToken) =>
        await customerRepository.Get(customerId, cancellationToken)
            ?? throw new NotFoundException("CUSTOMER_NOT_FOUND", "Không tìm thấy khách hàng.");

    private static CustomerDto ToDto(Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.Phone,
        customer.InstagramHandle,
        customer.Address);

    private static string NormalizePhone(string phone)
    {
        try
        {
            return PhoneNumberNormalizer.NormalizeVietnamese(phone);
        }
        catch (ArgumentException)
        {
            throw new ValidationException("INVALID_PHONE", "Số điện thoại Việt Nam không hợp lệ.");
        }
    }

    private static string? NormalizeInstagram(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimStart('@');

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("CUSTOMER_NAME_REQUIRED", "Tên khách hàng là bắt buộc.");
        }
    }

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > 100)
        {
            throw new ValidationException("INVALID_LIMIT", "Limit phải nằm trong khoảng 1 đến 100.");
        }
    }
}
