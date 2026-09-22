using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.WebAPI.Security;

namespace AuraRental.WebAPI.Middlewares;

public sealed class BranchAccessMiddleware(
    IUserRepository userRepository,
    IRequestContext requestContext,
    IRequestContextInitializer initializer) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<RequireBranchAttribute>() is null)
        {
            await next(context);
            return;
        }

        if (!Guid.TryParse(context.Request.Headers["X-Branch-Id"], out var branchId))
        {
            throw new ValidationException("BRANCH_REQUIRED", "Header X-Branch-Id là bắt buộc.");
        }

        if (!await userRepository.HasBranchAccess(requestContext.UserId, branchId, context.RequestAborted))
        {
            throw new ForbiddenException("BRANCH_ACCESS_DENIED", "Bạn không có quyền truy cập chi nhánh này.");
        }

        initializer.SetBranch(branchId);
        await next(context);
    }
}
