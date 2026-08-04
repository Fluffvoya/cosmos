using cosmos_error;

namespace path;

/// <summary>
/// Handles path resolution for the Cosmos runtime.
/// Paths prefixed with "cosmos://" are resolved relative to the base absolute path.
/// </summary>
public class CosmosPath
{
    private const string CosmosScheme = "cosmos://";

    /// <summary>
    /// The absolute base path used to resolve cosmos:// prefixed paths.
    /// </summary>
    public string AbsolutePath { get; }

    /// <summary>
    /// Creates a new CosmosPath instance with the specified absolute base path.
    /// </summary>
    /// <param name="absolutePath">The absolute path used as the base for resolving cosmos:// paths.</param>
    /// <exception cref="PathException">Thrown when absolutePath is null or empty.</exception>
    public CosmosPath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
        {
            throw new PathException(ErrorCode.PathNullOrEmpty, "Absolute path cannot be null or empty.");
        }

        AbsolutePath = absolutePath;
    }

    /// <summary>
    /// Resolves the given path. If the path starts with "cosmos://",
    /// replaces the scheme with the base absolute path.
    /// Otherwise, returns the path unchanged.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>The resolved absolute path.</returns>
    /// <exception cref="PathException">Thrown when path is null or empty.</exception>
    public string Resolve(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new PathException(ErrorCode.PathNullOrEmpty, "Path cannot be null or empty.");
        }

        if (path.StartsWith(CosmosScheme))
        {
            var relativePath = path[CosmosScheme.Length..];
            return Path.Combine(AbsolutePath, relativePath);
        }

        return path;
    }
}
