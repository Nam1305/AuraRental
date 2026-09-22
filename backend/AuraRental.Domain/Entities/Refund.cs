using System.Text.Json;
using AuraRental.Domain.Enums;

namespace AuraRental.Domain.Entities;

public sealed class Refund
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public int Version { get; set; } = 1;
    public RefundStatus Status { get; set; } = RefundStatus.Draft;
    public decimal DepositAmount { get; set; }
    public decimal RentalFee { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal RefundAmount { get; set; }
    public JsonDocument ItemsSnapshot { get; set; } = null!;
    public string? AdjustmentReason { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? SubmittedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Order Order { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public User? Submitter { get; set; }
    public User? Approver { get; set; }
    public ICollection<Payment> Payments { get; set; } = [];
}
