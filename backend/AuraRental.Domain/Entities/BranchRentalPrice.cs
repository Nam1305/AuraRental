namespace AuraRental.Domain.Entities;

public sealed class BranchRentalPrice
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int VariantId { get; set; }
    public string PackageCode { get; set; } = null!;
    public decimal Price { get; set; }
    public Branch Branch { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
