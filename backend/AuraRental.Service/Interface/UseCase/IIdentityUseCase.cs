using AuraRental.Service.DTOs.Identity;

namespace AuraRental.Service.Interface.UseCase;

public interface IIdentityUseCase
{
    Task<CurrentUserDto> GetCurrentUser(CancellationToken cancellationToken);
    Task<IReadOnlyList<BranchSummaryDto>> GetBranches(CancellationToken cancellationToken);
}
