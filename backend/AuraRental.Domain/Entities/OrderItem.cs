using AuraRental.Domain.Enums;

namespace AuraRental.Domain.Entities;

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int InventoryItemId { get; set; }
    public string ProductName { get; set; } = null!;
    public string Size { get; set; } = null!;
    public string AssetCode { get; set; } = null!;
    public string PackageCode { get; set; } = null!;
    public decimal ReplacementValue { get; set; }
    public decimal RentalPrice { get; set; }
    public decimal OneDayPrice { get; set; }
    public decimal ExtraDayRate { get; set; }
    public decimal? ActualRentalFee { get; set; }
    public ItemCondition? Condition { get; set; }
    public decimal ProcessingFee { get; set; }
    public string? DamageNote { get; set; }
    public string[] DamagePhotoPaths { get; set; } = [];
    public Order Order { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
