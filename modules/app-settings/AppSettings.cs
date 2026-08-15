using System.Text.Json.Serialization;

namespace app_settings;

/// <summary>
/// Application settings model.
/// Only stores user preferences — runtime data (tasks, script output)
/// is stored separately via DataStore.
/// </summary>
public class AppSettings
{
    [JsonPropertyName("tabPosition")]
    public string TabPosition { get; set; } = "top";

    [JsonPropertyName("tabStripWidth")]
    public int TabStripWidth { get; set; } = 140;

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("pythonPath")]
    public string PythonPath { get; set; } = "";

    [JsonPropertyName("startupScriptPath")]
    public string StartupScriptPath { get; set; } = "";
}

/// <summary>
/// A single line of script terminal output.
/// </summary>
public class ScriptOutputEntry
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("level")]
    public string Level { get; set; } = "";
}
