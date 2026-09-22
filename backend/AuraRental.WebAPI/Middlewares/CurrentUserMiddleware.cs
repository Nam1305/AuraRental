using System.Security.Claims;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;

namespace AuraRental.WebAPI.Middlewares;

public sealed class CurrentUserMiddleware(
    IUserRepository userRepository,
    IRequestContextInitializer requestContext) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var subject = context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var userId))
        {
            throw new UnauthorizedAccessException("Token không có subject hợp lệ.");
        }

        var user = await userRepository.GetById(userId, context.RequestAborted)
            ?? throw new UnauthorizedAccessException("Tài khoản không tồn tại hoặc đã bị khóa.");
        requestContext.SetUser(user.Id, user.Role);

        await next(context);
    }
}
