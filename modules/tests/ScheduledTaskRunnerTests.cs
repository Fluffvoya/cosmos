using app_bridge;
using app_scheduler;
using app_settings;

namespace tests;

/// <summary>
/// Unit tests for the app-scheduler module: ScheduledTaskRunner.
/// </summary>
public class ScheduledTaskRunnerTests : IDisposable
{
    private readonly List<string> _logs = new();

    private void LogToUI(string level, string message, string sender)
    {
        _logs.Add($"[{level}] {message}");
    }

    private SettingsManager CreateManagerWithTasks(params ScheduledTask[] tasks)
    {
        var manager = new SettingsManager();
        foreach (var task in tasks)
            manager.Current.ScheduledTasks.Add(task);
        return manager;
    }

    private class MockScriptRunner : IScriptRunner
    {
        public List<string> RunSources { get; } = new();
        public bool ShouldThrow { get; set; }

        public Task Run(string source)
        {
            if (ShouldThrow)
                throw new InvalidOperationException("Script execution failed");
            RunSources.Add(source);
            return Task.CompletedTask;
        }
    }

    private class MockWebViewBridge : IWebViewBridge
    {
        public List<string> PostedMessages { get; } = new();

        public void PostMessage(string json)
        {
            PostedMessages.Add(json);
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    // ── Constructor ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidParameters_DoesNotThrow()
    {
        var manager = CreateManagerWithTasks();
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        Assert.NotNull(runner);
    }

    [Fact]
    public void Constructor_WithOptionalParameters_DoesNotThrow()
    {
        var manager = CreateManagerWithTasks();
        var scriptRunner = new MockScriptRunner();
        var webViewBridge = new MockWebViewBridge();
        var runner = new ScheduledTaskRunner(manager, LogToUI, scriptRunner, webViewBridge);

        Assert.NotNull(runner);
    }

    // ── Start / Stop ────────────────────────────────────────────────

    [Fact]
    public void Start_LogsStartMessage()
    {
        var manager = CreateManagerWithTasks();
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        runner.Start();

        Assert.Contains(_logs, l => l.Contains("started"));
        runner.Stop();
    }

    [Fact]
    public void Start_LogsTaskCount()
    {
        var manager = CreateManagerWithTasks(
            new ScheduledTask { Time = "09:00" },
            new ScheduledTask { Time = "18:00" });
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        runner.Start();

        Assert.Contains(_logs, l => l.Contains("2 scheduled tasks"));
        runner.Stop();
    }

    [Fact]
    public void Stop_LogsStopMessage()
    {
        var manager = CreateManagerWithTasks();
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        runner.Start();
        runner.Stop();

        Assert.Contains(_logs, l => l.Contains("stopped"));
    }

    [Fact]
    public void Stop_WithoutStart_DoesNotThrow()
    {
        var manager = CreateManagerWithTasks();
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        // Stop without Start should not throw
        runner.Stop();
    }

    // ── Dispose ─────────────────────────────────────────────────────

    [Fact]
    public void Dispose_StopsRunner()
    {
        var manager = CreateManagerWithTasks();
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        runner.Start();
        runner.Dispose();

        // Should have stop log
        Assert.Contains(_logs, l => l.Contains("stopped"));
    }

    [Fact]
    public void Dispose_WithoutStart_DoesNotThrow()
    {
        var manager = CreateManagerWithTasks();
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        // Dispose without Start should not throw
        runner.Dispose();
    }

    // ── RunTaskNow ──────────────────────────────────────────────────

    [Fact]
    public async Task RunTaskNow_EmptyPath_ReturnsFailure()
    {
        var manager = CreateManagerWithTasks();
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        var (success, message) = await runner.RunTaskNow("");

        Assert.False(success);
        Assert.Contains("No script path", message);
    }

    [Fact]
    public async Task RunTaskNow_WhitespacePath_ReturnsFailure()
    {
        var manager = CreateManagerWithTasks();
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        var (success, message) = await runner.RunTaskNow("   ");

        Assert.False(success);
    }

    [Fact]
    public async Task RunTaskNow_NonExistentFile_ReturnsFailure()
    {
        var manager = CreateManagerWithTasks();
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        var (success, message) = await runner.RunTaskNow(@"C:\nonexistent\script.cms");

        Assert.False(success);
        Assert.Contains("not found", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunTaskNow_NoScriptRunner_ReturnsFailure()
    {
        var manager = CreateManagerWithTasks();
        // Create a temp file so the file-exists check passes
        var tempFile = Path.GetTempFileName();
        try
        {
            var runner = new ScheduledTaskRunner(manager, LogToUI, scriptRunner: null);

            var (success, message) = await runner.RunTaskNow(tempFile);

            Assert.False(success);
            Assert.Contains("not initialized", message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RunTaskNow_ValidScript_CallsScriptRunner()
    {
        var manager = CreateManagerWithTasks();
        var scriptRunner = new MockScriptRunner();
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "COSMOS noop");
            var runner = new ScheduledTaskRunner(manager, LogToUI, scriptRunner);

            var (success, message) = await runner.RunTaskNow(tempFile);

            Assert.True(success);
            Assert.Equal("OK", message);
            Assert.Single(scriptRunner.RunSources);
            Assert.Equal("COSMOS noop", scriptRunner.RunSources[0]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RunTaskNow_ScriptRunnerThrows_ReturnsFailure()
    {
        var manager = CreateManagerWithTasks();
        var scriptRunner = new MockScriptRunner { ShouldThrow = true };
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "COSMOS noop");
            var runner = new ScheduledTaskRunner(manager, LogToUI, scriptRunner);

            var (success, message) = await runner.RunTaskNow(tempFile);

            Assert.False(success);
            Assert.Contains("Script execution failed", message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RunTaskNow_ExpandsEnvironmentVariables()
    {
        var manager = CreateManagerWithTasks();
        var scriptRunner = new MockScriptRunner();

        // Use a path with %TEMP% environment variable
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "test");
            var envPath = tempFile.Replace(Path.GetTempPath(), "%TEMP%\\");

            var runner = new ScheduledTaskRunner(manager, LogToUI, scriptRunner);
            var (success, message) = await runner.RunTaskNow(envPath);

            Assert.True(success);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // ── Multiple Start/Stop cycles ──────────────────────────────────

    [Fact]
    public void Start_Stop_Start_DoesNotThrow()
    {
        var manager = CreateManagerWithTasks();
        var runner = new ScheduledTaskRunner(manager, LogToUI);

        runner.Start();
        runner.Stop();
        runner.Start();
        runner.Stop();
    }
}
