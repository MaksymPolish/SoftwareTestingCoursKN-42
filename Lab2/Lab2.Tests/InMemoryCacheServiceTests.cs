namespace Lab2.Tests;

using Lab2.Core;
using Xunit;
using Shouldly;

public class InMemoryCacheServiceTests
{
    private readonly InMemoryCacheService _cache;

    public InMemoryCacheServiceTests()
    {
        _cache = new InMemoryCacheService();
    }

    [Fact]
    public void Set_WithValidData_StoresData()
    {
        // Arrange
        var key = "test_key";
        var value = "test_value";
        var expiration = TimeSpan.FromMinutes(30);

        // Act
        _cache.Set(key, value, expiration);

        // Assert
        _cache.Exists(key).ShouldBeTrue();
        var retrieved = _cache.Get<string>(key);
        retrieved.ShouldBe(value);
    }

    [Fact]
    public void Get_WithValidKey_ReturnsStoredValue()
    {
        // Arrange
        var key = "weather:Kyiv:5";
        var weather = new WeatherData("Kyiv", 15.5, "Cloudy", DateTime.UtcNow);
        _cache.Set(key, weather, TimeSpan.FromMinutes(30));

        // Act
        var result = _cache.Get<WeatherData>(key);

        // Assert
        result.ShouldBe(weather);
        result.City.ShouldBe("Kyiv");
        result.Temperature.ShouldBe(15.5);
    }

    [Fact]
    public void Get_WithInvalidKey_ReturnsDefault()
    {
        // Act
        var result = _cache.Get<string>("nonexistent_key");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Exists_WithValidKey_ReturnsTrue()
    {
        // Arrange
        var key = "test_key";
        _cache.Set(key, "value", TimeSpan.FromMinutes(30));

        // Act
        var exists = _cache.Exists(key);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public void Exists_WithInvalidKey_ReturnsFalse()
    {
        // Act
        var exists = _cache.Exists("nonexistent_key");

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public void Get_AfterExpiration_ReturnsNull()
    {
        // Arrange
        var key = "expiring_key";
        var value = "temporary_data";
        _cache.Set(key, value, TimeSpan.FromMilliseconds(100)); // Very short expiration

        // Act
        Thread.Sleep(150); // Wait for expiration
        var result = _cache.Get<string>(key);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Exists_AfterExpiration_ReturnsFalse()
    {
        // Arrange
        var key = "expiring_key";
        _cache.Set(key, "value", TimeSpan.FromMilliseconds(100));

        // Act
        Thread.Sleep(150);
        var exists = _cache.Exists(key);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public void Set_WithNullKey_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _cache.Set(null!, "value", TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void Set_WithEmptyKey_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _cache.Set("", "value", TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void Set_WithNullValue_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _cache.Set<string>("key", null!, TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void Set_WithZeroExpiration_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _cache.Set("key", "value", TimeSpan.Zero));
    }

    [Fact]
    public void Set_WithNegativeExpiration_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _cache.Set("key", "value", TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void Clear_RemovesAllData()
    {
        // Arrange
        _cache.Set("key1", "value1", TimeSpan.FromMinutes(30));
        _cache.Set("key2", "value2", TimeSpan.FromMinutes(30));
        _cache.Set("key3", "value3", TimeSpan.FromMinutes(30));

        // Act
        _cache.Clear();

        // Assert
        _cache.Exists("key1").ShouldBeFalse();
        _cache.Exists("key2").ShouldBeFalse();
        _cache.Exists("key3").ShouldBeFalse();
    }

    [Fact]
    public void Set_WithComplexType_StoresAndRetrieves()
    {
        // Arrange
        var key = "weather_list";
        var forecasts = new List<WeatherData>
        {
            new WeatherData("Kyiv", 15.0, "Cloudy", DateTime.UtcNow),
            new WeatherData("Kyiv", 16.0, "Rainy", DateTime.UtcNow.AddDays(1))
        };

        // Act
        _cache.Set(key, forecasts, TimeSpan.FromMinutes(30));
        var result = _cache.Get<List<WeatherData>>(key);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].Temperature.ShouldBe(15.0);
        result[1].Description.ShouldBe("Rainy");
    }

    [Fact]
    public async Task MultipleOperations_WithThreadSafety_AreConsistent()
    {
        // Arrange
        var key = "concurrent_key";
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                _cache.Set($"{key}_{index}", $"value_{index}", TimeSpan.FromMinutes(30));
            }));
        }

        await Task.WhenAll(tasks.ToArray());

        // Assert
        for (int i = 0; i < 10; i++)
        {
            _cache.Exists($"{key}_{i}").ShouldBeTrue();
        }
    }
}
