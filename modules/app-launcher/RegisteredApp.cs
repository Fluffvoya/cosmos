using System.Text.Json.Serialization;

namespace app_launcher;

/// <summary>
/// Represents a registered application that can be launched from the Launch App tab.
/// </summary>
public class RegisteredApp
{
    /// <summary>
    /// Display name of the application (used as the unique key).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Absolute path to the application executable.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Optional command-line arguments passed to the application on launch.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    /// <summary>
    /// Creates a RegisteredApp from a name and executable path.
    /// </summary>
    public static RegisteredApp Create(string name, string path, string? arguments = null)
    {
        return new RegisteredApp
        {
            Name = name,
            Path = path,
            Arguments = arguments
        };
    }
}
