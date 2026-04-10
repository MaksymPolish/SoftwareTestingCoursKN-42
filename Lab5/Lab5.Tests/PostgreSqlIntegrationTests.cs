using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.PostgreSql;
using Shouldly;
using Xunit;

namespace Lab5.Tests;

public class ApiFixture : IAsyncLifetime
{
    private INetwork _network = null!;
    private PostgreSqlContainer _dbContainer = null!;
    private IContainer _apiContainer = null!;

    public HttpClient HttpClient { get; private set; } = null!;
    public string ConnectionString => _dbContainer.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString())
            .Build();

        await _network.CreateAsync();

        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("test_db")
            .WithUsername("postgres")
            .WithPassword("postgres_password")
            .WithNetwork(_network)
            .WithNetworkAliases("db")
            .Build();

        await _dbContainer.StartAsync();

        _apiContainer = new ContainerBuilder()
            .WithImage("lab5-api:latest")
            .WithNetwork(_network)
            .WithPortBinding(5000, true)
            .WithEnvironment("ASPNETCORE_URLS", "http://+:5000")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("ConnectionStrings__DefaultConnection",
                "Host=db;Port=5432;User Id=postgres;Password=postgres_password;Database=test_db")
            .Build();

        await _apiContainer.StartAsync();

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        HttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri($"http://{_apiContainer.Hostname}:{_apiContainer.GetMappedPublicPort(5000)}")
        };

        // Wait for API to start and health check to pass
        var maxRetries = 60;
        var attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                var response = await HttpClient.GetAsync("/health");
                if (response.IsSuccessStatusCode)
                    break;
            }
            catch { }
            
            await Task.Delay(1000);
            attempt++;
        }
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient?.Dispose();
        if (_apiContainer != null) await _apiContainer.DisposeAsync();
        if (_dbContainer != null) await _dbContainer.DisposeAsync();
        if (_network != null) await _network.DeleteAsync();
    }
}

[Collection("PostgreSQL Integration")]
public class PostgreSqlIntegrationTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;

    public PostgreSqlIntegrationTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.HttpClient;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Health_Endpoint_ReturnsOkAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        // (Nothing to arrange - just test the health endpoint)

        // Act
        var response = await _client.GetAsync("/health", ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAllStudents_ReturnsOkAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        // (Database has initial state from ApiFixture)

        // Act
        var response = await _client.GetAsync("/api/students", ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateStudent_WithValidData_ReturnsCreatedAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var request = new
        {
            fullName = "PostgreSQL Test Student",
            email = "postgres@test.com",
            enrollmentDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/students", request, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var locationHeader = response.Headers.Location;
        locationHeader.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateStudent_WithInvalidEmail_ReturnsBadRequestAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var request = new
        {
            fullName = "Invalid Email Test",
            email = "not-an-email",
            enrollmentDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/students", request, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateStudent_WithEmptyFullName_ReturnsBadRequestAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var request = new
        {
            fullName = "",
            email = "valid@test.com",
            enrollmentDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/students", request, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetStudentById_WithValidId_ReturnsOkAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var createRequest = new
        {
            fullName = "GetById Test",
            email = "getbyid@test.com",
            enrollmentDate = DateTime.UtcNow.AddDays(-1)
        };

        var createResponse = await _client.PostAsJsonAsync("/api/students", createRequest, ct);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var location = createResponse.Headers.Location;
        location.ShouldNotBeNull();

        // Act
        var getResponse = await _client.GetAsync(location, ct);

        // Assert
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateStudent_WithValidData_ReturnsNoContentAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var createRequest = new
        {
            fullName = "Update Test",
            email = "update@test.com",
            enrollmentDate = DateTime.UtcNow.AddDays(-1)
        };

        var createResponse = await _client.PostAsJsonAsync("/api/students", createRequest, ct);
        var location = createResponse.Headers.Location;
        location.ShouldNotBeNull();

        // Extract ID from location URL (which may be relative)
        var locationPath = location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString;
        var idString = locationPath.Split('/').Last();
        int.TryParse(idString, out int studentId);
        studentId.ShouldBeGreaterThan(0);

        var updateRequest = new
        {
            id = studentId,
            fullName = "Updated Name",
            email = "updated@test.com"
        };

        // Act
        var updateResponse = await _client.PutAsJsonAsync(location, updateRequest, ct);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteStudent_WithValidId_ReturnsNoContentAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var createRequest = new
        {
            fullName = "Delete Test",
            email = "delete@test.com",
            enrollmentDate = DateTime.UtcNow.AddDays(-1)
        };

        var createResponse = await _client.PostAsJsonAsync("/api/students", createRequest, ct);
        var location = createResponse.Headers.Location;
        location.ShouldNotBeNull();

        // Act
        var deleteResponse = await _client.DeleteAsync(location, ct);

        // Assert
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(location, ct);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}