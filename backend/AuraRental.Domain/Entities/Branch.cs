namespace AuraRental.Domain.Entities;

public sealed class Branch
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<UserBranch> UserBranches { get; set; } = [];
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
    public ICollection<BranchRentalPrice> RentalPrices { get; set; } = [];
}
