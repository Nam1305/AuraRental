namespace AuraRental.Domain.Entities;

public sealed class IdempotencyRecord
{
    public int Id { get; set; }
    public int IdempotencyKey { get; set; }
    public string Scope { get; set; } = null!;
    public string Operation { get; set; } = null!;
    public string RequestHash { get; set; } = null!;
    public int? ResponseStatus { get; set; }
    public string? ResponsePayload { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
