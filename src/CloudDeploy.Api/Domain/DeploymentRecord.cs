namespace CloudDeploy.Api.Domain;

public sealed record DeploymentRecord(
    Guid Id,
    string Application,
    string Environment,
    string Version,
    DeploymentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Notes);
