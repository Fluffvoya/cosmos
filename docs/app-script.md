# app-script Module

## Purpose

Handles cm-script execution requests from the Script Terminal tab. Runs user-provided cm-script source code line-by-line via the `IScriptRunner` abstraction and posts per-line results back to the frontend via `IWebViewBridge`.

## Namespace

`app_script`

## Classes

### `ScriptRunner`

Orchestrates script execution on a background thread and delivers results to the WebView frontend.

```csharp
public class ScriptRunner
{
    public ScriptRunner(IScriptRunner scriptRunner, IWebViewBridge webViewBridge, Action<string, string, string> logToUI)
    public void RunSource(string source)
}
```

#### Constructor

```csharp
public ScriptRunner(IScriptRunner scriptRunner, IWebViewBridge webViewBridge, Action<string, string, string> logToUI)
```

- **Arguments:**
  - `scriptRunner` — The cm-script engine abstraction (`IScriptRunner`).
  - `webViewBridge` — Bridge for posting JSON messages to the frontend (`IWebViewBridge`).
  - `logToUI` — Callback for logging to the UI: `(level, message, sender)`.

#### `RunSource(string source)`

Executes a single line of cm-script source on a background thread via `Task.Run`. On completion, posts a `scriptRunResult` JSON message to the frontend:

- On success: `{ "type": "scriptRunResult", "success": true, "message": "" }`
- On failure: `{ "type": "scriptRunResult", "success": false, "message": "Error: ..." }`

If `PostMessage` itself throws, the exception is silently swallowed.

- **Arguments:**
  - `source` — The cm-script source code to execute (single line).

## Dependencies

- `app-bridge` (`IScriptRunner`, `IWebViewBridge`)
