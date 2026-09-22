using AuraRental.Domain.Enums;

namespace AuraRental.Service.Interface.Service;

public interface IRequestContext
{
    Guid UserId { get; }
    UserRole Role { get; }
    Guid BranchId { get; }
    bool HasBranch { get; }
}

public interface IRequestContextInitializer
{
    void SetUser(Guid userId, UserRole role);
    void SetBranch(Guid branchId);
}
