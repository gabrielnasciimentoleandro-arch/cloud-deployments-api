using System.ComponentModel.DataAnnotations;
using CloudDeploy.Api.Domain;

namespace CloudDeploy.Api.Contracts;

public sealed class UpdateDeploymentStatusRequest
{
    [Required]
    public DeploymentStatus? Status { get; init; }

    [StringLength(300)]
    public string? Notes { get; init; }
}
