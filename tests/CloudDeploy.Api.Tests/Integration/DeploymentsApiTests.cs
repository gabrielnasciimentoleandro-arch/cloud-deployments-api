using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CloudDeploy.Api.Contracts;
using CloudDeploy.Api.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudDeploy.Api.Tests.Integration;

public sealed class DeploymentsApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Root_ReturnsServiceMetadataAndSecurityHeaders()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/", cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("cloud-deploy-api", payload.GetProperty("service").GetString());
        Assert.Equal("/swagger", payload.GetProperty("documentation").GetString());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task Swagger_ContainsAllPublishedRoutes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json", cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var paths = payload.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(paths.TryGetProperty("/", out _));
        Assert.True(paths.TryGetProperty("/healthz", out _));
        Assert.True(paths.TryGetProperty("/api/v1/deployments", out _));
        Assert.True(paths.TryGetProperty("/api/v1/deployments/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/v1/deployments/{id}/status", out _));
    }

    [Fact]
    public async Task Health_ReturnsHealthyStatus()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/healthz", cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", payload.GetProperty("status").GetString());
        Assert.Equal("cloud-deploy-api", payload.GetProperty("service").GetString());
    }

    [Fact]
    public async Task Health_WhenACheckFails_ReturnsServiceUnavailable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    services.AddHealthChecks().AddCheck(
                        "forced-unhealthy",
                        () => HealthCheckResult.Unhealthy())));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/healthz", cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("unhealthy", payload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Deployment_CanBeCreatedReadUpdatedAndDeleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/deployments",
            new
            {
                application = "payment-api",
                environment = "PRODUCTION",
                version = "v1.4.0",
                notes = "Release from CI"
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var created = await createResponse.Content.ReadFromJsonAsync<DeploymentResponse>(
            JsonOptions,
            cancellationToken);
        Assert.NotNull(created);
        Assert.Equal("payment-api", created.Application);
        Assert.Equal("production", created.Environment);
        Assert.Equal(DeploymentStatus.Queued, created.Status);
        Assert.Equal("Release from CI", created.Notes);

        using var getResponse = await client.GetAsync(
            $"/api/v1/deployments/{created.Id}",
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/deployments/{created.Id}/status",
            new { status = "Succeeded", notes = "Deployment completed" },
            cancellationToken);

        var updated = await updateResponse.Content.ReadFromJsonAsync<DeploymentResponse>(
            JsonOptions,
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(DeploymentStatus.Succeeded, updated.Status);
        Assert.Equal("Deployment completed", updated.Notes);
        Assert.True(updated.UpdatedAt >= created.UpdatedAt);

        using var deleteResponse = await client.DeleteAsync(
            $"/api/v1/deployments/{created.Id}",
            cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var missingResponse = await client.GetAsync(
            $"/api/v1/deployments/{created.Id}",
            cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidBody_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/deployments",
            new
            {
                application = "!",
                environment = "unknown",
                version = "version with spaces"
            },
            cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("One or more validation errors occurred.", payload.GetProperty("title").GetString());
        Assert.True(payload.GetProperty("errors").EnumerateObject().Count() >= 3);
    }

    [Fact]
    public async Task Create_WithUnknownProperty_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/deployments",
            new
            {
                application = "catalog-api",
                environment = "staging",
                version = "1.0.0",
                unexpected = true
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WithNumericEnum_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.PatchAsJsonAsync(
            "/api/v1/deployments/11111111-1111-1111-1111-111111111111/status",
            new { status = 2 },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_CanFilterByEnvironmentAndStatus()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        await CreateDeploymentAsync(client, "catalog-api", "staging", "2.0.0");
        var production = await CreateDeploymentAsync(client, "catalog-api", "production", "2.0.0");

        using var updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/deployments/{production.Id}/status",
            new { status = "Succeeded" },
            cancellationToken);
        updateResponse.EnsureSuccessStatusCode();

        using var response = await client.GetAsync(
            "/api/v1/deployments?environment=production&status=Succeeded",
            cancellationToken);
        var deployments = await response.Content.ReadFromJsonAsync<DeploymentResponse[]>(
            JsonOptions,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(deployments);
        var result = Assert.Single(deployments);
        Assert.Equal(production.Id, result.Id);
    }

    [Fact]
    public async Task UpdateStatus_ForMissingDeployment_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.PatchAsJsonAsync(
            "/api/v1/deployments/11111111-1111-1111-1111-111111111111/status",
            new { status = "Failed" },
            cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Deployment not found", payload.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("GET", "/api/v1/deployments/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/api/v1/deployments/11111111-1111-1111-1111-111111111111")]
    public async Task MissingDeployment_ReturnsNotFound(string method, string path)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Deployment not found", payload.GetProperty("title").GetString());
    }

    private static async Task<DeploymentResponse> CreateDeploymentAsync(
        HttpClient client,
        string application,
        string environment,
        string version)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var response = await client.PostAsJsonAsync(
            "/api/v1/deployments",
            new { application, environment, version },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DeploymentResponse>(
            JsonOptions,
            cancellationToken))!;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
