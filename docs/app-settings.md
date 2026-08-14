# app-settings Module

## Purpose

Defines the application settings model and persistence layer. Settings are stored as JSON in `~/.cosmos/settings.json`.

## Namespace

`app_settings`

## Classes

### `AppSettings`

The root settings model. All properties have JSON serialization attributes.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TabPosition` | `string` | `"top"` | Tab strip position (`"top"`, `"bottom"`, `"left"`, `"right"`) |
| `TabStripWidth` | `int` | `140` | Width of the tab strip in pixels |
| `PythonPath` | `string` | `""` | Path to the Python interpreter (`python.exe`) |
| `StartupScriptPath` | `string` | `""` | Path to the startup script (`.cms` or `.py`) |
| `ScheduledTasks` | `List<ScheduledTask>` | `[]` | List of scheduled tasks |

### `ScheduledTask`

Represents a single scheduled task.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Whether the task is active |
| `Time` | `string` | `"00:00"` | Time to run in `HH:mm` format |
| `ScriptPath` | `string` | `""` | Path to the `.cms` script file |
| `Recurrence` | `string` | `"daily"` | Recurrence type: `"once"`, `"daily"`, `"weekly"` |
| `Days` | `List<int>` | `[]` | Days of week for weekly recurrence (0=Sun, 1=Mon, ..., 6=Sat) |
| `OnceDate` | `string` | `""` | Date for once tasks in `yyyy-MM-dd` format. Empty = first matching time |

### `SettingsManager`

Manages loading, saving, and updating settings.

```csharp
public class SettingsManager
{
    public AppSettings Current { get; }
    public event Action<AppSettings>? SettingsChanged;

    public void Load()
    public void Save()
    public void Update(AppSettings newSettings)
}
```

**`Load`**: Reads `~/.cosmos/settings.json`. If the file doesn't exist or is corrupt, keeps the default `AppSettings`. Errors are written to `Console.Error` (silent to user).

**`Save`**: Writes `Current` to `~/.cosmos/settings.json` with indented JSON. Creates the `~/.cosmos/` directory if it doesn't exist.

**`Update`**: Replaces `Current`, calls `Save`, and raises the `SettingsChanged` event.

## Storage Location

```
~/.cosmos/settings.json
```

On Windows: `C:\Users\<username>\.cosmos\settings.json`

## Dependencies

None. This is a leaf module (no project references).
