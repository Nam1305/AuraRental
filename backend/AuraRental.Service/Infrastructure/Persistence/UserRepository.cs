using AuraRental.Domain.Entities;
using AuraRental.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class UserRepository(AuraRentalDbContext context) : IUserRepository
{
    public Task<User?> GetByLoginIdentifier(
        string identifier,
        bool tracking,
        CancellationToken cancellationToken)
    {
        IQueryable<User> users = context.Users;
        if (!tracking)
        {
            users = users.AsNoTracking();
        }

        return users.FirstOrDefaultAsync(
            user => user.IsActive && (user.Username == identifier || user.Email == identifier),
            cancellationToken);
    }

    public Task<User?> GetById(Guid userId, CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId && user.IsActive, cancellationToken);

    public Task<bool> HasBranchAccess(Guid userId, Guid branchId, CancellationToken cancellationToken) =>
        context.Branches.AnyAsync(branch =>
            branch.Id == branchId &&
            branch.IsActive &&
            (branch.UserBranches.Any(item => item.UserId == userId) ||
             context.Users.Any(user => user.Id == userId && user.IsActive && user.Role == AuraRental.Domain.Enums.UserRole.Manager)),
            cancellationToken);

    public async Task<IReadOnlyList<Branch>> GetBranches(Guid userId, CancellationToken cancellationToken) =>
        await context.UserBranches
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Branch.IsActive)
            .Select(item => item.Branch)
            .OrderBy(item => item.Code)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Branch>> GetAllBranches(CancellationToken cancellationToken) =>
        await context.Branches.AsNoTracking().Where(branch => branch.IsActive).OrderBy(branch => branch.Code).ToListAsync(cancellationToken);
}
