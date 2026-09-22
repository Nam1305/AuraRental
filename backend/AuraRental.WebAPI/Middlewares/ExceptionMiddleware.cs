using System.Net;
using System.Text.Json;
using AuraRental.Service.DTOs.Common;
using AuraRental.Service.Exceptions;

namespace AuraRental.WebAPI.Middlewares;

public sealed class ExceptionMiddleware(
    ILogger<ExceptionMiddleware> logger,
    IHostEnvironment environment) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Request {RequestId} failed for {Method} {Path}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var (status, code, message, fields) = Map(exception);
            context.Response.Clear();
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";

            var response = new ApiErrorResponse(new ApiErrorBody(
                code,
                message,
                fields,
                context.TraceIdentifier));
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions), context.RequestAborted);
        }
    }

    private (HttpStatusCode Status, string Code, string Message, IReadOnlyDictionary<string, string>? Fields) Map(
        Exception exception) => exception switch
        {
            AppException appException => (
                appException.StatusCode,
                appException.Code,
                appException.Message,
                appException.Fields),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "UNAUTHORIZED",
                exception.Message,
                null),
            ArgumentException => (
                HttpStatusCode.BadRequest,
                "BAD_REQUEST",
                exception.Message,
                null),
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                environment.IsDevelopment() ? exception.Message : "Đã xảy ra lỗi không mong muốn.",
                null)
        };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
