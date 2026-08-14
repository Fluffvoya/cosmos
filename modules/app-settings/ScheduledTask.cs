using System.Text.Json.Serialization;

namespace app_settings;

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
