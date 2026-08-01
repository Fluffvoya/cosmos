namespace app.Models;

/// <summary>
/// Represents a single tab in the browser-like interface.
/// </summary>
public class TabInfo
{
    /// <summary>
    /// Unique identifier for drag-and-drop tracking.
    /// </summary>
    public Guid id { get; } = Guid.NewGuid();

    /// <summary>
    /// Display title shown on the tab header.
    /// </summary>
    public string title { get; set; } = "Untitled";

    /// <summary>
    /// The type of content this tab displays (e.g. "Settings", "Document", "Browser").
    /// </summary>
    public string contentType { get; set; } = "Document";

    /// <summary>
    /// Optional icon identifier (e.g. a Segoe Fluent Icons glyph or pack URI).
    /// </summary>
    public string? icon { get; set; }

    /// <summary>
    /// Optional tag for storing a ViewModel or other data associated with this tab.
    /// </summary>
    public object? tag { get; set; }
}
