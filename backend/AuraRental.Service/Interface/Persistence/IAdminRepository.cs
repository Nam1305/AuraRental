using AuraRental.Domain.Entities;

namespace AuraRental.Service.Interface.Persistence;

public interface IAdminRepository
{
    Task<IReadOnlyList<Branch>> GetAllBranches(CancellationToken cancellationToken);
    Task<Branch?> GetBranchForUpdate(Guid branchId, CancellationToken cancellationToken);
    Task<bool> BranchCodeExists(string code, CancellationToken cancellationToken);
    Task<bool> BranchHasActiveWork(Guid branchId, CancellationToken cancellationToken);
    void AddBranch(Branch branch);

    Task<IReadOnlyList<User>> SearchUsers(string? query, bool? active, int limit, CancellationToken cancellationToken);
    Task<User?> GetUserForUpdate(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Branch>> GetBranchesByIds(IReadOnlyCollection<Guid> branchIds, CancellationToken cancellationToken);
    void RemoveUserBranches(IEnumerable<UserBranch> accesses);

    Task<Setting?> GetSettings(bool tracking, CancellationToken cancellationToken);
    void AddSettings(Setting setting);
}
