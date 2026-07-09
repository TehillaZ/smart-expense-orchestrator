using System.Net;
using System.Net.Http.Json;
using ApiGateway.Auth;
using ApprovalFlow.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ApiGateway.Tests;

/// <summary>
/// API-layer smoke tests for public gateway routes.
/// </summary>
[Trait(TestLayers.TraitName, TestLayers.Api)]
public class GatewayApiRouteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public GatewayApiRouteTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting($"{JwtOptions.SectionName}:SigningKey", "test-signing-key-minimum-32-characters");
        });
    }

    [Fact]
    public async Task Health_IsPublicAndReturnsOk()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidRole_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { role = "NotARole" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
