using System.Text.Json;
using app_bridge;

namespace app_script;

/// <summary>
/// Handles cm-script execution requests from the Script Terminal tab.
/// Runs each line of user-provided cm-script source individually and posts
/// per-line results back to the frontend.
/// </summary>
public class ScriptRunner
{
    private readonly IScriptRunner _scriptRunner;
    private readonly IWebViewBridge _webViewBridge;
    private readonly Action<string, string, string> _logToUI;

    /// <summary>
    /// Create a new ScriptRunner.
    /// </summary>
    /// <param name="scriptRunner">The cm-script engine abstraction.</param>
    /// <param name="webViewBridge">Bridge for posting messages to the frontend.</param>
    /// <param name="logToUI">Callback for logging: (level, message, sender).</param>
    public ScriptRunner(IScriptRunner scriptRunner, IWebViewBridge webViewBridge, Action<string, string, string> logToUI)
    {
        _scriptRunner = scriptRunner;
        _webViewBridge = webViewBridge;
        _logToUI = logToUI;
    }

    /// <summary>
    /// Execute a cm-script source string (single line) and post the result to the frontend.
    /// Runs on a background thread; the result is posted as a scriptRunResult message.
    /// </summary>
    /// <param name="source">The cm-script source code to execute (single line).</param>
    public void RunSource(string source)
    {
        Task.Run(async () =>
        {
            var success = false;
            var message = "";

            try
            {
                await _scriptRunner.Run(source);
                success = true;
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
            }

            var responseJson = JsonSerializer.Serialize(new
            {
                type = "scriptRunResult",
                success = success,
                message = message
            });

            try
            {
                _webViewBridge.PostMessage(responseJson);
            }
            catch { }
        });
    }
}
