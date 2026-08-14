namespace app_bridge;

/// <summary>
/// Abstraction for running cm-script source code.
/// Decouples feature modules from the concrete Script engine.
/// </summary>
public interface IScriptRunner
{
    /// <summary>
    /// Run a cm-script source string.
    /// </summary>
    Task Run(string source);
}
