using AuraRental.Domain.Enums;
using AuraRental.Service.Interface.Service;

namespace AuraRental.Service.Service;

public sealed class RequestContext : IRequestContext, IRequestContextInitializer
{
    private int? _userId;
    private UserRole? _role;
    private int? _branchId;

    public int UserId => _userId ?? throw new InvalidOperationException("User context has not been initialized.");
    public UserRole Role => _role ?? throw new InvalidOperationException("User context has not been initialized.");
    public int BranchId => _branchId ?? throw new InvalidOperationException("Branch context has not been initialized.");
    public bool HasBranch => _branchId.HasValue;

    public void SetUser(int userId, UserRole role)
    {
        _userId = userId;
        _role = role;
    }

    public void SetBranch(int branchId) => _branchId = branchId;
}
