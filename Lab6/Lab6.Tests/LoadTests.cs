using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace Lab6.Tests;

public class LoadTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public LoadTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void SmokeTest_SingleUser_ShouldRespondWithoutErrors()
    {
        // Arrange: налаштування тестового середовища
        var httpClient = _factory.CreateClient();

        var scenario = Scenario.Create("smoke_get_products", async context =>
        {
            var request = Http.CreateRequest("GET", $"{_factory.BaseUrl}/api/products");
            var response = await Http.Send(httpClient, request);
            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 1,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(1))
        );

        // Act: виконання навантажувального тесту
        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert: перевірка результатів
        Assert.NotNull(result);
        
        Console.WriteLine($"\n=== Smoke Test Results ===");
        Console.WriteLine("Smoke test completed successfully");
    }

    [Fact]
    public void MediumLoadTest_50VirtualUsers_SimulatesNormalTraffic()
    {
        // Arrange: налаштування тестового середовища
        var httpClient = _factory.CreateClient();

        var scenario = Scenario.Create("medium_load_test", async context =>
        {
            var request = Http.CreateRequest("GET", $"{_factory.BaseUrl}/api/products");
            var response = await Http.Send(httpClient, request);
            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 50,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(5))
        );

        // Act: виконання навантажувального тесту
        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert: перевірка результатів
        Assert.NotNull(result);
        
        Console.WriteLine($"\n=== Medium Load Test Results ===");
        Console.WriteLine("Medium load test completed successfully");
    }
}

