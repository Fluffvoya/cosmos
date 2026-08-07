using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace app;

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
            // Log error but continue with default settings
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
    /// <param name="newSettings">The new settings to apply.</param>
    public void Update(AppSettings newSettings)
    {
        Current = newSettings;
        Save();
        SettingsChanged?.Invoke(Current);
    }
}

/// <summary>
/// Application settings model.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Position of the tab strip (top, left, right).
    /// </summary>
    [JsonPropertyName("tabPosition")]
    public string TabPosition { get; set; } = "top";

    /// <summary>
    /// Width of the tab strip when positioned left or right.
    /// </summary>
    [JsonPropertyName("tabStripWidth")]
    public int TabStripWidth { get; set; } = 140;

    /// <summary>
    /// Path to the Python interpreter.
    /// </summary>
    [JsonPropertyName("pythonPath")]
    public string PythonPath { get; set; } = "";

    /// <summary>
    /// Path to a script to run on application startup.
    /// </summary>
    [JsonPropertyName("startupScriptPath")]
    public string StartupScriptPath { get; set; } = "";
}
