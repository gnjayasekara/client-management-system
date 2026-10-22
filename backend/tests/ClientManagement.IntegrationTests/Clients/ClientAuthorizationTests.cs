
using System.Net;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ClientManagement.IntegrationTests.Clients;

public class ClientAuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ClientAuthorizationTests(ApiFactory factory)
    {
        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
    }

    [Fact]
    public async Task GetClients_WithoutToken_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/clients");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetClients_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                "invalid.jwt.token");

        // Act
        var response = await _client.GetAsync("/api/clients");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}
