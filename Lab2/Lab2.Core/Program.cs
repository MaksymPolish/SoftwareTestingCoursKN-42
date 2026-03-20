namespace Lab2.Core;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Load settings from appsettings.json (not hardcoded - SECURITY BEST PRACTICE!)
            var settings = ConfigurationLoader.LoadWeatherApiSettings();

            // Initialize services
            var cacheService = new InMemoryCacheService();
            var apiClient = new OpenWeatherMapClient(settings);
            var weatherService = new WeatherForecastService(apiClient, cacheService);

            // Example 1: Get forecast for Kyiv
            Console.WriteLine("=== Weather Forecast for Kyiv ===");
            var kyivForecast = await weatherService.GetForecastAsync("Kyiv", 5);
            
            foreach (var weather in kyivForecast)
            {
                Console.WriteLine($"Date: {weather.Date:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"City: {weather.City}");
                Console.WriteLine($"Temperature: {weather.Temperature}°C");
                Console.WriteLine($"Description: {weather.Description}");
                Console.WriteLine();
            }

            // Example 2: Get forecast from cache (won't call API)
            Console.WriteLine("=== Second Request (from cache) ===");
            var kyivForecastCached = await weatherService.GetForecastAsync("Kyiv", 5);
            Console.WriteLine($"Retrieved {kyivForecastCached.Count()} weather records from cache");

            // Example 3: Get forecast for different city
            Console.WriteLine("\n=== Weather Forecast for London ===");
            var londonForecast = await weatherService.GetForecastAsync("London", 3);
            
            foreach (var weather in londonForecast)
            {
                Console.WriteLine($"Date: {weather.Date:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"City: {weather.City}");
                Console.WriteLine($"Temperature: {weather.Temperature}°C");
                Console.WriteLine($"Description: {weather.Description}");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"Details: {ex.InnerException.Message}");
        }
    }
}
