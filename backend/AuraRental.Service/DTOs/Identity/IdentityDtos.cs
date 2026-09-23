namespace AuraRental.Service.DTOs.Identity;

public sealed record BranchSummaryDto(int Id, string Code, string Name, string Address, bool IsActive);

public sealed record CurrentUserDto(
    int Id,
    string Name,
    string Username,
    string Email,
    string Role,
    IReadOnlyList<BranchSummaryDto> Branches,
    int? SuggestedBranchId);
