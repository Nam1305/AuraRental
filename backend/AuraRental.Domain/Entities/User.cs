using AuraRental.Domain.Enums;

namespace AuraRental.Domain.Entities;

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<UserBranch> UserBranches { get; set; } = [];
}
