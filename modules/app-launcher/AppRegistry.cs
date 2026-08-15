using app_settings;
using cosmos_error;

namespace app_launcher;

/// <summary>
/// Manages the collection of registered applications.
/// Persists to ~/.cosmos/launch-apps.json via DataStore.
/// </summary>
public class AppRegistry
{
    private const string DataFile = "launch-apps.json";

    /// <summary>
    /// Load all registered applications from the data file.
    /// Returns an empty list if the file does not exist.
    /// </summary>
    public static List<RegisteredApp> Load()
    {
        return DataStore.Load<List<RegisteredApp>>(DataFile) ?? new List<RegisteredApp>();
    }

    /// <summary>
    /// Save the applications list to the data file.
    /// </summary>
    public static void Save(List<RegisteredApp> apps)
    {
        DataStore.Save(DataFile, apps);
    }

    /// <summary>
    /// Add a new registered application. Throws if an app with the same name already exists.
    /// </summary>
    public static void Add(RegisteredApp app)
    {
        var apps = Load();
        if (apps.Any(a => a.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new LauncherException(
                ErrorCode.DuplicateAppName,
                $"An application with the name '{app.Name}' is already registered.");
        }
        apps.Add(app);
        Save(apps);
    }

    /// <summary>
    /// Remove a registered application by name. Throws if not found.
    /// </summary>
    public static void Remove(string name)
    {
        var apps = Load();
        var index = apps.FindIndex(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (index == -1)
        {
            throw new LauncherException(
                ErrorCode.AppNotFound,
                $"No application registered with the name '{name}'.");
        }
        apps.RemoveAt(index);
        Save(apps);
    }

    /// <summary>
    /// Get a registered application by name. Returns null if not found.
    /// </summary>
    public static RegisteredApp? GetByName(string name)
    {
        var apps = Load();
        return apps.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all registered applications.
    /// </summary>
    public static List<RegisteredApp> GetAll()
    {
        return Load();
    }

    /// <summary>
    /// Search registered applications by name (case-insensitive substring match).
    /// </summary>
    public static List<RegisteredApp> Search(string query)
    {
        var apps = Load();
        if (string.IsNullOrWhiteSpace(query))
            return apps;

        return apps.Where(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
