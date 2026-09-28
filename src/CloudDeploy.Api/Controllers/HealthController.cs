using CloudDeploy.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudDeploy.Api.Controllers;

[ApiController]
[Route("healthz")]
[Produces("application/json")]
[DisableRateLimiting]
public sealed class HealthController(
    HealthCheckService healthCheckService,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>Checks whether the API is healthy.</summary>
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);
        var response = new HealthResponse(
            report.Status.ToString().ToLowerInvariant(),
            "cloud-deploy-api",
            timeProvider.GetUtcNow());

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
