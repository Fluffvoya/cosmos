# app-scheduler Module

## Purpose

Background service that periodically checks scheduled tasks and runs them at their configured times. Supports daily, weekly, and one-time recurrence patterns.

## Namespace

`app_scheduler`

## Classes

### `ScheduledTaskRunner`

Implements `IDisposable`. Uses a `System.Threading.Timer` that fires every 15 seconds.

```csharp
public class ScheduledTaskRunner : IDisposable
{
    public ScheduledTaskRunner(
        SettingsManager settingsManager,
        Action<string, string, string> logToUI,
        IScriptRunner? scriptRunner = null,
        IWebViewBridge? webViewBridge = null)

    public void Start()
    public void Stop()
    public async Task<(bool Success, string Message)> RunTaskNow(string scriptPath)
    public void Dispose()
}
```

#### Constructor Parameters

| Parameter | Description |
|-----------|-------------|
| `settingsManager` | Provides the list of scheduled tasks |
| `logToUI` | Callback `(level, message, sender)` to send log messages to the UI |
| `scriptRunner` | Optional. Executes cm-script source strings. Required for task execution |
| `webViewBridge` | Optional. Posts messages to the frontend. Used to notify when a once-task is auto-disabled |

#### `Start()`

Begins the 15-second polling loop. Logs the start event and the number of configured tasks.

#### `Stop()`

Stops the timer. Logs the stop event.

#### `RunTaskNow(string scriptPath)`

Immediately executes a script file outside the scheduler loop. Used by the frontend "Run Now" button.

**Returns:** `(bool Success, string Message)` tuple.

| Scenario | Success | Message |
|----------|---------|---------|
| Empty path | `false` | `"No script path specified"` |
| File not found | `false` | `"File not found: <path>"` |
| Script runner not initialized | `false` | `"Script engine not initialized"` |
| Execution succeeded | `true` | `"OK"` |
| Execution threw | `false` | Exception message |

Expands environment variables in the script path before checking.

#### Timer Callback (`CheckTasks`)

Runs every 15 seconds. For each enabled task:

1. Checks if the current `HH:mm` matches `task.Time`.
2. If matched, evaluates recurrence via `ShouldRunToday`:
   - **`"daily"`**: always runs.
   - **`"weekly"`**: runs only if `task.Days` contains today's day-of-week (0=Sun).
   - **`"once"`**: runs on `task.OnceDate` (or immediately if no date set).
3. Fires the task asynchronously via `Task.Run`.
4. After a `"once"` task completes successfully, disables it, saves settings, and posts a `schedulerTaskAutoDisabled` message to the frontend.

**De-duplication:** Tracks `_lastRunMinute` to avoid firing tasks more than once per minute.

## Dependencies

- `app-bridge` (IScriptRunner, IWebViewBridge)
- `app-settings` (SettingsManager, ScheduledTask)
