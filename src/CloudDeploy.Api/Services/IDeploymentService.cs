using CloudDeploy.Api.Contracts;
using CloudDeploy.Api.Domain;

namespace CloudDeploy.Api.Services;

public interface IDeploymentService
{
    IReadOnlyList<DeploymentRecord> GetAll(
        string? environment = null,
        DeploymentStatus? status = null);

    DeploymentRecord? GetById(Guid id);

    DeploymentRecord Create(CreateDeploymentRequest request);

    DeploymentRecord? UpdateStatus(Guid id, UpdateDeploymentStatusRequest request);

    bool Delete(Guid id);
}
