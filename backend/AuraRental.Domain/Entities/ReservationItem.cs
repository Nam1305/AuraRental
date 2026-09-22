namespace AuraRental.Domain.Entities;

public sealed class ReservationItem
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public Guid InventoryItemId { get; set; }
    public string PackageCode { get; set; } = null!;
    public decimal ReplacementValue { get; set; }
    public decimal RentalPrice { get; set; }
    public decimal OneDayPrice { get; set; }
    public decimal ExtraDayRate { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
