using AuraRental.Domain.Enums;

namespace AuraRental.Domain.Entities;

public sealed class Payment
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public int? RefundId { get; set; }
    public PaymentType Type { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = null!;
    public PaymentStatus Status { get; set; } = PaymentStatus.Recorded;
    public string? TransactionRef { get; set; }
    public string? ProofPath { get; set; }
    public int RecordedBy { get; set; }
    public int? ConfirmedBy { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? Note { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public Refund? Refund { get; set; }
    public User Recorder { get; set; } = null!;
    public User? Confirmer { get; set; }
}
