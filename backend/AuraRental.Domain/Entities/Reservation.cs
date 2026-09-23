using AuraRental.Domain.Enums;

namespace AuraRental.Domain.Entities;

public sealed class Reservation
{
    public int Id { get; set; }
    public string ReservationNo { get; set; } = null!;
    // A reservation is created before the customer completes the public rental form.
    // The customer is attached when that form is submitted.
    public int? CustomerId { get; set; }
    public int BranchId { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTimeOffset RentalStartAt { get; set; }
    public DateTimeOffset RentalEndAt { get; set; }
    public string DepositPlan { get; set; } = null!;
    public decimal DepositRequired { get; set; }
    public string? OtpHash { get; set; }
    public string? FormTokenHash { get; set; }
    public DateTimeOffset? OtpExpiresAt { get; set; }
    public DateTimeOffset? OtpUsedAt { get; set; }
    public string? CancellationReason { get; set; }
    public int CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Customer? Customer { get; set; }
    public Branch Branch { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<ReservationItem> Items { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public Order? Order { get; set; }
}
