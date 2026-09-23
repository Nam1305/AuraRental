using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class AdminRepository(AuraRentalDbContext context) : IAdminRepository
{
    public async Task<IReadOnlyList<Branch>> GetAllBranches(CancellationToken cancellationToken) =>
        await context.Branches.AsNoTracking().OrderBy(branch => branch.Code).ToListAsync(cancellationToken);

    public Task<Branch?> GetBranchForUpdate(Guid branchId, CancellationToken cancellationToken) =>
        context.Branches.FirstOrDefaultAsync(branch => branch.Id == branchId, cancellationToken);

    public Task<bool> BranchCodeExists(string code, CancellationToken cancellationToken) =>
        context.Branches.AnyAsync(branch => branch.Code == code, cancellationToken);

    public async Task<bool> BranchHasActiveWork(Guid branchId, CancellationToken cancellationToken)
    {
        var hasReservation = await context.Reservations.AnyAsync(reservation =>
            reservation.BranchId == branchId &&
            reservation.Status == ReservationStatus.Active, cancellationToken);
        return hasReservation || await context.Orders.AnyAsync(order =>
            order.BranchId == branchId &&
            order.Status != OrderStatus.Completed &&
            order.Status != OrderStatus.Cancelled,
            cancellationToken);
    }

    public void AddBranch(Branch branch) => context.Branches.Add(branch);

    public async Task<IReadOnlyList<User>> SearchUsers(
        string? query,
        bool? active,
        int limit,
        CancellationToken cancellationToken)
    {
        var users = context.Users.AsNoTracking().Include(user => user.UserBranches).ThenInclude(access => access.Branch).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var value = query.Trim();
            users = users.Where(user =>
                EF.Functions.ILike(user.Name, $"%{value}%") ||
                EF.Functions.ILike(user.Username, $"%{value}%") ||
                EF.Functions.ILike(user.Email, $"%{value}%"));
        }

        if (active.HasValue)
        {
            users = users.Where(user => user.IsActive == active.Value);
        }

        return await users.OrderBy(user => user.Name).Take(limit).ToListAsync(cancellationToken);
    }

    public Task<User?> GetUserForUpdate(Guid userId, CancellationToken cancellationToken) =>
        context.Users
            .Include(user => user.UserBranches)
                .ThenInclude(access => access.Branch)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<Branch>> GetBranchesByIds(
        IReadOnlyCollection<Guid> branchIds,
        CancellationToken cancellationToken) =>
        await context.Branches.Where(branch => branchIds.Contains(branch.Id) && branch.IsActive).ToListAsync(cancellationToken);

    public void RemoveUserBranches(IEnumerable<UserBranch> accesses) => context.UserBranches.RemoveRange(accesses);

    public Task<Setting?> GetSettings(bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Setting> settings = context.Settings;
        if (!tracking)
        {
            settings = settings.AsNoTracking();
        }

        return settings.SingleOrDefaultAsync(setting => setting.Id == 1, cancellationToken);
    }

    public void AddSettings(Setting setting) => context.Settings.Add(setting);
}
