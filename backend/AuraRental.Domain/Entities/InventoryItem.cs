using AuraRental.Domain.Enums;

namespace AuraRental.Domain.Entities;

public sealed class InventoryItem
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid BranchId { get; set; }
    public string AssetCode { get; set; } = null!;
    public InventoryStatus Status { get; set; } = InventoryStatus.Usable;
    public int CleaningHours { get; set; } = 12;
    public DateTimeOffset? CleaningUntil { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public ICollection<ReservationItem> ReservationItems { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
