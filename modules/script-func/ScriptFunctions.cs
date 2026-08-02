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
    /// Shows a window with the given name and message.
    /// Args: [String name, String message]
    /// </summary>
    public static Function ShowWindow => new(
        (IServer server, List<object> args) =>
        {
            var name = (string)args[0];
            var message = (string)args[1];
            var request = Client.CreateRequest(Client.ShowWindow(name, message));
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
