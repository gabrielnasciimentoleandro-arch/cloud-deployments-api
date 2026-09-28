using CloudDeploy.Api.Contracts;
using CloudDeploy.Api.Domain;
using CloudDeploy.Api.Repositories;
using CloudDeploy.Api.Services;

namespace CloudDeploy.Api.Tests;

public sealed class DeploymentServiceTests
{
    [Fact]
    public void Create_NormalizesInputAndUsesUtcClock()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(clock);

        var deployment = service.Create(new CreateDeploymentRequest
        {
            Application = "  orders-api ",
            Environment = " STAGING ",
            Version = " 3.2.1 ",
            Notes = "  first release "
        });

        Assert.Equal("orders-api", deployment.Application);
        Assert.Equal("staging", deployment.Environment);
        Assert.Equal("3.2.1", deployment.Version);
        Assert.Equal("first release", deployment.Notes);
        Assert.Equal(DeploymentStatus.Queued, deployment.Status);
        Assert.Equal(clock.GetUtcNow(), deployment.CreatedAt);
        Assert.Equal(deployment.CreatedAt, deployment.UpdatedAt);
    }

    [Fact]
    public void GetAll_FiltersAndOrdersNewestFirst()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(clock);

        var first = service.Create(CreateRequest("first-api", "production"));
        clock.Advance(TimeSpan.FromMinutes(1));
        var second = service.Create(CreateRequest("second-api", "staging"));
        clock.Advance(TimeSpan.FromMinutes(1));
        var third = service.Create(CreateRequest("third-api", "PRODUCTION"));
        service.UpdateStatus(third.Id, new UpdateDeploymentStatusRequest
        {
            Status = DeploymentStatus.Succeeded
        });

        var all = service.GetAll();
        var production = service.GetAll("Production");
        var succeeded = service.GetAll(status: DeploymentStatus.Succeeded);

        Assert.Equal(new[] { third.Id, second.Id, first.Id }, all.Select(item => item.Id));
        Assert.Equal(new[] { third.Id, first.Id }, production.Select(item => item.Id));
        Assert.Equal(third.Id, Assert.Single(succeeded).Id);
    }

    [Fact]
    public void UpdateStatus_UpdatesTimestampAndHandlesNotes()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(clock);
        var deployment = service.Create(new CreateDeploymentRequest
        {
            Application = "orders-api",
            Environment = "development",
            Version = "1.0.0",
            Notes = "original"
        });

        clock.Advance(TimeSpan.FromMinutes(5));
        var updated = service.UpdateStatus(deployment.Id, new UpdateDeploymentStatusRequest
        {
            Status = DeploymentStatus.InProgress,
            Notes = null
        });

        Assert.NotNull(updated);
        Assert.Equal(DeploymentStatus.InProgress, updated.Status);
        Assert.Equal("original", updated.Notes);
        Assert.Equal(clock.GetUtcNow(), updated.UpdatedAt);

        var cleared = service.UpdateStatus(deployment.Id, new UpdateDeploymentStatusRequest
        {
            Status = DeploymentStatus.Failed,
            Notes = "   "
        });
        Assert.NotNull(cleared);
        Assert.Null(cleared.Notes);
    }

    [Fact]
    public void MissingDeployment_ReturnsExpectedResults()
    {
        var service = CreateService(new TestTimeProvider(DateTimeOffset.UtcNow));
        var id = Guid.NewGuid();

        Assert.Null(service.GetById(id));
        Assert.Null(service.UpdateStatus(id, new UpdateDeploymentStatusRequest
        {
            Status = DeploymentStatus.Failed
        }));
        Assert.False(service.Delete(id));
    }

    [Fact]
    public void Delete_RemovesExistingDeployment()
    {
        var service = CreateService(new TestTimeProvider(DateTimeOffset.UtcNow));
        var deployment = service.Create(CreateRequest("billing-api", "development"));

        Assert.True(service.Delete(deployment.Id));
        Assert.Null(service.GetById(deployment.Id));
        Assert.False(service.Delete(deployment.Id));
    }

    [Fact]
    public void Repository_RejectsDuplicateIdentifier()
    {
        var repository = new InMemoryDeploymentRepository();
        var deployment = new DeploymentRecord(
            Guid.NewGuid(),
            "api",
            "development",
            "1.0.0",
            DeploymentStatus.Queued,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);

        repository.Add(deployment);

        Assert.Throws<InvalidOperationException>(() => repository.Add(deployment));
    }

    private static DeploymentService CreateService(TimeProvider timeProvider) =>
        new(new InMemoryDeploymentRepository(), timeProvider);

    private static CreateDeploymentRequest CreateRequest(string application, string environment) =>
        new()
        {
            Application = application,
            Environment = environment,
            Version = "1.0.0"
        };

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan value) => _utcNow = _utcNow.Add(value);
    }
}
