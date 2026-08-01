using System.Windows;
using bridge;

namespace app;

/// <summary>
/// Application entry point. Implements IServer so cm-script can call back into the UI.
/// </summary>
public partial class App : Application, IServer
{
    private static App? _instance;

    /// <summary>
    /// Shared Script instance for running cm-script sources.
    /// Initialized lazily on first use.
    /// </summary>
    public static cm_script.Script? Script { get; private set; }

    /// <summary>
    /// Singleton accessor for the current App as IServer.
    /// </summary>
    public static IServer Server => _instance ?? throw new InvalidOperationException("App not initialized");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _instance = this;

        // Initialize the Script engine with this app as the IServer implementation.
        // The python path starts empty; the user can set it in Settings and it will
        // be synced to Script.python via MainViewModel.applySettings().
        Script = new cm_script.Script(this, string.Empty);
    }

    /// <summary>
    /// IServer.Execute — receives requests from cm-script and returns a response.
    /// Currently returns an empty string placeholder.
    /// </summary>
    public string Execute(string requests)
    {
        // TODO: dispatch requests to the UI layer and return meaningful responses.
        return string.Empty;
    }
}
