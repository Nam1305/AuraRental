namespace AuraRental.Domain.Entities;

public sealed class ReservationItem
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public int InventoryItemId { get; set; }
    public string PackageCode { get; set; } = null!;
    public decimal ReplacementValue { get; set; }
    public decimal RentalPrice { get; set; }
    public decimal OneDayPrice { get; set; }
    public decimal ExtraDayRate { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
