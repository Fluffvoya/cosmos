using System.Text.Json.Serialization;

namespace app_settings;

/// <summary>
/// Start page configuration model.
/// Stored separately from main settings in start-config.json.
/// </summary>
public class StartConfig
{
    [JsonPropertyName("timeFormat")]
    public string TimeFormat { get; set; } = "24";
}
