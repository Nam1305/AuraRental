using AuraRental.Service.DTOs.Identity;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.Utils;

namespace AuraRental.Service.UseCase;

public sealed class IdentityUseCase(
    IUserRepository userRepository,
    IRequestContext requestContext) : IIdentityUseCase
{
    public async Task<CurrentUserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(requestContext.UserId, cancellationToken)
            ?? throw new NotFoundException("USER_NOT_FOUND", "Không tìm thấy tài khoản đang đăng nhập.");
        var branches = await GetBranches(cancellationToken);

        return new CurrentUserDto(
            user.Id,
            user.Name,
            user.Username,
            user.Email,
            ApiText.EnumValue(user.Role),
            branches,
            branches.FirstOrDefault()?.Id);
    }

    public async Task<IReadOnlyList<BranchSummaryDto>> GetBranches(CancellationToken cancellationToken)
    {
        var branches = requestContext.Role == AuraRental.Domain.Enums.UserRole.Manager
            ? await userRepository.GetAllBranches(cancellationToken)
            : await userRepository.GetBranches(requestContext.UserId, cancellationToken);
        return branches
            .Select(branch => new BranchSummaryDto(
                branch.Id,
                branch.Code,
                branch.Name,
                branch.Address,
                branch.IsActive))
            .ToList();
    }
}
