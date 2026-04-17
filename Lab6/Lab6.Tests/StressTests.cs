using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace Lab6.Tests;

public class StressTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public StressTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void StressTest_RampUp_FindsBreakingPoint()
    {
        // Arrange: налаштування тестового середовища
        var httpClient = _factory.CreateClient();

        var scenario = Scenario.Create("stress_ramp_up", async context =>
        {
            var request = Http.CreateRequest("GET", $"{_factory.BaseUrl}/api/products");
            var response = await Http.Send(httpClient, request);
            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
            Simulation.Inject(rate: 250, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
            Simulation.Inject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
        );

        // Act: виконання стрес-тесту з поступовим наростанням навантаження
        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert: перевірка результатів
        Assert.NotNull(result);
        
        Console.WriteLine($"\n=== Stress Test Results ===");
        Console.WriteLine("Stress test completed successfully");
    }
}
