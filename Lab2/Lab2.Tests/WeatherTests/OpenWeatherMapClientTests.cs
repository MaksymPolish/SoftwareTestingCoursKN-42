namespace Lab2.Tests;

using Lab2.Core;
using Shouldly;
using Xunit;
using System.Net;
using NSubstitute;

public class OpenWeatherMapClientTests
{
    private readonly WeatherApiSettings _settings;

    public OpenWeatherMapClientTests()
    {
        _settings = new WeatherApiSettings
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.openweathermap.org/data/2.5",
            Timeout = 30
        };
    }

    private OpenWeatherMapClient CreateClientWithMockedHttp(string jsonResponse)
    {
        // Create a mock HttpMessageHandler using a custom test handler
        var httpClient = new HttpClient(new TestHttpMessageHandler(jsonResponse))
        {
            BaseAddress = new Uri(_settings.BaseUrl)
        };

        return new OpenWeatherMapClient(_settings, httpClient);
    }

    private OpenWeatherMapClient CreateClientWithErrorResponse(HttpStatusCode statusCode)
    {
        var httpClient = new HttpClient(new TestHttpMessageHandler(null, statusCode))
        {
            BaseAddress = new Uri(_settings.BaseUrl)
        };

        return new OpenWeatherMapClient(_settings, httpClient);
    }

    // Test 1: GetCurrentWeatherAsync - Successfully deserializes real API response
    [Fact]
    public async Task GetCurrentWeatherAsync_WithValidCity_DeserializesApiResponseCorrectly()
    {
        // Arrange
        var city = "Kyiv";
        var jsonResponse = @"{
            ""name"": ""Kyiv"",
            ""main"": {
                ""temp"": 15.5
            },
            ""weather"": [
                {
                    ""main"": ""Cloudy""
                }
            ]
        }";

        var client = CreateClientWithMockedHttp(jsonResponse);

        // Act
        var result = await client.GetCurrentWeatherAsync(city);

        // Assert
        result.ShouldNotBeNull();
        result.City.ShouldBe("Kyiv");
        result.Temperature.ShouldBe(15.5);
        result.Description.ShouldBe("Cloudy");
    }

    // Test 2: GetCurrentWeatherAsync - Throws exception for empty city
    [Fact]
    public async Task GetCurrentWeatherAsync_WithEmptyCity_ThrowsArgumentException()
    {
        // Arrange
        var client = new OpenWeatherMapClient(_settings);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            client.GetCurrentWeatherAsync(""));

        exception.Message.ShouldContain("City cannot be empty");
    }

    // Test 3: GetCurrentWeatherAsync - Handles null response
    [Fact]
    public async Task GetCurrentWeatherAsync_WhenApiReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var city = "Kyiv";
        var jsonResponse = "null";
        var client = CreateClientWithMockedHttp(jsonResponse);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            client.GetCurrentWeatherAsync(city));

        exception.Message.ShouldContain("Failed to parse weather data");
    }

    // Test 4: GetCurrentWeatherAsync - Handles API errors
    [Fact]
    public async Task GetCurrentWeatherAsync_WhenApiError_ThrowsInvalidOperationException()
    {
        // Arrange
        var city = "Kyiv";
        var client = CreateClientWithErrorResponse(HttpStatusCode.Unauthorized);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            client.GetCurrentWeatherAsync(city));

        exception.Message.ShouldContain("Failed to get current weather");
    }

    // Test 5: GetForecastAsync - Successfully deserializes real forecast response
    [Fact]
    public async Task GetForecastAsync_WithValidInput_DeserializesForecastResponseCorrectly()
    {
        // Arrange
        var city = "Kyiv";
        var days = 5;
        var jsonResponse = @"{
            ""city"": {
                ""name"": ""Kyiv""
            },
            ""list"": [
                {
                    ""dt_txt"": ""2026-03-20 12:00:00"",
                    ""main"": {
                        ""temp"": 15.5
                    },
                    ""weather"": [{ ""main"": ""Cloudy"" }]
                },
                {
                    ""dt_txt"": ""2026-03-20 15:00:00"",
                    ""main"": {
                        ""temp"": 16.0
                    },
                    ""weather"": [{ ""main"": ""Cloudy"" }]
                },
                {
                    ""dt_txt"": ""2026-03-21 12:00:00"",
                    ""main"": {
                        ""temp"": 14.0
                    },
                    ""weather"": [{ ""main"": ""Rainy"" }]
                },
                {
                    ""dt_txt"": ""2026-03-22 12:00:00"",
                    ""main"": {
                        ""temp"": 17.0
                    },
                    ""weather"": [{ ""main"": ""Sunny"" }]
                }
            ]
        }";

        var client = CreateClientWithMockedHttp(jsonResponse);

        // Act
        var result = await client.GetForecastAsync(city, days);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(3); // Should have 3 days (one forecast per day)
        result.First().City.ShouldBe("Kyiv");
        result.First().Temperature.ShouldBe(15.5);
        result.First().Description.ShouldBe("Cloudy");
    }

    // Test 6: GetForecastAsync - Throws exception for invalid days
    [Fact]
    public async Task GetForecastAsync_WithZeroDays_ThrowsArgumentException()
    {
        // Arrange
        var client = new OpenWeatherMapClient(_settings);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            client.GetForecastAsync("Kyiv", 0));

        exception.Message.ShouldContain("Days must be greater than 0");
    }

    // Test 7: GetForecastAsync - Throws exception for empty city
    [Fact]
    public async Task GetForecastAsync_WithEmptyCity_ThrowsArgumentException()
    {
        // Arrange
        var client = new OpenWeatherMapClient(_settings);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            client.GetForecastAsync("", 5));

        exception.Message.ShouldContain("City cannot be empty");
    }

    // Test 8: GetForecastAsync - Handles null response
    [Fact]
    public async Task GetForecastAsync_WhenApiReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var city = "Kyiv";
        var jsonResponse = "null";
        var client = CreateClientWithMockedHttp(jsonResponse);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            client.GetForecastAsync(city, 5));

        exception.Message.ShouldContain("Failed to parse forecast data");
    }

    // Test 9: GetForecastAsync - Handles API errors
    [Fact]
    public async Task GetForecastAsync_WhenApiError_ThrowsInvalidOperationException()
    {
        // Arrange
        var city = "Kyiv";
        var client = CreateClientWithErrorResponse(HttpStatusCode.BadRequest);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            client.GetForecastAsync(city, 5));

        exception.Message.ShouldContain("Failed to get forecast");
    }

    // Test 10: Constructor - Throws exception for missing API key
    [Fact]
    public void Constructor_WithMissingApiKey_ThrowsArgumentException()
    {
        // Arrange
        var invalidSettings = new WeatherApiSettings
        {
            ApiKey = "",
            BaseUrl = "https://api.openweathermap.org/data/2.5",
            Timeout = 30
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            new OpenWeatherMapClient(invalidSettings));

        exception.Message.ShouldContain("API Key cannot be empty");
    }

    // Test 11: Constructor - Throws exception for null settings
    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OpenWeatherMapClient(null!));
    }

    // Test 12: GetCurrentWeatherAsync - Handles missing weather in response
    [Fact]
    public async Task GetCurrentWeatherAsync_WithEmptyWeatherList_DeserializesSuccessfully()
    {
        // Arrange
        var city = "Kyiv";
        var jsonResponse = @"{
            ""name"": ""Kyiv"",
            ""main"": {
                ""temp"": 15.5
            },
            ""weather"": []
        }";

        var client = CreateClientWithMockedHttp(jsonResponse);

        // Act
        var result = await client.GetCurrentWeatherAsync(city);

        // Assert
        result.ShouldNotBeNull();
        result.Description.ShouldBe("Unknown"); // Should default to "Unknown"
    }

    // Test 13: GetForecastAsync - Selects forecast closest to noon
    [Fact]
    public async Task GetForecastAsync_SelectsForecastClosestToNoon()
    {
        // Arrange
        var city = "Kyiv";
        var days = 1;
        var jsonResponse = @"{
            ""city"": {
                ""name"": ""Kyiv""
            },
            ""list"": [
                {
                    ""dt_txt"": ""2026-03-20 09:00:00"",
                    ""main"": { ""temp"": 10.0 },
                    ""weather"": [{ ""main"": ""Cloudy"" }]
                },
                {
                    ""dt_txt"": ""2026-03-20 12:00:00"",
                    ""main"": { ""temp"": 15.5 },
                    ""weather"": [{ ""main"": ""Sunny"" }]
                },
                {
                    ""dt_txt"": ""2026-03-20 15:00:00"",
                    ""main"": { ""temp"": 16.0 },
                    ""weather"": [{ ""main"": ""Cloudy"" }]
                }
            ]
        }";

        var client = CreateClientWithMockedHttp(jsonResponse);

        // Act
        var result = await client.GetForecastAsync(city, days);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(1);
        result.First().Temperature.ShouldBe(15.5); // Should select 12:00 (noon)
        result.First().Description.ShouldBe("Sunny");
    }

    /// <summary>
    /// Test HTTP message handler that returns mocked responses for testing
    /// </summary>
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly string? _jsonResponse;
        private readonly HttpStatusCode _statusCode;

        public TestHttpMessageHandler(string? jsonResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _jsonResponse = jsonResponse;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = _statusCode == HttpStatusCode.OK && _jsonResponse != null
                    ? new StringContent(_jsonResponse, System.Text.Encoding.UTF8, "application/json")
                    : new StringContent("")
            };

            return Task.FromResult(response);
        }
    }
}
