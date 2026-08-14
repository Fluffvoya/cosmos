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
    /// Shows a message with the given name and message.
    /// Args: [String name, String message]
    /// </summary>
    public static Function ShowMessage => new(
        (IServer server, List<object> args) =>
        {
            var name = (string)args[0];
            var message = (string)args[1];
            var request = Client.CreateRequest(Client.ShowMessage(name, message));
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

    /// <summary>
    /// Shows a non-blocking message bar (toast) with the given message and level.
    /// Args: [String message, String level] — level is "info", "warning", or "error".
    /// </summary>
    public static Function ShowMessageBar => new(
        (IServer server, List<object> args) =>
        {
            var message = (string)args[0];
            var level = (string)args[1];
            var request = Client.CreateRequest(Client.ShowMessageBar(message, level));
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
}

