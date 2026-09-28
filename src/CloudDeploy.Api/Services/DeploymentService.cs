using CloudDeploy.Api.Contracts;
using CloudDeploy.Api.Domain;
using CloudDeploy.Api.Repositories;

namespace CloudDeploy.Api.Services;

public sealed class DeploymentService(
    IDeploymentRepository repository,
    TimeProvider timeProvider) : IDeploymentService
{
    public IReadOnlyList<DeploymentRecord> GetAll(
        string? environment = null,
        DeploymentStatus? status = null)
    {
        IEnumerable<DeploymentRecord> deployments = repository.GetAll();

        if (!string.IsNullOrWhiteSpace(environment))
        {
            deployments = deployments.Where(deployment =>
                deployment.Environment.Equals(
                    environment.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        if (status.HasValue)
        {
            deployments = deployments.Where(deployment => deployment.Status == status.Value);
        }

        return deployments
            .OrderByDescending(deployment => deployment.CreatedAt)
            .ToArray();
    }

    public DeploymentRecord? GetById(Guid id) =>
        repository.GetById(id);

    public DeploymentRecord Create(CreateDeploymentRequest request)
    {
        var now = timeProvider.GetUtcNow();
        var deployment = new DeploymentRecord(
            Guid.NewGuid(),
            request.Application.Trim(),
            request.Environment.Trim().ToLowerInvariant(),
            request.Version.Trim(),
            DeploymentStatus.Queued,
            now,
            now,
            NormalizeNotes(request.Notes));

        repository.Add(deployment);
        return deployment;
    }

    public DeploymentRecord? UpdateStatus(Guid id, UpdateDeploymentStatusRequest request)
    {
        var current = repository.GetById(id);
        if (current is null)
        {
            return null;
        }

        var updated = current with
        {
            Status = request.Status!.Value,
            UpdatedAt = timeProvider.GetUtcNow(),
            Notes = request.Notes is null
                ? current.Notes
                : NormalizeNotes(request.Notes)
        };

        repository.Update(updated);
        return updated;
    }

    public bool Delete(Guid id) =>
        repository.Delete(id);

    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
