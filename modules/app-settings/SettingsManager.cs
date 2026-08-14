using System;
using System.IO;
using System.Text.Json;

namespace app_settings;

/// <summary>
/// Manages application settings persistence.
/// Settings are stored in a JSON file in the user's home directory (~/.cosmos/).
/// </summary>
public class SettingsManager
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".cosmos");

    private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

    /// <summary>
    /// The current application settings.
    /// </summary>
    public AppSettings Current { get; private set; } = new();

    /// <summary>
    /// Event raised when settings are changed.
    /// </summary>
    public event Action<AppSettings>? SettingsChanged;

    /// <summary>
    /// Load settings from the settings file.
    /// </summary>
    public void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    Current = settings;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Save current settings to the settings file.
    /// </summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(Current, options);
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Update settings and notify listeners.
    /// </summary>
    public void Update(AppSettings newSettings)
    {
        Current = newSettings;
        Save();
        SettingsChanged?.Invoke(Current);
    }
}
