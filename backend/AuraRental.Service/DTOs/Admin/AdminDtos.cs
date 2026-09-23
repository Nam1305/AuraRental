namespace AuraRental.Service.DTOs.Admin;

public sealed record CreateBranchRequest(string Code, string Name, string Address);
public sealed record UpdateBranchRequest(string Name, string Address, bool IsActive);

public sealed record UserListItemDto(
    int Id,
    string Name,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    IReadOnlyList<UserBranchDto> Branches);

public sealed record UserBranchDto(int Id, string Code, string Name);
public sealed record ReplaceUserBranchesRequest(IReadOnlyList<int> BranchIds);

public sealed record SettingsDto(
    decimal SlotDepositAmount,
    decimal ExtraDayRate,
    IReadOnlyList<string> SupportedPackageCodes);

public sealed record UpdateSettingsRequest(decimal SlotDepositAmount, decimal ExtraDayRate);
