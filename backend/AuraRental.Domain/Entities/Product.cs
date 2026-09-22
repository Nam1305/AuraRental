namespace AuraRental.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string? Color { get; set; }
    public string? Material { get; set; }
    public string? Description { get; set; }
    public string[] ImagePaths { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public ICollection<ProductVariant> Variants { get; set; } = [];
}
