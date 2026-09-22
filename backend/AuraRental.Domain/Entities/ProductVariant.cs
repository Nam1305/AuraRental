namespace AuraRental.Domain.Entities;

public sealed class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Size { get; set; } = null!;
    public string? Measurements { get; set; }
    public decimal ReplacementValue { get; set; }
    public bool IsActive { get; set; } = true;
    public Product Product { get; set; } = null!;
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
    public ICollection<BranchRentalPrice> RentalPrices { get; set; } = [];
}
