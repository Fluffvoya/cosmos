# app-launcher Module

## Purpose

Data model and registry for applications that can be launched from the Launch App tab. Each registered application tracks a display name, executable path, and optional command-line arguments. The registry persists to `~/.cosmos/launch-apps.json` via `DataStore`.

## Namespace

`app_launcher`

## Classes

### `RegisteredApp`

Represents a registered application with a name, executable path, and optional arguments.

```csharp
public class RegisteredApp
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    public static RegisteredApp Create(string name, string path, string? arguments = null)
}
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Display name of the application (used as the unique key) |
| `Path` | `string` | Absolute path to the application executable |
| `Arguments` | `string?` | Optional command-line arguments passed on launch |

#### `Create(string name, string path, string? arguments = null)`

Factory method that creates a `RegisteredApp` from the given name, path, and optional arguments.

**Returns:** A new `RegisteredApp` instance.

### `AppRegistry`

Static class that manages the collection of registered applications. Uses `DataStore` for persistence.

```csharp
public static class AppRegistry
{
    public static List<RegisteredApp> Load()
    public static void Save(List<RegisteredApp> apps)
    public static void Add(RegisteredApp app)
    public static void Remove(string name)
    public static RegisteredApp? GetByName(string name)
    public static List<RegisteredApp> GetAll()
    public static List<RegisteredApp> Search(string query)
}
```

#### `Load()`

Loads all registered applications from `~/.cosmos/launch-apps.json`. Returns an empty list if the file does not exist.

#### `Save(List<RegisteredApp> apps)`

Saves the applications list to the data file.

#### `Add(RegisteredApp app)`

Adds a new registered application. Throws `LauncherException` with `ErrorCode.DuplicateAppName` if an app with the same name already exists (case-insensitive).

#### `Remove(string name)`

Removes a registered application by name. Throws `LauncherException` with `ErrorCode.AppNotFound` if not found.

#### `GetByName(string name)`

Gets a registered application by name (case-insensitive). Returns `null` if not found.

#### `GetAll()`

Returns all registered applications.

#### `Search(string query)`

Searches registered applications by name using case-insensitive substring match. Returns all apps if the query is empty or whitespace.

## Dependencies

- `app-settings` (DataStore for JSON persistence)
- `error` (LauncherException, ErrorCode)
