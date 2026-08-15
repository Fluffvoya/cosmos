using System.Text.Json.Serialization;

namespace app_ringtone;

/// <summary>
/// Represents a single ringtone entry with an identifier, file path, and display label.
/// </summary>
public class RingtoneEntry
{
    /// <summary>
    /// Unique identifier for this ringtone entry.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Absolute path to the audio file.
    /// </summary>
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the ringtone (typically the file name).
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Creates a RingtoneEntry from a file path, deriving the label from the file name.
    /// </summary>
    public static RingtoneEntry FromPath(string filePath)
    {
        return new RingtoneEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            FilePath = filePath,
            Label = System.IO.Path.GetFileName(filePath)
        };
    }
}
