using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[ApiController]
[Route("health")]
[Route("api/v1/health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "aura-rental-api",
        timestamp = DateTimeOffset.UtcNow
    });
}
