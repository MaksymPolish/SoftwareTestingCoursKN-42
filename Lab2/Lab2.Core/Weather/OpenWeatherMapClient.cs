namespace Lab2.Core;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

public class OpenWeatherMapClient : IWeatherApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WeatherApiSettings _settings;

    public OpenWeatherMapClient(WeatherApiSettings settings) : this(settings, null)
    {
    }

    public OpenWeatherMapClient(WeatherApiSettings settings, HttpClient? httpClient)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        
        if (string.IsNullOrEmpty(_settings.ApiKey))
            throw new ArgumentException("API Key cannot be empty", nameof(settings));

        _httpClient = httpClient ?? new HttpClient
        {
            BaseAddress = new Uri(_settings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_settings.Timeout)
        };

        // Set base address and timeout if using injected HttpClient
        if (httpClient != null)
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.Timeout);
        }
    }

    public async Task<WeatherData> GetCurrentWeatherAsync(string city)
    {
        if (string.IsNullOrEmpty(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        try
        {
            var url = $"/weather?q={Uri.EscapeDataString(city)}&appid={_settings.ApiKey}&units=metric";
            var data = await _httpClient.GetFromJsonAsync<OpenWeatherMapResponse>(url);
            
            if (data == null)
                throw new InvalidOperationException("Empty response from API");

            return new WeatherData(
                City: data.Name,
                Temperature: data.Main.Temp,
                Description: data.Weather.FirstOrDefault()?.Main ?? "Unknown",
                Date: DateTime.UtcNow
            );
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to get current weather for {city}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse weather data for {city}", ex);
        }
    }

    public async Task<IEnumerable<WeatherData>> GetForecastAsync(string city, int days)
    {
        if (string.IsNullOrEmpty(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        if (days <= 0)
            throw new ArgumentException("Days must be greater than 0", nameof(days));

        try
        {
            // OpenWeatherMap forecast endpoint returns data in 3-hour intervals
            var url = $"/forecast?q={Uri.EscapeDataString(city)}&appid={_settings.ApiKey}&units=metric";
            var data = await _httpClient.GetFromJsonAsync<OpenWeatherMapForecastResponse>(url);

            if (data == null)
                throw new InvalidOperationException("Empty response from API");

            // Group by day and take one forecast per day (closest to noon)
            var forecast = data.List
                .GroupBy(x => x.ParsedDate.Date)
                .Take(days)
                .Select(g =>
                {
                    var item = g.OrderBy(x => Math.Abs((x.ParsedDate.Hour - 12))).First();
                    return new WeatherData(
                        City: data.City.Name,
                        Temperature: item.Main.Temp,
                        Description: item.Weather.FirstOrDefault()?.Main ?? "Unknown",
                        Date: item.ParsedDate
                    );
                })
                .ToList();

            return forecast;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to get forecast for {city}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse forecast data for {city}", ex);
        }
    }

    // Internal classes for JSON deserialization
    private class OpenWeatherMapResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("main")]
        public MainWeather Main { get; set; } = new();

        [JsonPropertyName("weather")]
        public List<WeatherInfo> Weather { get; set; } = new();
    }

    private class MainWeather
    {
        [JsonPropertyName("temp")]
        public double Temp { get; set; }
    }

    private class WeatherInfo
    {
        [JsonPropertyName("main")]
        public string Main { get; set; } = string.Empty;
    }

    private class OpenWeatherMapForecastResponse
    {
        [JsonPropertyName("city")]
        public CityInfo City { get; set; } = new();

        [JsonPropertyName("list")]
        public List<ForecastItem> List { get; set; } = new();
    }

    private class CityInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private class ForecastItem
    {
        [JsonPropertyName("main")]
        public MainWeather Main { get; set; } = new();

        [JsonPropertyName("weather")]
        public List<WeatherInfo> Weather { get; set; } = new();

        [JsonPropertyName("dt_txt")]
        public string DtTxt { get; set; } = string.Empty;

        public DateTime ParsedDate => DateTime.ParseExact(DtTxt, "yyyy-MM-dd HH:mm:ss", null);
    }
}
