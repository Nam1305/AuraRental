namespace AuraRental.Service.DTOs.Identity;

public sealed record BranchSummaryDto(Guid Id, string Code, string Name, string Address, bool IsActive);

public sealed record CurrentUserDto(
    Guid Id,
    string Name,
    string Username,
    string Email,
    string Role,
    IReadOnlyList<BranchSummaryDto> Branches,
    Guid? SuggestedBranchId);
