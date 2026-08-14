# app Module

## Purpose

The main WinForms application. Hosts a WebView2 control for the UI, implements the `IServer` interface to bridge cm-script requests to the frontend, manages settings persistence, and runs the scheduled task background service.

## Namespace

`app`

## Classes

### `Program`

Application entry point.

```csharp
internal static class Program
{
    [STAThread]
    static void Main()
}
```

Configures high-DPI mode (`PerMonitorV2`), creates `MainWindow`, and starts the WinForms message loop.

### `MainWindow`

The main form. Implements three interfaces:

| Interface | Method | Purpose |
|-----------|--------|---------|
| `IServer` | `Execute(string)` | Receives cm-script requests and routes them |
| `IWebViewBridge` | `PostMessage(string)` | Posts JSON to the WebView2 frontend |
| `IScriptRunner` | `Run(string)` | Executes cm-script source strings |

#### Key Members

| Member | Type | Description |
|--------|------|-------------|
| `Instance` | `static MainWindow?` | Singleton accessor |
| `Script` | `Script?` | The cm-script engine instance |
| `SettingsManager` | `SettingsManager` | Settings persistence manager |

#### Window Behavior

- Frameless window (`FormBorderStyle.None`) with DWM-extended frame for native minimize/maximize/close animations.
- Custom `WndProc` handles `WM_NCCALCSIZE` (collapse non-client area) and `WM_NCHITTEST` (resize borders and drag regions).
- JS-initiated drag via `ReleaseCapture` + `SendMessage(WM_NCLBUTTONDOWN)`.

#### Initialization Flow

1. `InitializeAsync()` is called on the `Load` event.
2. WebView2 is initialized with a virtual host mapping (`app.local` → `wwwroot/`).
3. `ServerBridge` is created to handle frontend messages.
4. cm-script engine is initialized with the configured Python path.
5. Built-in functions are registered: `ShowMessage`, `Log`, `Warning`, `Error`, `GetUserName`.
6. `ScheduledTaskRunner` is started.
7. Startup script is executed if configured (`.cms` through script engine, `.py`/other as external process).

#### Frontend Message Handling

`OnWebMessageReceived` dispatches string messages for window control:
- `window:minimize`, `window:maximize`, `window:close`, `window:drag`, `window:dblclick-maximize`

Structured JSON messages are forwarded to `ServerBridge.HandleFrontendMessage`.

### `ServerBridge`

Bridges `IServer.Execute` requests from cm-script to the WebView2 frontend using a request/response correlation pattern.

```csharp
public class ServerBridge
{
    public ServerBridge(WebView2 webView, MainWindow mainWindow, Action<string, string, string> logToUI)
    public void SetTaskRunner(ScheduledTaskRunner runner)
    public void SendInternalLog(string level, string message, string sender = "program")
    public void HandleFrontendMessage(string messageJson)
    public string Execute(string requestJson)
}
```

#### `Execute` Flow

1. Deserializes the `Request` JSON.
2. **Intercepts** built-in requests:
   - `ShowMessage` → shows a native `MessageBox` on the UI thread.
   - `Log`/`Warning`/`Error` → forwards as internal log messages.
3. For other requests: generates a unique request ID, creates a `TaskCompletionSource`, posts the request to the frontend via `postMessage`, and blocks (up to 30 seconds) waiting for the frontend to respond.
4. Returns the frontend's response as a serialized `Response` JSON.

#### Frontend Message Types

| Type | Direction | Description |
|------|-----------|-------------|
| `request` | Host → Frontend | Function call request with `requestId` |
| `response` | Frontend → Host | Response to a request, correlated by `requestId` |
| `settingsChanged` | Frontend → Host | User modified settings in the UI |
| `validatePythonPath` | Frontend → Host | Validate a Python interpreter path |
| `browsePythonPath` | Frontend → Host | Open file dialog for Python path |
| `browseStartupScriptPath` | Frontend → Host | Open file dialog for startup script |
| `schedulerTasksChanged` | Frontend → Host | Scheduled tasks list updated |
| `schedulerRunTask` | Frontend → Host | "Run Now" button clicked |
| `schedulerBrowseScript` | Frontend → Host | Browse for scheduler script file |
| `internalLog` | Host → Frontend | Log message for the Log panel |
| `settingsLoaded` | Host → Frontend | Initial settings on page load |
| `pythonPathValidation` | Host → Frontend | Validation result |
| `browseResult` | Host → Frontend | File dialog result (Python path) |
| `startupScriptBrowseResult` | Host → Frontend | File dialog result (startup script) |
| `schedulerRunResult` | Host → Frontend | Result of manual task execution |
| `schedulerBrowseResult` | Host → Frontend | File dialog result (scheduler script) |
| `schedulerTaskAutoDisabled` | Host → Frontend | Once-task disabled after execution |

## Dependencies

- `app-bridge` (IScriptRunner, IWebViewBridge)
- `app-scheduler` (ScheduledTaskRunner)
- `app-settings` (SettingsManager, AppSettings)
- `bridge` (IServer)
- `cm-script` (Script)
- `script-func` (ScriptFunctions)
- `public-model` (Request, Response)
- `server` (Server)
- Microsoft.Web.WebView2
