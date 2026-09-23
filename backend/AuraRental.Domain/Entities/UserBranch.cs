namespace AuraRental.Domain.Entities;

public sealed class UserBranch
{
    public int UserId { get; set; }
    public int BranchId { get; set; }
    public User User { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
