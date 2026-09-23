using AuraRental.Domain.Entities;

namespace AuraRental.Service.Interface.Persistence;

public interface IUserRepository
{
    Task<User?> GetByLoginIdentifier(string identifier, bool tracking, CancellationToken cancellationToken);
    Task<User?> GetById(int userId, CancellationToken cancellationToken);
    Task<bool> HasBranchAccess(int userId, int branchId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Branch>> GetBranches(int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Branch>> GetAllBranches(CancellationToken cancellationToken);
}
