namespace Lab2.Tests;

using Lab2.Core;
using NSubstitute;
using Xunit;
using Shouldly;

public class WeatherForecastServiceTests
{
    private readonly IWeatherApiClient _apiClient;
    private readonly ICacheService _cacheService;
    private readonly WeatherForecastService _sut;

    public WeatherForecastServiceTests()
    {
        _apiClient = Substitute.For<IWeatherApiClient>();
        _cacheService = Substitute.For<ICacheService>();
        _sut = new WeatherForecastService(_apiClient, _cacheService);
    }

    // Test 1: Cache Hit - API Never Called

    [Fact]
    public async Task GetForecastAsync_WhenDataInCache_ReturnsFromCacheAndDoesNotCallApi()
    {
        // Arrange
        var city = "Kyiv";
        var days = 5;
        var cacheKey = $"weather:{city}:{days}";
        var cachedForecast = new List<WeatherData>
        {
            new WeatherData(city, 15.5, "Cloudy", DateTime.UtcNow),
            new WeatherData(city, 16.0, "Rainy", DateTime.UtcNow.AddDays(1))
        };

        _cacheService.Exists(cacheKey).Returns(true);
        _cacheService.Get<IEnumerable<WeatherData>>(cacheKey).Returns(cachedForecast);

        // Act
        var result = await _sut.GetForecastAsync(city, days);

        // Assert
        result.ShouldBe(cachedForecast);
        _apiClient.DidNotReceive().GetForecastAsync(city, days);
    }

    // Test 2: Cache Miss - API Called and Result Cached

    [Fact]
    public async Task GetForecastAsync_WhenDataNotInCache_CallsApiAndStoresInCache()
    {
        // Arrange
        var city = "London";
        var days = 7;
        var cacheKey = $"weather:{city}:{days}";
        var apiForecast = new List<WeatherData>
        {
            new WeatherData(city, 10.0, "Sunny", DateTime.UtcNow),
            new WeatherData(city, 12.0, "Cloudy", DateTime.UtcNow.AddDays(1))
        };

        _cacheService.Exists(cacheKey).Returns(false);
        _apiClient.GetForecastAsync(city, days).Returns(Task.FromResult((IEnumerable<WeatherData>)apiForecast));

        // Act
        var result = await _sut.GetForecastAsync(city, days);

        // Assert
        result.ShouldBe(apiForecast);
        _apiClient.Received(1).GetForecastAsync(city, days);
        _cacheService.Received(1).Set(
            cacheKey,
            Arg.Is<IEnumerable<WeatherData>>(f => f == apiForecast),
            TimeSpan.FromMinutes(30));
    }

    // Test 3: API Exception with Cached Data available

    [Fact]
    public async Task GetForecastAsync_WhenApiThrowsExceptionAndCacheExists_ReturnsCachedData()
    {
        // Arrange
        var city = "Paris";
        var days = 3;
        var cacheKey = $"weather:{city}:{days}";
        var cachedForecast = new List<WeatherData>
        {
            new WeatherData(city, 14.0, "Cloudy", DateTime.UtcNow)
        };

        _cacheService.Exists(cacheKey).Returns(false); // First call returns false
        _apiClient.GetForecastAsync(city, days)
            .Returns(Task.FromException<IEnumerable<WeatherData>>(new HttpRequestException("API unavailable"))); // On error, we try cache
        _cacheService.Get<IEnumerable<WeatherData>>(cacheKey).Returns(cachedForecast);

        // Act
        var result = await _sut.GetForecastAsync(city, days);

        // Assert
        result.ShouldBe(cachedForecast);
        _apiClient.Received(1).GetForecastAsync(city, days);
    }

    // Test 4: API Exception without Cached Data

    [Fact]
    public async Task GetForecastAsync_WhenApiThrowsExceptionAndNoCacheExists_ThrowsException()
    {
        // Arrange
        var city = "Berlin";
        var days = 5;
        var cacheKey = $"weather:{city}:{days}";

        _cacheService.Exists(cacheKey).Returns(false);
        _apiClient.GetForecastAsync(city, days)
            .Returns(Task.FromException<IEnumerable<WeatherData>>(new HttpRequestException("Network error")));
        _cacheService.Get<IEnumerable<WeatherData>>(cacheKey).Returns((IEnumerable<WeatherData>?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.GetForecastAsync(city, days));

        exception.Message.ShouldContain("Failed to get weather forecast");
    }

    // Test 5: Invalid Input - Empty City

    [Fact]
    public async Task GetForecastAsync_WithEmptyCity_ThrowsArgumentException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _sut.GetForecastAsync("", 5));

        _cacheService.DidNotReceive().Exists(Arg.Any<string>());
        _apiClient.DidNotReceive().GetForecastAsync(Arg.Any<string>(), Arg.Any<int>());
    }

    // Test 6: Invalid Input - Non-Positive Days

    [Fact]
    public async Task GetForecastAsync_WithZeroDays_ThrowsArgumentException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _sut.GetForecastAsync("Rome", 0));

        _apiClient.DidNotReceive().GetForecastAsync(Arg.Any<string>(), Arg.Any<int>());
    }

    // Test 7: Constructor Validation

    [Fact]
    public void Constructor_WithNullApiClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new WeatherForecastService(null!, _cacheService));
    }

    // Test 8: Cache Key Format Verification

    [Fact]
    public async Task GetForecastAsync_ChecksCacheWithCorrectKeyFormat()
    {
        // Arrange
        var city = "Tokyo";
        var days = 10;
        var expectedCacheKey = "weather:Tokyo:10";

        _cacheService.Exists(expectedCacheKey).Returns(false);
        var forecast = new List<WeatherData>
        {
            new WeatherData(city, 20.0, "Clear", DateTime.UtcNow)
        };
        _apiClient.GetForecastAsync(city, days).Returns(Task.FromResult((IEnumerable<WeatherData>)forecast));

        // Act
        await _sut.GetForecastAsync(city, days);

        // Assert
        _cacheService.Received(1).Exists(expectedCacheKey);
        _cacheService.Received(1).Set(
            expectedCacheKey,
            Arg.Any<IEnumerable<WeatherData>>(),
            Arg.Any<TimeSpan>());
    }

    // Test 9: Multiple Sequential Calls - Second Should Use Cache

    [Fact]
    public async Task GetForecastAsync_SecondCallForSameParameters_UsesCacheWithoutApiCall()
    {
        // Arrange
        var city = "Amsterdam";
        var days = 5;
        var cacheKey = $"weather:{city}:{days}";
        var forecast = new List<WeatherData>
        {
            new WeatherData(city, 12.0, "Rainy", DateTime.UtcNow)
        };

        _cacheService.Exists(cacheKey).Returns(false, true); // First call: miss, Second call: hit
        _cacheService.Get<IEnumerable<WeatherData>>(cacheKey).Returns(forecast);
        _apiClient.GetForecastAsync(city, days).Returns(Task.FromResult((IEnumerable<WeatherData>)forecast));

        // Act - First call
        var result1 = await _sut.GetForecastAsync(city, days);
        
        // Act - Second call
        var result2 = await _sut.GetForecastAsync(city, days);

        // Assert
        result1.ShouldBe(forecast);
        result2.ShouldBe(forecast);
        _apiClient.Received(1).GetForecastAsync(city, days); // API called only once
    }

    // Test 10: Cache Expiration Time is 30 Minutes

    [Fact]
    public async Task GetForecastAsync_StoresCacheWithCorrectExpiration()
    {
        // Arrange
        var city = "Madrid";
        var days = 7;
        var forecast = new List<WeatherData>
        {
            new WeatherData(city, 18.0, "Sunny", DateTime.UtcNow)
        };

        _cacheService.Exists(Arg.Any<string>()).Returns(false);
        _apiClient.GetForecastAsync(city, days).Returns(Task.FromResult((IEnumerable<WeatherData>)forecast));

        // Act
        await _sut.GetForecastAsync(city, days);

        // Assert
        _cacheService.Received(1).Set(
            Arg.Any<string>(),
            Arg.Any<IEnumerable<WeatherData>>(),
            TimeSpan.FromMinutes(30));
    }
}
