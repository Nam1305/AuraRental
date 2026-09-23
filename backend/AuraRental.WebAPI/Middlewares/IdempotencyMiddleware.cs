using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuraRental.Domain.Entities;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Infrastructure.Persistence;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.WebAPI.Middlewares;

public sealed class IdempotencyMiddleware(
    IDbContextFactory<AuraRentalDbContext> contextFactory,
    IDataProtectionProvider dataProtectionProvider) : IMiddleware
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("AuraRental.Idempotency.Response.v1");

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IdempotentAttribute>() is null)
        {
            await next(context);
            return;
        }

        if (!int.TryParse(context.Request.Headers["Idempotency-Key"], out var key))
        {
            throw new ValidationException("IDEMPOTENCY_KEY_REQUIRED", "Header Idempotency-Key phải là số nguyên hợp lệ.");
        }

        var body = await ReadRequestBody(context.Request);
        var requestHash = Hash($"{context.Request.Method}\n{context.Request.QueryString}\n{body}");
        var scope = GetScope(context);
        var operation = GetOperation(context);
        var record = new IdempotencyRecord
        {
            IdempotencyKey = key,
            Scope = scope,
            Operation = operation,
            RequestHash = requestHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(Retention)
        };

        await using (var database = await contextFactory.CreateDbContextAsync(context.RequestAborted))
        {
            database.IdempotencyRecords.Add(record);
            try
            {
                await database.SaveChangesAsync(context.RequestAborted);
            }
            catch (DbUpdateException)
            {
                database.ChangeTracker.Clear();
                var existing = await database.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(item =>
                    item.Scope == scope && item.Operation == operation && item.IdempotencyKey == key,
                    context.RequestAborted);
                if (existing is null || existing.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    if (existing is not null)
                    {
                        await database.IdempotencyRecords
                            .Where(item => item.Id == existing.Id)
                            .ExecuteDeleteAsync(context.RequestAborted);
                    }

                    throw new ConflictException("IDEMPOTENCY_REQUEST_IN_PROGRESS", "Request trước đang được xử lý; vui lòng thử lại.");
                }

                if (existing.RequestHash != requestHash)
                {
                    throw new ConflictException("IDEMPOTENCY_KEY_REUSED", "Idempotency-Key đã được dùng với payload khác.");
                }

                if (!existing.ResponseStatus.HasValue || existing.ResponsePayload is null)
                {
                    throw new ConflictException("IDEMPOTENCY_REQUEST_IN_PROGRESS", "Request trước đang được xử lý; vui lòng thử lại.");
                }

                context.Response.StatusCode = existing.ResponseStatus.Value;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(_protector.Unprotect(existing.ResponsePayload), context.RequestAborted);
                return;
            }
        }

        var originalBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;
        try
        {
            await next(context);
            responseBuffer.Position = 0;
            var responseBody = await new StreamReader(responseBuffer, Encoding.UTF8, leaveOpen: true).ReadToEndAsync(context.RequestAborted);
            if (context.Response.StatusCode < StatusCodes.Status500InternalServerError)
            {
                await Complete(record.Id, context.Response.StatusCode, responseBody, context.RequestAborted);
            }
            else
            {
                await Delete(record.Id, context.RequestAborted);
            }

            responseBuffer.Position = 0;
            context.Response.Body = originalBody;
            await responseBuffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        catch
        {
            context.Response.Body = originalBody;
            await Delete(record.Id, context.RequestAborted);
            throw;
        }
    }

    private async Task Complete(int recordId, int status, string responseBody, CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var record = await database.IdempotencyRecords.SingleAsync(item => item.Id == recordId, cancellationToken);
        record.ResponseStatus = status;
        record.ResponsePayload = _protector.Protect(responseBody);
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task Delete(int recordId, CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        await database.IdempotencyRecords.Where(item => item.Id == recordId).ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task<string> ReadRequestBody(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(request.HttpContext.RequestAborted);
        request.Body.Position = 0;
        return body;
    }

    private static string GetScope(HttpContext context)
    {
        var subject = context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(subject))
        {
            return $"user:{subject}";
        }

        var token = context.Request.RouteValues["token"]?.ToString() ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return $"public:{Hash(token)}";
    }

    private static string GetOperation(HttpContext context) =>
        $"{context.Request.Method}:{(context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? context.Request.Path}";

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
