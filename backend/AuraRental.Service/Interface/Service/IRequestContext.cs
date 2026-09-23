using AuraRental.Domain.Enums;

namespace AuraRental.Service.Interface.Service;

public interface IRequestContext
{
    int UserId { get; }
    UserRole Role { get; }
    int BranchId { get; }
    bool HasBranch { get; }
}

public interface IRequestContextInitializer
{
    void SetUser(int userId, UserRole role);
    void SetBranch(int branchId);
}
