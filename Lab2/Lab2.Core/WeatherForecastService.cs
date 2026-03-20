namespace Lab2.Core;

public class WeatherForecastService
{
    private readonly IWeatherApiClient _apiClient;
    private readonly ICacheService _cacheService;
    private const string CacheKeyFormat = "weather:{0}:{1}";
    private const int CacheExpirationMinutes = 30;

    public WeatherForecastService(IWeatherApiClient apiClient, ICacheService cacheService)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<IEnumerable<WeatherData>> GetForecastAsync(string city, int days)
    {
        if (string.IsNullOrEmpty(city))
            throw new ArgumentException("City cannot be empty", nameof(city));
        
        if (days <= 0)
            throw new ArgumentException("Days must be greater than 0", nameof(days));

        var cacheKey = string.Format(CacheKeyFormat, city, days);

        // Check cache first
        if (_cacheService.Exists(cacheKey))
        {
            var cachedData = _cacheService.Get<IEnumerable<WeatherData>>(cacheKey);
            if (cachedData != null)
                return cachedData;
        }

        // Call API if not in cache
        try
        {
            var forecast = await _apiClient.GetForecastAsync(city, days);
            
            // Store in cache
            _cacheService.Set(cacheKey, forecast, TimeSpan.FromMinutes(CacheExpirationMinutes));
            
            return forecast;
        }
        catch (Exception ex)
        {
            // Try to return cached data on error
            var cachedData = _cacheService.Get<IEnumerable<WeatherData>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            // If no cache, throw exception
            throw new InvalidOperationException($"Failed to get weather forecast for {city}", ex);
        }
    }
}
