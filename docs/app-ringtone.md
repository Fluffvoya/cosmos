# app-ringtone Module

## Purpose

Data model for ringtone entries used by the Ringtone tab. Each entry tracks an audio file path and a display label. The frontend Ringtone tab manages playback and user interaction.

## Namespace

`app_ringtone`

## Classes

### `RingtoneEntry`

Represents a single ringtone entry with an identifier, file path, and display label.

```csharp
public class RingtoneEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    public static RingtoneEntry FromPath(string filePath)
}
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier for this ringtone entry |
| `FilePath` | `string` | Absolute path to the audio file |
| `Label` | `string` | Display label (typically the file name) |

#### `FromPath(string filePath)`

Factory method that creates a `RingtoneEntry` from a file path. Generates a new GUID for the `Id` and derives the `Label` from the file name using `Path.GetFileName`.

**Returns:** A new `RingtoneEntry` instance.

## Dependencies

None (leaf module with no project references).
