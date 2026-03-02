using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UtilityBelt.App.Models;

namespace UtilityBelt.App.Services;

public sealed class SettingsService
{
    public string AppDataDirectory { get; }
    public string SettingsPath { get; }

    public SettingsService(string appName = "UtilityBelt")
    {
        AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            appName);

        SettingsPath = Path.Combine(AppDataDirectory, "appsettings.json");
    }

    public AppSettings LoadOrCreateDefault(string defaultSettingsJsonPathInAppFolder)
    {
        Directory.CreateDirectory(AppDataDirectory);

        if (!File.Exists(SettingsPath))
        {
            File.Copy(defaultSettingsJsonPathInAppFolder, SettingsPath, overwrite: false);
        }

        return Load();
    }

    public AppSettings Load()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(SettingsPath, optional: false, reloadOnChange: false)
            .Build();

        var settings = new AppSettings();
        config.Bind(settings);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppDataDirectory);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}