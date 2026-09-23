using System.Text.Json;
using AuraRental.Domain.Enums;

namespace AuraRental.Domain.Entities;

public sealed class Refund
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int Version { get; set; } = 1;
    public RefundStatus Status { get; set; } = RefundStatus.Draft;
    public decimal DepositAmount { get; set; }
    public decimal RentalFee { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal RefundAmount { get; set; }
    public JsonDocument ItemsSnapshot { get; set; } = null!;
    public string? AdjustmentReason { get; set; }
    public int CreatedBy { get; set; }
    public int? SubmittedBy { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Order Order { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public User? Submitter { get; set; }
    public User? Approver { get; set; }
    public ICollection<Payment> Payments { get; set; } = [];
}
