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

/// <summary>
/// Application settings model.
/// </summary>
public class AppSettings
{
    [JsonPropertyName("tabPosition")]
    public string TabPosition { get; set; } = "top";

    [JsonPropertyName("tabStripWidth")]
    public int TabStripWidth { get; set; } = 140;

    [JsonPropertyName("pythonPath")]
    public string PythonPath { get; set; } = "";

    [JsonPropertyName("startupScriptPath")]
    public string StartupScriptPath { get; set; } = "";

    [JsonPropertyName("scheduledTasks")]
    public List<ScheduledTask> ScheduledTasks { get; set; } = new();
}

/// <summary>
/// Represents a scheduled task that runs a cm-script at a specified time.
/// </summary>
public class ScheduledTask
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("time")]
    public string Time { get; set; } = "00:00";

    [JsonPropertyName("scriptPath")]
    public string ScriptPath { get; set; } = "";

    /// <summary>
    /// Recurrence type: "once", "daily", "weekly".
    /// </summary>
    [JsonPropertyName("recurrence")]
    public string Recurrence { get; set; } = "daily";

    /// <summary>
    /// Days of week for weekly recurrence (0=Sun, 1=Mon, ..., 6=Sat).
    /// Only used when Recurrence is "weekly".
    /// </summary>
    [JsonPropertyName("days")]
    public List<int> Days { get; set; } = new();

    /// <summary>
    /// For "once" tasks, the date to run (yyyy-MM-dd). Empty means first matching time.
    /// </summary>
    [JsonPropertyName("onceDate")]
    public string OnceDate { get; set; } = "";
}
