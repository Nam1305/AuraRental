using AuraRental.Domain.Entities;

namespace AuraRental.Service.Interface.Persistence;

public interface IUserRepository
{
    Task<User?> GetByLoginIdentifier(string identifier, bool tracking, CancellationToken cancellationToken);
    Task<User?> GetById(Guid userId, CancellationToken cancellationToken);
    Task<bool> HasBranchAccess(Guid userId, Guid branchId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Branch>> GetBranches(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Branch>> GetAllBranches(CancellationToken cancellationToken);
}
