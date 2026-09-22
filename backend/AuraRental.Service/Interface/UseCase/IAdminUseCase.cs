using AuraRental.Service.DTOs.Admin;
using AuraRental.Service.DTOs.Identity;

namespace AuraRental.Service.Interface.UseCase;

public interface IAdminUseCase
{
    Task<IReadOnlyList<BranchSummaryDto>> GetBranches(CancellationToken cancellationToken);
    Task<BranchSummaryDto> CreateBranch(CreateBranchRequest request, CancellationToken cancellationToken);
    Task<BranchSummaryDto> UpdateBranch(Guid branchId, UpdateBranchRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserListItemDto>> SearchUsers(string? query, bool? active, int limit, CancellationToken cancellationToken);
    Task<UserListItemDto> ReplaceUserBranches(Guid userId, ReplaceUserBranchesRequest request, CancellationToken cancellationToken);
    Task<SettingsDto> GetSettings(CancellationToken cancellationToken);
    Task<SettingsDto> UpdateSettings(UpdateSettingsRequest request, CancellationToken cancellationToken);
}
