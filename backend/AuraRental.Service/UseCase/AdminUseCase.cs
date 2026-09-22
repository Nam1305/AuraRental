using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.DTOs.Admin;
using AuraRental.Service.DTOs.Identity;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.Utils;

namespace AuraRental.Service.UseCase;

public sealed class AdminUseCase(
    IAdminRepository adminRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext) : IAdminUseCase
{
    public async Task<IReadOnlyList<BranchSummaryDto>> GetBranches(CancellationToken cancellationToken)
    {
        var branches = requestContext.Role == UserRole.Manager
            ? await adminRepository.GetAllBranches(cancellationToken)
            : await userRepository.GetBranches(requestContext.UserId, cancellationToken);
        return branches.Select(ToBranch).ToList();
    }

    public async Task<BranchSummaryDto> CreateBranch(
        CreateBranchRequest request,
        CancellationToken cancellationToken)
    {
        EnsureManager();
        ValidateBranch(request.Code, request.Name, request.Address);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await adminRepository.BranchCodeExists(code, cancellationToken))
        {
            throw new ConflictException("BRANCH_CODE_EXISTS", "Mã chi nhánh đã tồn tại.");
        }

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            Address = request.Address.Trim()
        };
        adminRepository.AddBranch(branch);
        await unitOfWork.SaveChanges(cancellationToken);
        return ToBranch(branch);
    }

    public async Task<BranchSummaryDto> UpdateBranch(
        Guid branchId,
        UpdateBranchRequest request,
        CancellationToken cancellationToken)
    {
        EnsureManager();
        ValidateBranch("unchanged", request.Name, request.Address);
        var branch = await adminRepository.GetBranchForUpdate(branchId, cancellationToken)
            ?? throw new NotFoundException("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh.");
        if (!request.IsActive && branch.IsActive && await adminRepository.BranchHasActiveWork(branch.Id, cancellationToken))
        {
            throw new ConflictException("BRANCH_HAS_ACTIVE_WORK", "Không thể khóa chi nhánh còn reservation hoặc order chưa hoàn tất.");
        }

        branch.Name = request.Name.Trim();
        branch.Address = request.Address.Trim();
        branch.IsActive = request.IsActive;
        await unitOfWork.SaveChanges(cancellationToken);
        return ToBranch(branch);
    }

    public async Task<IReadOnlyList<UserListItemDto>> SearchUsers(
        string? query,
        bool? active,
        int limit,
        CancellationToken cancellationToken)
    {
        EnsureManager();
        ValidateLimit(limit);
        var users = await adminRepository.SearchUsers(query, active, limit, cancellationToken);
        return users.Select(ToUser).ToList();
    }

    public async Task<UserListItemDto> ReplaceUserBranches(
        Guid userId,
        ReplaceUserBranchesRequest request,
        CancellationToken cancellationToken)
    {
        EnsureManager();
        var branchIds = request.BranchIds.Distinct().ToArray();
        if (branchIds.Length == 0)
        {
            throw new ValidationException("USER_BRANCH_REQUIRED", "Tài khoản active phải có ít nhất một chi nhánh.");
        }

        var user = await adminRepository.GetUserForUpdate(userId, cancellationToken)
            ?? throw new NotFoundException("USER_NOT_FOUND", "Không tìm thấy nhân viên.");
        var branches = await adminRepository.GetBranchesByIds(branchIds, cancellationToken);
        if (branches.Count != branchIds.Length)
        {
            throw new ValidationException("INVALID_BRANCH_IDS", "Có chi nhánh không tồn tại hoặc đã bị khóa.");
        }

        var desired = branchIds.ToHashSet();
        var removed = user.UserBranches.Where(access => !desired.Contains(access.BranchId)).ToList();
        adminRepository.RemoveUserBranches(removed);
        foreach (var access in removed)
        {
            user.UserBranches.Remove(access);
        }
        var existing = user.UserBranches.Select(access => access.BranchId).ToHashSet();
        foreach (var branchId in branchIds.Where(id => !existing.Contains(id)))
        {
            user.UserBranches.Add(new UserBranch
            {
                UserId = user.Id,
                BranchId = branchId,
                Branch = branches.Single(branch => branch.Id == branchId)
            });
        }

        await unitOfWork.SaveChanges(cancellationToken);
        return ToUser(user);
    }

    public async Task<SettingsDto> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await adminRepository.GetSettings(false, cancellationToken) ?? new Setting();
        return ToSettings(settings);
    }

    public async Task<SettingsDto> UpdateSettings(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        EnsureManager();
        ValidateSettings(request);
        var settings = await adminRepository.GetSettings(true, cancellationToken);
        if (settings is null)
        {
            settings = new Setting();
            adminRepository.AddSettings(settings);
        }

        settings.SlotDepositAmount = request.SlotDepositAmount;
        settings.DefaultCleaningHours = request.DefaultCleaningHours;
        settings.ExtraDayRate = request.ExtraDayRate;
        await unitOfWork.SaveChanges(cancellationToken);
        return ToSettings(settings);
    }

    private static BranchSummaryDto ToBranch(Branch branch) =>
        new(branch.Id, branch.Code, branch.Name, branch.Address, branch.IsActive);

    private static UserListItemDto ToUser(User user) => new(
        user.Id,
        user.Name,
        user.Username,
        user.Email,
        ApiText.EnumValue(user.Role),
        user.IsActive,
        user.UserBranches.OrderBy(access => access.Branch.Code).Select(access => new UserBranchDto(
            access.Branch.Id,
            access.Branch.Code,
            access.Branch.Name)).ToList());

    private static SettingsDto ToSettings(Setting setting) => new(
        setting.SlotDepositAmount,
        setting.DefaultCleaningHours,
        setting.ExtraDayRate,
        ["12H", "1D", "2D", "3D"]);

    private void EnsureManager()
    {
        if (requestContext.Role != UserRole.Manager)
        {
            throw new ForbiddenException("MANAGER_REQUIRED", "Thao tác này chỉ dành cho manager.");
        }
    }

    private static void ValidateBranch(string code, string name, string address)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
        {
            throw new ValidationException("BRANCH_FIELDS_REQUIRED", "Mã, tên và địa chỉ chi nhánh là bắt buộc.");
        }
    }

    private static void ValidateSettings(UpdateSettingsRequest request)
    {
        if (request.SlotDepositAmount <= 0 || request.DefaultCleaningHours is < 0 or > 168 ||
            request.ExtraDayRate is <= 0 or > 1)
        {
            throw new ValidationException("INVALID_SETTINGS", "Cọc giữ chỗ phải lớn hơn 0 và cleaning hours từ 0 đến 168.");
        }
    }

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > 100)
        {
            throw new ValidationException("INVALID_LIMIT", "Limit phải nằm trong khoảng 1 đến 100.");
        }
    }
}
