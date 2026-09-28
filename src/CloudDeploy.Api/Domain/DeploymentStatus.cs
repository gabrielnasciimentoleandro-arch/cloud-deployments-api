namespace CloudDeploy.Api.Domain;

public enum DeploymentStatus
{
    Queued,
    InProgress,
    Succeeded,
    Failed,
    RolledBack
}
