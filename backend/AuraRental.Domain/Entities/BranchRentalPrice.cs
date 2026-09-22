namespace AuraRental.Domain.Entities;

public sealed class BranchRentalPrice
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid VariantId { get; set; }
    public string PackageCode { get; set; } = null!;
    public decimal Price { get; set; }
    public Branch Branch { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
