namespace cosmos_error;

/// <summary>
/// Represents an error in the launcher module (app not found, duplicate name, invalid path, etc.).
/// </summary>
public class LauncherException : CosmosException
{
    public LauncherException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
