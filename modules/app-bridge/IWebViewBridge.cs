namespace app_bridge;

/// <summary>
/// Abstraction for posting messages to the WebView2 frontend.
/// Decouples feature modules from the concrete WebView2 control.
/// </summary>
public interface IWebViewBridge
{
    /// <summary>
    /// Post a JSON message to the frontend via WebView2 postMessage.
    /// </summary>
    void PostMessage(string json);
}
