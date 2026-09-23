using AuraRental.Domain.Enums;

namespace AuraRental.Domain.Entities;

public sealed class InventoryItem
{
    public int Id { get; set; }
    public int VariantId { get; set; }
    public int BranchId { get; set; }
    public string AssetCode { get; set; } = null!;
    public InventoryStatus Status { get; set; } = InventoryStatus.Usable;
    public ProductVariant Variant { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public ICollection<ReservationItem> ReservationItems { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
