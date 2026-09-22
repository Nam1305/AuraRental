namespace AuraRental.Domain.Entities;

public sealed class Setting
{
    public int Id { get; set; } = 1;
    public decimal SlotDepositAmount { get; set; } = 100_000;
    public decimal ExtraDayRate { get; set; } = 0.10m;
}
