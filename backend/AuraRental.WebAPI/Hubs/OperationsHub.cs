using System.Security.Claims;
using AuraRental.Service.Interface.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AuraRental.WebAPI.Hubs;

[Authorize]
public sealed class OperationsHub(IUserRepository userRepository) : Hub
{
    public async Task JoinBranch(Guid branchId)
    {
        var subject = Context.User?.FindFirstValue("sub")
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var userId))
        {
            throw new HubException("UNAUTHORIZED");
        }

        var user = await userRepository.GetById(userId, Context.ConnectionAborted);
        if (user is null || !await userRepository.HasBranchAccess(user.Id, branchId, Context.ConnectionAborted))
        {
            throw new HubException("BRANCH_ACCESS_DENIED");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(branchId), Context.ConnectionAborted);
    }

    public Task LeaveBranch(Guid branchId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(branchId), Context.ConnectionAborted);

    public static string GroupName(Guid branchId) => $"branch:{branchId}";
}
