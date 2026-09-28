using CloudDeploy.Api.Domain;

namespace CloudDeploy.Api.Repositories;

public interface IDeploymentRepository
{
    IReadOnlyCollection<DeploymentRecord> GetAll();

    DeploymentRecord? GetById(Guid id);

    void Add(DeploymentRecord deployment);

    void Update(DeploymentRecord deployment);

    bool Delete(Guid id);
}
