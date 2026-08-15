using argument;
using bridge;
using client;
using cosmos_error;
using func_router;
using public_model;

namespace script_func;

/// <summary>
/// Provides func_router.Function objects that wrap Client interfaces.
/// Each Function sends a Request to the main program via IServer.Execute.
/// </summary>
public static class ScriptFunctions
{
    /// <summary>
    /// Shows a native message box with the given title and message.
    /// Args: [String name, String message]
    /// </summary>
    public static Function MessageBox => new(
        (IServer server, List<object> args) =>
        {
            var name = (string)args[0];
            var message = (string)args[1];
            var request = Client.CreateRequest(Client.MessageBox(name, message));
            server.Execute(request);
        },
        ArgumentType.String, ArgumentType.String
    );

    /// <summary>
    /// Logs a message.
    /// Args: [String content]
    /// </summary>
    public static Function Log => new(
        (IServer server, List<object> args) =>
        {
            var content = (string)args[0];
            var request = Client.CreateRequest(Client.Log(content));
            server.Execute(request);
        },
        ArgumentType.String
    );

    /// <summary>
    /// Logs a warning message.
    /// Args: [String content]
    /// </summary>
    public static Function Warning => new(
        (IServer server, List<object> args) =>
        {
            var content = (string)args[0];
            var request = Client.CreateRequest(Client.Warning(content));
            server.Execute(request);
        },
        ArgumentType.String
    );

    /// <summary>
    /// Logs an error message.
    /// Args: [String content]
    /// </summary>
    public static Function Error => new(
        (IServer server, List<object> args) =>
        {
            var content = (string)args[0];
            var request = Client.CreateRequest(Client.Error(content));
            server.Execute(request);
        },
        ArgumentType.String
    );

    private static readonly HashSet<string> ValidMessageBarLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "info", "warning", "error"
    };

    /// <summary>
    /// Shows a non-blocking message bar (toast) with the given message and level.
    /// Args: [String message, String level] — level must be "info", "warning", or "error".
    /// Throws ScriptFuncException if level is not a recognized value.
    /// </summary>
    public static Function MessageBar => new(
        (IServer server, List<object> args) =>
        {
            var message = (string)args[0];
            var level = (string)args[1];
            if (!ValidMessageBarLevels.Contains(level))
                throw new ScriptFuncException(
                    ErrorCode.InvalidArgumentValue,
                    $"Invalid message bar level '{level}'. Expected one of: info, warning, error.");
            var request = Client.CreateRequest(Client.MessageBar(message, level));
            server.Execute(request);
        },
        ArgumentType.String, ArgumentType.String
    );

    /// <summary>
    /// Gets the current user name from the main program.
    /// Args: none
    /// </summary>
    public static Function GetUserName => new(
        (IServer server, List<object> args) =>
        {
            var request = Client.CreateRequest(Client.GetUserName());
            server.Execute(request);
        }
    );

    /// <summary>
    /// Launches a registered application by name.
    /// Args: [String appName] — the name of the registered app to launch.
    /// Throws ScriptFuncException if the app is not found or the path is invalid.
    /// </summary>
    public static Function OpenRegisteredApp => new(
        (IServer server, List<object> args) =>
        {
            var appName = (string)args[0];
            var request = Client.CreateRequest(Client.OpenRegisteredApp(appName));
            var result = server.Execute(request);
            var message = Client.GetResponseMessage(result);
            if (message != null && message.StartsWith("error:"))
            {
                var detail = message["error:".Length..];
                throw new ScriptFuncException(
                    ErrorCode.AppNotFound,
                    $"Failed to open '{appName}': {detail}");
            }
        },
        ArgumentType.String
    );

    /// <summary>
    /// Plays a ringtone audio file in the Ringtone tab.
    /// Args: [String audioPath] — path to the audio file to play.
    /// Throws ScriptFuncException if the file does not exist.
    /// </summary>
    public static Function PlayRingtone => new(
        (IServer server, List<object> args) =>
        {
            var audioPath = (string)args[0];
            var expandedPath = Environment.ExpandEnvironmentVariables(audioPath);
            if (!File.Exists(expandedPath))
                throw new ScriptFuncException(
                    ErrorCode.InvalidArgumentValue,
                    $"Ringtone file not found: '{audioPath}'");
            var request = Client.CreateRequest(Client.PlayRingtone(audioPath));
            server.Execute(request);
        },
        ArgumentType.String
    );
}

