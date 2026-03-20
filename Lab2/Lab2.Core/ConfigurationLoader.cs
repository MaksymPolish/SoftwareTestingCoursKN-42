namespace Lab2.Core;

using Microsoft.Extensions.Configuration;
using System;

/// Helper класс для завантаження конфігурації з appsettings.json
public static class ConfigurationLoader
{
    /// Завантажує конфігурацію метеорологічного API з JSON файлу
    public static WeatherApiSettings LoadWeatherApiSettings(string? configPath = null)
    {
        // Визначити шлях до конфігураційного файлу
        var configFile = string.IsNullOrEmpty(configPath)
            ? Path.Combine(AppContext.BaseDirectory, "appsettings.json")
            : configPath;

        // Перевірити наявність файлу
        if (!File.Exists(configFile))
        {
            throw new FileNotFoundException(
                $"Файл конфігурації не знайдено: {configFile}\n" +
                $"Будь ласка, скопіюйте appsettings.example.json в appsettings.json",
                configFile);
        }

        // Побудувати конфігурацію
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(configFile) ?? AppContext.BaseDirectory)
            .AddJsonFile(Path.GetFileName(configFile), optional: false, reloadOnChange: false)
            .Build();

        // Отримати секцію WeatherApi
        var weatherApiSection = config.GetSection("WeatherApi");
        
        if (!weatherApiSection.Exists())
        {
            throw new InvalidOperationException(
                "Секція 'WeatherApi' не знайдена в appsettings.json");
        }

        // Спробувати прив'язати до WeatherApiSettings
        var settings = weatherApiSection.Get<WeatherApiSettings>();

        if (settings == null)
        {
            throw new InvalidOperationException(
                "Не вдалось завантажити конфігурацію WeatherApi з appsettings.json");
        }

        // Валідація обов'язкових полів
        if (string.IsNullOrEmpty(settings.ApiKey))
        {
            throw new InvalidOperationException(
                "API Key не може бути пустим. " +
                "Перевірте appsettings.json та додайте ваш OpenWeatherMap API ключ.");
        }

        return settings;
    }

    /// Завантажує будь-яку конфігурацію з JSON файлу за типом
    public static T LoadConfiguration<T>(string configFile, string sectionName) where T : class
    {
        if (!File.Exists(configFile))
        {
            throw new FileNotFoundException($"Файл конфігурації не знайдено: {configFile}");
        }

        var config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(configFile) ?? AppContext.BaseDirectory)
            .AddJsonFile(Path.GetFileName(configFile), optional: false, reloadOnChange: false)
            .Build();

        var section = config.GetSection(sectionName);
        
        if (!section.Exists())
        {
            throw new InvalidOperationException($"Секція '{sectionName}' не знайдена в {configFile}");
        }

        var settings = section.Get<T>();
        
        if (settings == null)
        {
            throw new InvalidOperationException($"Не вдалось завантажити конфігурацію '{sectionName}'");
        }

        return settings;
    }
}
