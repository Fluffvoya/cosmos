using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using app.Models;

namespace app.ViewModels;

/// <summary>
/// ViewModel for the main window. Manages the tab collection, settings, and commands.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private TabInfo? _selectedTab;
    private AppSettings _settings;

    public MainViewModel()
    {
        _settings = new AppSettings();

        tabs = new ObservableCollection<TabInfo>();

        // Commands
        openSettingsCommand = new RelayCommand(_ => openSettings());
        closeTabCommand = new RelayCommand(tab => closeTab(tab as TabInfo));
        moveTabCommand = new RelayCommand(param => executeMoveTab(param));
        aboutCommand = new RelayCommand(_ => showAbout());

        // Open a welcome tab by default.
        tabs.Add(new TabInfo { title = "Welcome", contentType = "Document" });
        selectedTab = tabs[0];
    }

    // ── Tab Management ───────────────────────────────────────────────

    public ObservableCollection<TabInfo> tabs { get; }

    public TabInfo? selectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    /// <summary>
    /// Opens the Settings tab, reusing an existing one if already present.
    /// </summary>
    public void openSettings()
    {
        var existing = tabs.FirstOrDefault(t => t.contentType == "Settings");
        if (existing != null)
        {
            selectedTab = existing;
            return;
        }

        var svm = new SettingsViewModel(_settings, applySettings);
        svm.closeRequested += (_, _) => closeTab(tabs.FirstOrDefault(t => t.contentType == "Settings"));

        var tab = new TabInfo
        {
            title = "Settings",
            contentType = "Settings",
            icon = "", // Segoe Fluent Icons gear
            tag = svm
        };

        tabs.Add(tab);
        selectedTab = tab;
    }

    /// <summary>
    /// Closes the specified tab. If it is currently selected, selects the nearest neighbour.
    /// </summary>
    public void closeTab(TabInfo? tab)
    {
        if (tab == null || !tabs.Contains(tab))
            return;

        int index = tabs.IndexOf(tab);
        tabs.Remove(tab);

        if (selectedTab == tab)
        {
            if (tabs.Count > 0)
            {
                int newIndex = Math.Min(index, tabs.Count - 1);
                selectedTab = tabs[newIndex];
            }
            else
            {
                selectedTab = null;
            }
        }
    }

    /// <summary>
    /// Moves a tab from one index to another (used by drag-and-drop).
    /// Parameter is a string "fromIndex,toIndex".
    /// </summary>
    private void executeMoveTab(object? param)
    {
        if (param is not string s)
            return;

        var parts = s.Split(',');
        if (parts.Length != 2)
            return;

        if (!int.TryParse(parts[0], out int from) || !int.TryParse(parts[1], out int to))
            return;

        if (from < 0 || from >= tabs.Count || to < 0 || to >= tabs.Count || from == to)
            return;

        tabs.Move(from, to);
    }

    // ── Settings ─────────────────────────────────────────────────────

    public AppSettings settings
    {
        get => _settings;
        set
        {
            if (SetProperty(ref _settings, value))
                OnPropertyChanged(nameof(tabStripPlacement));
        }
    }

    /// <summary>
    /// Convenience property for XAML binding to Dock.Top / Dock.Left / Dock.Right.
    /// </summary>
    public Dock tabStripPlacement => _settings.tabPosition switch
    {
        TabPosition.Top => Dock.Top,
        TabPosition.Left => Dock.Left,
        TabPosition.Right => Dock.Right,
        _ => Dock.Top
    };

    /// <summary>
    /// Called by SettingsViewModel when the user applies new settings.
    /// Also syncs the python path to the Script engine and updates
    /// the SettingsViewModel's baseline so hasChanges reflects the latest state.
    /// </summary>
    public void applySettings(AppSettings newSettings)
    {
        settings = newSettings;

        // Sync python path to Script engine.
        if (App.Script is { } script)
            script.python = newSettings.pythonPath;

        // Update SettingsViewModel so hasChanges compares against the just-applied state.
        var svm = tabs.FirstOrDefault(t => t.contentType == "Settings")?.tag as SettingsViewModel;
        svm?.updateOriginal(newSettings);
    }

    // ── Commands ─────────────────────────────────────────────────────

    public ICommand openSettingsCommand { get; }
    public ICommand closeTabCommand { get; }
    public ICommand moveTabCommand { get; }
    public ICommand aboutCommand { get; }

    // ── About ───────────────────────────────────────────────────────

    private void showAbout()
    {
        MessageBox.Show(
            "Cosmos Application\nVersion 1.0",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}

/// <summary>
/// Minimal ICommand implementation for WPF command binding.
/// </summary>
internal class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
