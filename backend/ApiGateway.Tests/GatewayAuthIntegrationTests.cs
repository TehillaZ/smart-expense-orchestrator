using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApiGateway.Auth;
using ApprovalFlow.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ApiGateway.Tests;

[Trait(TestLayers.TraitName, TestLayers.Api)]
public class GatewayAuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GatewayAuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting($"{JwtOptions.SectionName}:SigningKey", "test-signing-key-minimum-32-characters");
        });
    }

    [Fact]
    public async Task Login_WithValidRole_ReturnsJwtToken()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { role = AppRoles.Submitter, username = "tehila" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponsePayload>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));
        Assert.Equal(AppRoles.Submitter, payload.Role);
        Assert.Equal("tehila", payload.Username);
    }

    [Fact]
    public async Task Login_WithoutUsername_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { role = AppRoles.Submitter });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/submissions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Submitter_CannotAccessApprovalQueue()
    {
        using var client = CreateAuthenticatedClient(AppRoles.Submitter);

        var response = await client.GetAsync("/approvals/queue");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Submitter_CannotApproveEscalatedItem()
    {
        using var client = CreateAuthenticatedClient(AppRoles.Submitter);

        var response = await client.PostAsJsonAsync(
            "/approvals/tracking-123/decision",
            new { decision = "Approved" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approver_CanAccessApprovalQueue_PastAuthorizationLayer()
    {
        using var client = CreateAuthenticatedClient(AppRoles.Approver);

        var response = await client.GetAsync("/approvals/queue");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanAccessPolicyEndpoint_PastAuthorizationLayer()
    {
        using var client = CreateAuthenticatedClient(AppRoles.Admin);

        var response = await client.GetAsync("/policy");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Submitter_CannotUpdatePolicy()
    {
        using var client = CreateAuthenticatedClient(AppRoles.Submitter);

        var response = await client.PutAsJsonAsync("/policy", new { autonomyCeiling = 1000 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Submitter_CanAccessSubmissions_PastAuthorizationLayer()
    {
        using var client = CreateAuthenticatedClient(AppRoles.Submitter);

        var response = await client.GetAsync("/submissions");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task JwtTokenService_CreatesTokenWithExpectedRoleClaim()
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();

        var tokenResult = tokenService.CreateToken(AppRoles.Approver, "approver-demo");

        Assert.Equal(AppRoles.Approver, tokenResult.Role);
        Assert.True(tokenResult.ExpiresAt > DateTimeOffset.UtcNow);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);

        var response = await client.GetAsync("/approvals/queue");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();
        var token = tokenService.CreateToken(role, role.ToLowerInvariant()).Token;

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed record LoginResponsePayload(string Token, string Role, string Username, DateTimeOffset ExpiresAt);
}
