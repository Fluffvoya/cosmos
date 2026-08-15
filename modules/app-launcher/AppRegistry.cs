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
    public static List<RegisteredApp> Load(string? folder = null)
    {
        return DataStore.Load<List<RegisteredApp>>(DataFile, folder) ?? new List<RegisteredApp>();
    }

    /// <summary>
    /// Save the applications list to the data file.
    /// </summary>
    public static void Save(List<RegisteredApp> apps, string? folder = null)
    {
        DataStore.Save(DataFile, apps, folder);
    }

    /// <summary>
    /// Add a new registered application. Throws if an app with the same name already exists.
    /// </summary>
    public static void Add(RegisteredApp app, string? folder = null)
    {
        var apps = Load(folder);
        if (apps.Any(a => a.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new LauncherException(
                ErrorCode.DuplicateAppName,
                $"An application with the name '{app.Name}' is already registered.");
        }
        apps.Add(app);
        Save(apps, folder);
    }

    /// <summary>
    /// Remove a registered application by name. Throws if not found.
    /// </summary>
    public static void Remove(string name, string? folder = null)
    {
        var apps = Load(folder);
        var index = apps.FindIndex(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (index == -1)
        {
            throw new LauncherException(
                ErrorCode.AppNotFound,
                $"No application registered with the name '{name}'.");
        }
        apps.RemoveAt(index);
        Save(apps, folder);
    }

    /// <summary>
    /// Get a registered application by name. Returns null if not found.
    /// </summary>
    public static RegisteredApp? GetByName(string name, string? folder = null)
    {
        var apps = Load(folder);
        return apps.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all registered applications.
    /// </summary>
    public static List<RegisteredApp> GetAll(string? folder = null)
    {
        return Load(folder);
    }

    /// <summary>
    /// Search registered applications by name (case-insensitive substring match).
    /// </summary>
    public static List<RegisteredApp> Search(string query, string? folder = null)
    {
        var apps = Load(folder);
        if (string.IsNullOrWhiteSpace(query))
            return apps;

        return apps.Where(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Reorder registered applications by moving an app from one index to another.
    /// Throws if the indices are out of range.
    /// </summary>
    public static void Reorder(int fromIndex, int toIndex, string? folder = null)
    {
        var apps = Load(folder);
        if (fromIndex < 0 || fromIndex >= apps.Count)
        {
            throw new LauncherException(
                ErrorCode.AppNotFound,
                $"Source index {fromIndex} is out of range.");
        }
        if (toIndex < 0 || toIndex > apps.Count)
        {
            throw new LauncherException(
                ErrorCode.AppNotFound,
                $"Target index {toIndex} is out of range.");
        }

        var app = apps[fromIndex];
        apps.RemoveAt(fromIndex);
        // Adjust insertion index after removal
        if (toIndex > fromIndex) toIndex--;
        apps.Insert(toIndex, app);
        Save(apps, folder);
    }
}
