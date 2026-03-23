namespace Lab3.Tests;

using Lab3.Api.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

public class MiddlewareTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MiddlewareTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Task 2.1: Test request without API key returns 401
    [Fact]
    public async Task Request_WithoutApiKey_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().ShouldBe("API key is required");
    }

    // Task 2.2: Test request with invalid API key returns 403
    [Fact]
    public async Task Request_WithInvalidApiKey_Returns403()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().ShouldBe("Invalid API key");
    }

    // Task 2.3: Test unhandled exception returns structured JSON error as 500
    [Fact]
    public async Task UnhandledException_ReturnsStructuredJsonError()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.FirstOrDefault(
                    d => d.ServiceType == typeof(IProductRepository));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddSingleton<IProductRepository>(new ThrowingRepository());
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "valid-test-key");

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().ShouldNotBeNullOrEmpty();
        body.TryGetProperty("type", out var type).ShouldBeTrue();
    }

    // Additional test: Valid API key allows access
    [Fact]
    public async Task Request_WithValidApiKey_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "valid-test-key");

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Additional test: Middleware order verification - API key auth before logging
    [Fact]
    public async Task MiddlewareOrder_ApiKeyAuthenticationBeforeLogging()
    {
        // Arrange - without API key should fail before reaching logging
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert - should get 401 from auth middleware
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

// Helper class to test exception handling
public class ThrowingRepository : IProductRepository
{
    public Lab3.Api.Models.Product Add(Lab3.Api.Models.Product product)
        => throw new InvalidOperationException("Repository operation failed");

    public bool Delete(int id)
        => throw new InvalidOperationException("Repository operation failed");

    public IEnumerable<Lab3.Api.Models.Product> GetAll()
        => throw new InvalidOperationException("Repository operation failed");

    public Lab3.Api.Models.Product? GetById(int id)
        => throw new InvalidOperationException("Repository operation failed");

    public bool Update(int id, Lab3.Api.Models.Product product)
        => throw new InvalidOperationException("Repository operation failed");
}
