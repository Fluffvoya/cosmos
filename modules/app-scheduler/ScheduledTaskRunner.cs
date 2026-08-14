using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using app_bridge;
using app_settings;

namespace app_scheduler;

/// <summary>
/// Background service that periodically checks scheduled tasks and runs them
/// when their scheduled time arrives.
/// </summary>
public class ScheduledTaskRunner : IDisposable
{
    private readonly SettingsManager _settingsManager;
    private readonly Action<string, string, string> _logToUI;
    private readonly IScriptRunner? _scriptRunner;
    private readonly IWebViewBridge? _webViewBridge;
    private readonly string? _dataFolder;
    private Timer? _timer;
    private string? _lastRunMinute;

    public ScheduledTaskRunner(
        SettingsManager settingsManager,
        Action<string, string, string> logToUI,
        IScriptRunner? scriptRunner = null,
        IWebViewBridge? webViewBridge = null,
        string? dataFolder = null)
    {
        _settingsManager = settingsManager;
        _logToUI = logToUI;
        _scriptRunner = scriptRunner;
        _webViewBridge = webViewBridge;
        _dataFolder = dataFolder;
    }

    /// <summary>
    /// Start the scheduled task runner.
    /// Checks every 15 seconds for tasks that should fire.
    /// </summary>
    public void Start()
    {
        _logToUI("Info", "Scheduled task runner started", "program");
        _timer = new Timer(CheckTasks, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
        var tasks = DataStore.Load<List<ScheduledTask>>("tasks.json", _dataFolder) ?? new List<ScheduledTask>();
        _logToUI("Info", $"Checking {tasks.Count} scheduled tasks", "program");
    }

    /// <summary>
    /// Stop the runner.
    /// </summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _logToUI("Info", "Scheduled task runner stopped", "program");
    }

    /// <summary>
    /// Timer callback - checks each enabled task against the current time.
    /// </summary>
    private void CheckTasks(object? state)
    {
        try
        {
            var now = DateTime.Now;
            var currentMinute = now.ToString("HH:mm");
            var today = (int)now.DayOfWeek; // 0=Sun, 1=Mon, ..., 6=Sat

            // Only run once per unique minute
            if (_lastRunMinute == currentMinute)
                return;
            _lastRunMinute = currentMinute;

            var tasks = DataStore.Load<List<ScheduledTask>>("tasks.json", _dataFolder) ?? new List<ScheduledTask>();
            if (tasks.Count == 0)
            {
                _logToUI("Info", "No scheduled tasks found", "program");
                return;
            }

            _logToUI("Info", $"Checking {tasks.Count} tasks at {currentMinute}", "program");

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                _logToUI("Info", $"Task #{i + 1}: enabled={task.Enabled}, time={task.Time}, recurrence={task.Recurrence}, onceDate={task.OnceDate}", "program");

                if (!task.Enabled)
                    continue;

                // Check time match
                if (!string.Equals(task.Time, currentMinute, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check recurrence
                if (!ShouldRunToday(task, now, today))
                {
                    _logToUI("Info", $"Task #{i + 1} skipped: not scheduled for today", "program");
                    continue;
                }

                _logToUI("Info", $"Scheduled task #{i + 1} matched: time={task.Time}, recurrence={task.Recurrence}", "program");

                // Fire and forget
                var index = i;
                Task.Run(() => ExecuteTask(index, task));
            }
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Scheduler check failed: {ex.Message}", "program");
        }
    }

    /// <summary>
    /// Determines whether a task should run today based on its recurrence settings.
    /// </summary>
    private bool ShouldRunToday(ScheduledTask task, DateTime now, int todayDow)
    {
        switch (task.Recurrence)
        {
            case "once":
                // Run once on the specified date (or today if no date set)
                if (string.IsNullOrWhiteSpace(task.OnceDate))
                {
                    _logToUI("Info", "Once task has no date set, will run at first matching time", "program");
                    return true; // no date = run at first matching time
                }
                if (DateTime.TryParse(task.OnceDate, out var onceDate))
                {
                    _logToUI("Info", $"Once task date: {onceDate:yyyy-MM-dd}, today: {now:yyyy-MM-dd}", "program");
                    return now.Date == onceDate.Date;
                }
                _logToUI("Warning", $"Failed to parse once date: {task.OnceDate}", "program");
                return false;

            case "weekly":
                // Run only on the specified days of week
                if (task.Days == null || task.Days.Count == 0)
                    return false;
                return task.Days.Contains(todayDow);

            case "daily":
            default:
                return true;
        }
    }

    /// <summary>
    /// Execute a single scheduled task by running its cm-script through the script runner.
    /// </summary>
    private async void ExecuteTask(int index, ScheduledTask task)
    {
        if (string.IsNullOrWhiteSpace(task.ScriptPath))
        {
            _logToUI("Warning", $"Scheduled task #{index + 1} has no script path - skipped", "program");
            return;
        }

        var scriptPath = Environment.ExpandEnvironmentVariables(task.ScriptPath);

        if (!File.Exists(scriptPath))
        {
            _logToUI("Error", $"Scheduled task #{index + 1} script not found: {scriptPath}", "program");
            return;
        }

        if (_scriptRunner == null)
        {
            _logToUI("Error", "Script engine not initialized", "program");
            return;
        }

        _logToUI("Info", $"Running scheduled task #{index + 1}: {scriptPath}", "program");

        try
        {
            var source = await File.ReadAllTextAsync(scriptPath);
            await _scriptRunner.Run(source);
            _logToUI("Info", $"Scheduled task #{index + 1} completed", "program");

            // If this was a "once" task, disable it after successful execution
            if (task.Recurrence == "once")
            {
                task.Enabled = false;
                // Reload tasks, update the disabled one, and save back
                var currentTasks = DataStore.Load<List<ScheduledTask>>("tasks.json", _dataFolder) ?? new List<ScheduledTask>();
                if (index < currentTasks.Count)
                {
                    currentTasks[index].Enabled = false;
                    DataStore.Save("tasks.json", currentTasks, _dataFolder);
                }
                _logToUI("Info", $"One-time task #{index + 1} disabled after execution", "program");

                // Notify frontend to refresh
                try
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        type = "schedulerTaskAutoDisabled",
                        index = index,
                        enabled = false
                    });
                    _webViewBridge?.PostMessage(json);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Scheduled task #{index + 1} failed: {ex.Message}", "program");
        }
    }

    /// <summary>
    /// Run a task immediately (called from frontend "Run Now" button).
    /// </summary>
    public async Task<(bool Success, string Message)> RunTaskNow(string scriptPath)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
            return (false, "No script path specified");

        var expanded = Environment.ExpandEnvironmentVariables(scriptPath);
        if (!File.Exists(expanded))
            return (false, $"File not found: {expanded}");

        if (_scriptRunner == null)
            return (false, "Script engine not initialized");

        try
        {
            _logToUI("Info", $"Manual run: {expanded}", "program");
            var source = await File.ReadAllTextAsync(expanded);
            await _scriptRunner.Run(source);
            return (true, "OK");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
