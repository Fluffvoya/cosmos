using System.Collections.Concurrent;
using System.Text.Json;

namespace app_settings;

/// <summary>
/// Generic JSON file store for feature-specific data.
/// Each data type lives in its own file under ~/.cosmos/.
/// </summary>
public class DataStore
{
    private static readonly string DefaultDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".cosmos");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    // Per-path semaphore to prevent concurrent read/write on the same file
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private static SemaphoreSlim GetLock(string path)
    {
        return _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>
    /// Load data from a JSON file. Uses ~/.cosmos/ by default.
    /// Returns default(T) if the file does not exist or cannot be read.
    /// </summary>
    public static T? Load<T>(string fileName, string? folder = null)
    {
        var path = Path.GetFullPath(Path.Combine(folder ?? DefaultDataFolder, fileName));
        var sem = GetLock(path);
        sem.Wait();
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load {fileName}: {ex.Message}");
        }
        finally
        {
            sem.Release();
        }
        return default;
    }

    /// <summary>
    /// Save data to a JSON file. Uses ~/.cosmos/ by default.
    /// Creates the directory if it does not exist.
    /// </summary>
    public static void Save<T>(string fileName, T data, string? folder = null)
    {
        var dir = folder ?? DefaultDataFolder;
        var path = Path.GetFullPath(Path.Combine(dir, fileName));
        var sem = GetLock(path);
        sem.Wait();
        try
        {
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save {fileName}: {ex.Message}");
        }
        finally
        {
            sem.Release();
        }
    }
}
