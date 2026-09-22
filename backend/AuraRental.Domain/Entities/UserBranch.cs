namespace AuraRental.Domain.Entities;

public sealed class UserBranch
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public User User { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
