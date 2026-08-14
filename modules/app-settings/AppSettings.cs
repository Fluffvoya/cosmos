using System.Text.Json.Serialization;

namespace app_settings;

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
