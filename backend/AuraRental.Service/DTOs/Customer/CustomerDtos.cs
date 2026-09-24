namespace AuraRental.Service.DTOs.Customer;

public sealed record CustomerDto(
    int Id,
    string Name,
    string Phone,
    string? InstagramHandle,
    string? TiktokHandle,
    string? Address);

public sealed record CustomerListItemDto(
    int Id,
    string Name,
    string Phone,
    string? InstagramHandle,
    string? TiktokHandle,
    string? Address,
    int CompletedOrderCount,
    DateTimeOffset? LastOrderAt);

public sealed record CreateCustomerRequest(
    string Name,
    string Phone,
    string? InstagramHandle,
    string? TiktokHandle,
    string? Address);

public sealed record UpdateCustomerRequest(
    string Name,
    string? InstagramHandle,
    string? TiktokHandle,
    string? Address);

public sealed record CustomerOrderItemDto(string ProductName, string Size, string AssetCode);

public sealed record CustomerOrderHistoryDto(
    int OrderId,
    string OrderNo,
    string BranchCode,
    string BranchName,
    string Status,
    DateTimeOffset RentalStartAt,
    DateTimeOffset RentalEndAt,
    IReadOnlyList<CustomerOrderItemDto> Items,
    decimal RentalFee,
    decimal ProcessingFee,
    DateTimeOffset CreatedAt);
