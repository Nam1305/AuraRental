using AuraRental.Domain.Enums;

namespace AuraRental.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Guid ReservationId { get; set; }
    public Guid BranchId { get; set; }
    public OrderStatus Status { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string DeliveryAddress { get; set; } = null!;
    public string DeliveryStatus { get; set; } = "NOT_STARTED";
    public string ReturnDeliveryStatus { get; set; } = "NOT_STARTED";
    public string? DeliveryTrackingCode { get; set; }
    public string? ReturnTrackingCode { get; set; }
    public Guid? IdentityVerifiedBy { get; set; }
    public DateTimeOffset? IdentityVerifiedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? ReturnedAt { get; set; }
    public Guid? SettledBy { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Customer Customer { get; set; } = null!;
    public Reservation Reservation { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public User? IdentityVerifier { get; set; }
    public User? Settler { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<Refund> Refunds { get; set; } = [];
}
