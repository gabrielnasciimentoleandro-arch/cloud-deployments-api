using System.Collections.Concurrent;
using CloudDeploy.Api.Domain;

namespace CloudDeploy.Api.Repositories;

public sealed class InMemoryDeploymentRepository : IDeploymentRepository
{
    private readonly ConcurrentDictionary<Guid, DeploymentRecord> _deployments = new();

    public IReadOnlyCollection<DeploymentRecord> GetAll() =>
        _deployments.Values.ToArray();

    public DeploymentRecord? GetById(Guid id) =>
        _deployments.GetValueOrDefault(id);

    public void Add(DeploymentRecord deployment)
    {
        if (!_deployments.TryAdd(deployment.Id, deployment))
        {
            throw new InvalidOperationException("A deployment with the same identifier already exists.");
        }
    }

    public void Update(DeploymentRecord deployment) =>
        _deployments[deployment.Id] = deployment;

    public bool Delete(Guid id) =>
        _deployments.TryRemove(id, out _);
}
