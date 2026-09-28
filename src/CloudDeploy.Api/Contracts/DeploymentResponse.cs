using CloudDeploy.Api.Domain;

namespace CloudDeploy.Api.Contracts;

public sealed record DeploymentResponse(
    Guid Id,
    string Application,
    string Environment,
    string Version,
    DeploymentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Notes)
{
    public static DeploymentResponse FromDomain(DeploymentRecord deployment) =>
        new(
            deployment.Id,
            deployment.Application,
            deployment.Environment,
            deployment.Version,
            deployment.Status,
            deployment.CreatedAt,
            deployment.UpdatedAt,
            deployment.Notes);
}
