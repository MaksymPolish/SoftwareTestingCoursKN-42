namespace Lab2.Core;

public class WeatherApiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openweathermap.org/data/2.5";
    public int Timeout { get; set; } = 30;
}
