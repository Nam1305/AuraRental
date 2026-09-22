using AuraRental.Service.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ApiResponse<T> ResponseData<T>(T data, string? nextCursor = null, bool? hasMore = null) =>
        new(data, new ApiMeta(HttpContext.TraceIdentifier, nextCursor, hasMore));
}
