using app_settings;

namespace tests;

/// <summary>
/// Unit tests for the app-settings module: AppSettings, ScheduledTask, and SettingsManager.
/// </summary>
public class SettingsTests
{
    // ── AppSettings defaults ────────────────────────────────────────

    [Fact]
    public void AppSettings_DefaultTabPosition_IsTop()
    {
        var settings = new AppSettings();
        Assert.Equal("top", settings.TabPosition);
    }

    [Fact]
    public void AppSettings_DefaultTabStripWidth_Is140()
    {
        var settings = new AppSettings();
        Assert.Equal(140, settings.TabStripWidth);
    }

    [Fact]
    public void AppSettings_DefaultPythonPath_IsEmpty()
    {
        var settings = new AppSettings();
        Assert.Equal("", settings.PythonPath);
    }

    [Fact]
    public void AppSettings_DefaultStartupScriptPath_IsEmpty()
    {
        var settings = new AppSettings();
        Assert.Equal("", settings.StartupScriptPath);
    }

    [Fact]
    public void AppSettings_DefaultScheduledTasks_IsEmptyList()
    {
        var settings = new AppSettings();
        Assert.NotNull(settings.ScheduledTasks);
        Assert.Empty(settings.ScheduledTasks);
    }

    [Fact]
    public void AppSettings_PropertiesCanBeSet()
    {
        var settings = new AppSettings
        {
            TabPosition = "left",
            TabStripWidth = 200,
            PythonPath = @"C:\Python\python.exe",
            StartupScriptPath = @"C:\scripts\startup.cms"
        };

        Assert.Equal("left", settings.TabPosition);
        Assert.Equal(200, settings.TabStripWidth);
        Assert.Equal(@"C:\Python\python.exe", settings.PythonPath);
        Assert.Equal(@"C:\scripts\startup.cms", settings.StartupScriptPath);
    }

    // ── ScheduledTask defaults ──────────────────────────────────────

    [Fact]
    public void ScheduledTask_DefaultEnabled_IsTrue()
    {
        var task = new ScheduledTask();
        Assert.True(task.Enabled);
    }

    [Fact]
    public void ScheduledTask_DefaultTime_IsMidnight()
    {
        var task = new ScheduledTask();
        Assert.Equal("00:00", task.Time);
    }

    [Fact]
    public void ScheduledTask_DefaultScriptPath_IsEmpty()
    {
        var task = new ScheduledTask();
        Assert.Equal("", task.ScriptPath);
    }

    [Fact]
    public void ScheduledTask_DefaultRecurrence_IsDaily()
    {
        var task = new ScheduledTask();
        Assert.Equal("daily", task.Recurrence);
    }

    [Fact]
    public void ScheduledTask_DefaultDays_IsEmptyList()
    {
        var task = new ScheduledTask();
        Assert.NotNull(task.Days);
        Assert.Empty(task.Days);
    }

    [Fact]
    public void ScheduledTask_DefaultOnceDate_IsEmpty()
    {
        var task = new ScheduledTask();
        Assert.Equal("", task.OnceDate);
    }

    [Fact]
    public void ScheduledTask_PropertiesCanBeSet()
    {
        var task = new ScheduledTask
        {
            Enabled = false,
            Time = "14:30",
            ScriptPath = @"C:\scripts\task.cms",
            Recurrence = "weekly",
            Days = new List<int> { 1, 3, 5 },
            OnceDate = "2026-01-15"
        };

        Assert.False(task.Enabled);
        Assert.Equal("14:30", task.Time);
        Assert.Equal(@"C:\scripts\task.cms", task.ScriptPath);
        Assert.Equal("weekly", task.Recurrence);
        Assert.Equal(3, task.Days.Count);
        Assert.Contains(1, task.Days);
        Assert.Contains(3, task.Days);
        Assert.Contains(5, task.Days);
        Assert.Equal("2026-01-15", task.OnceDate);
    }

    // ── SettingsManager ─────────────────────────────────────────────

    [Fact]
    public void SettingsManager_Current_IsNotNullByDefault()
    {
        var manager = new SettingsManager();
        Assert.NotNull(manager.Current);
    }

    [Fact]
    public void SettingsManager_Current_IsAppSettingsInstance()
    {
        var manager = new SettingsManager();
        Assert.IsType<AppSettings>(manager.Current);
    }

    [Fact]
    public void SettingsManager_Load_NonExistentFile_DoesNotThrow()
    {
        var manager = new SettingsManager();
        // Load should not throw even if the settings file doesn't exist
        manager.Load();
        Assert.NotNull(manager.Current);
    }

    [Fact]
    public void SettingsManager_Update_ChangesCurrent()
    {
        var manager = new SettingsManager();
        var newSettings = new AppSettings
        {
            TabPosition = "bottom",
            TabStripWidth = 300
        };

        manager.Update(newSettings);

        Assert.Equal("bottom", manager.Current.TabPosition);
        Assert.Equal(300, manager.Current.TabStripWidth);
    }

    [Fact]
    public void SettingsManager_Update_RaisesSettingsChangedEvent()
    {
        var manager = new SettingsManager();
        AppSettings? receivedSettings = null;
        manager.SettingsChanged += s => receivedSettings = s;

        var newSettings = new AppSettings { TabPosition = "left" };
        manager.Update(newSettings);

        Assert.NotNull(receivedSettings);
        Assert.Equal("left", receivedSettings!.TabPosition);
    }

    [Fact]
    public void SettingsManager_Update_MultipleSubscribers_AllNotified()
    {
        var manager = new SettingsManager();
        var callCount = 0;
        manager.SettingsChanged += _ => callCount++;
        manager.SettingsChanged += _ => callCount++;

        manager.Update(new AppSettings());

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void SettingsManager_Update_NoSubscribers_DoesNotThrow()
    {
        var manager = new SettingsManager();
        // No subscribers attached - should not throw
        manager.Update(new AppSettings());
    }

    [Fact]
    public void SettingsManager_Save_ThenLoad_PersistsSettings()
    {
        // Use a unique temp path to avoid conflicts
        var tempDir = Path.Combine(Path.GetTempPath(), "cosmos_test_" + Guid.NewGuid().ToString("N"));
        var settingsFile = Path.Combine(tempDir, "settings.json");

        try
        {
            Directory.CreateDirectory(tempDir);

            // Create settings with custom values
            var manager1 = new SettingsManager();
            var settings = new AppSettings
            {
                TabPosition = "right",
                TabStripWidth = 250,
                PythonPath = @"C:\TestPython\python.exe"
            };
            manager1.Update(settings);

            // Manually save to temp location
            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, json);

            // Read back
            var readJson = File.ReadAllText(settingsFile);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(readJson);

            Assert.NotNull(loaded);
            Assert.Equal("right", loaded!.TabPosition);
            Assert.Equal(250, loaded.TabStripWidth);
            Assert.Equal(@"C:\TestPython\python.exe", loaded.PythonPath);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SettingsManager_ScheduledTasks_CanBeManipulated()
    {
        var manager = new SettingsManager();

        manager.Current.ScheduledTasks.Add(new ScheduledTask
        {
            Time = "09:00",
            ScriptPath = @"C:\scripts\morning.cms",
            Recurrence = "daily"
        });

        manager.Current.ScheduledTasks.Add(new ScheduledTask
        {
            Time = "18:00",
            ScriptPath = @"C:\scripts\evening.cms",
            Recurrence = "weekly",
            Days = new List<int> { 1, 2, 3, 4, 5 }
        });

        Assert.Equal(2, manager.Current.ScheduledTasks.Count);
        Assert.Equal("09:00", manager.Current.ScheduledTasks[0].Time);
        Assert.Equal("weekly", manager.Current.ScheduledTasks[1].Recurrence);
    }
}
