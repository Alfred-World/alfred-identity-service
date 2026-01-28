using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[ApiController]
[Route("")]
[AllowAnonymous]
public class HealthController : BaseApiController
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <remarks>
    /// Returns 200 OK if the service is up and running.
    /// Used by load balancers and monitoring tools.
    /// </remarks>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return OkResponse(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
}
