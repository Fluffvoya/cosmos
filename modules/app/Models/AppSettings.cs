namespace app.Models;

/// <summary>
/// Where the tab strip is placed relative to the content area.
/// </summary>
public enum TabPosition
{
    Top,
    Left,
    Right
}

/// <summary>
/// Application-wide settings. Instances are persisted as JSON.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Position of the tab strip.
    /// </summary>
    public TabPosition tabPosition { get; set; } = TabPosition.Top;

    /// <summary>
    /// Path to the Python interpreter used by cm-script PYTHON commands.
    /// </summary>
    public string pythonPath { get; set; } = string.Empty;
}
