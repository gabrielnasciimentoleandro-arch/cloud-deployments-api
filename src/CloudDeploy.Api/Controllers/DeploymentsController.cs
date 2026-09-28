using CloudDeploy.Api.Contracts;
using CloudDeploy.Api.Domain;
using CloudDeploy.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CloudDeploy.Api.Controllers;

[ApiController]
[Route("api/v1/deployments")]
[Produces("application/json")]
public sealed class DeploymentsController(IDeploymentService service) : ControllerBase
{
    /// <summary>Lists deployments, optionally filtered by environment and status.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DeploymentResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DeploymentResponse>> GetAll(
        [FromQuery] string? environment,
        [FromQuery] DeploymentStatus? status)
    {
        var deployments = service
            .GetAll(environment, status)
            .Select(DeploymentResponse.FromDomain)
            .ToArray();

        return Ok(deployments);
    }

    /// <summary>Returns one deployment by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<DeploymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<DeploymentResponse> GetById(Guid id)
    {
        var deployment = service.GetById(id);
        return deployment is null
            ? NotFound(CreateNotFoundProblem(id))
            : Ok(DeploymentResponse.FromDomain(deployment));
    }

    /// <summary>Registers a new deployment with Queued status.</summary>
    [HttpPost]
    [ProducesResponseType<DeploymentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<DeploymentResponse> Create([FromBody] CreateDeploymentRequest request)
    {
        var deployment = service.Create(request);
        var response = DeploymentResponse.FromDomain(deployment);

        return CreatedAtAction(nameof(GetById), new { id = deployment.Id }, response);
    }

    /// <summary>Updates the status and optionally the notes of a deployment.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<DeploymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<DeploymentResponse> UpdateStatus(
        Guid id,
        [FromBody] UpdateDeploymentStatusRequest request)
    {
        var deployment = service.UpdateStatus(id, request);
        return deployment is null
            ? NotFound(CreateNotFoundProblem(id))
            : Ok(DeploymentResponse.FromDomain(deployment));
    }

    /// <summary>Deletes a deployment record.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id) =>
        service.Delete(id)
            ? NoContent()
            : NotFound(CreateNotFoundProblem(id));

    private static ProblemDetails CreateNotFoundProblem(Guid id) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Deployment not found",
        Detail = $"No deployment was found for identifier '{id}'."
    };
}
