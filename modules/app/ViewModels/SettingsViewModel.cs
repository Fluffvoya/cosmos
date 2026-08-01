using System.IO;
using System.Windows;
using System.Windows.Input;
using app.Models;

namespace app.ViewModels;

/// <summary>
/// ViewModel for the settings tab. Edits a copy of settings; changes are applied
/// or saved via the injected callback.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private AppSettings _original;
    private readonly Action<AppSettings> _apply;

    private TabPosition _tabPosition;
    private string _pythonPath;
    private bool _isValidPythonPath = true;
    private string _selectedCategory;

    /// <summary>
    /// Fires when the settings tab should be closed (user clicked Save or Cancel).
    /// </summary>
    public event EventHandler? closeRequested;

    public SettingsViewModel(AppSettings original, Action<AppSettings> apply)
    {
        _original = original;
        _apply = apply;

        // Work on a copy so edits are not immediately visible.
        _tabPosition = original.tabPosition;
        _pythonPath = original.pythonPath;
        _selectedCategory = "General";

        applyCommand = new RelayCommand(_ => executeApply(), _ => hasChanges);
        saveCommand = new RelayCommand(_ => executeSave(), _ => hasChanges);
        cancelCommand = new RelayCommand(_ => executeCancel());
        browsePythonPathCommand = new RelayCommand(_ => browsePythonPath());
    }

    // ── Category Navigation ──────────────────────────────────────────

    /// <summary>
    /// Available setting categories for the sidebar.
    /// </summary>
    public string[] settingsCategories => ["General", "Scripting"];

    /// <summary>
    /// Currently selected category in the sidebar.
    /// </summary>
    public string selectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    // ── General Settings ─────────────────────────────────────────────

    /// <summary>
    /// Where tabs are positioned (Top, Left, Right).
    /// </summary>
    public TabPosition tabPosition
    {
        get => _tabPosition;
        set
        {
            if (SetProperty(ref _tabPosition, value))
                OnPropertyChanged(nameof(hasChanges));
        }
    }

    /// <summary>
    /// All available tab positions for the combo box.
    /// </summary>
    public TabPosition[] tabPositions => Enum.GetValues<TabPosition>();

    // ── Scripting Settings ───────────────────────────────────────────

    /// <summary>
    /// Path to the Python interpreter executable.
    /// </summary>
    public string pythonPath
    {
        get => _pythonPath;
        set
        {
            if (SetProperty(ref _pythonPath, value))
            {
                validatePythonPath();
                OnPropertyChanged(nameof(hasChanges));
            }
        }
    }

    /// <summary>
    /// Whether the current pythonPath points to an existing file.
    /// </summary>
    public bool isValidPythonPath
    {
        get => _isValidPythonPath;
        private set => SetProperty(ref _isValidPythonPath, value);
    }

    // ── Change Tracking ──────────────────────────────────────────────

    /// <summary>
    /// True when the working copy differs from the original settings.
    /// </summary>
    public bool hasChanges =>
        _tabPosition != _original.tabPosition ||
        _pythonPath != _original.pythonPath;

    // ── Commands ─────────────────────────────────────────────────────

    public ICommand applyCommand { get; }
    public ICommand saveCommand { get; }
    public ICommand cancelCommand { get; }
    public ICommand browsePythonPathCommand { get; }

    // ── Private Helpers ──────────────────────────────────────────────

    private void validatePythonPath()
    {
        // Empty is valid (no python configured); non-empty must point to an existing file.
        isValidPythonPath = string.IsNullOrWhiteSpace(_pythonPath) || File.Exists(_pythonPath);
    }

    /// <summary>
    /// Checks whether the configured python path exists on disk.
    /// If it does not, shows a warning dialog and returns false.
    /// Empty path is allowed (python not configured).
    /// </summary>
    private bool checkPythonPath()
    {
        if (string.IsNullOrWhiteSpace(_pythonPath))
            return true;

        if (File.Exists(_pythonPath))
            return true;

        MessageBox.Show(
            $"Python interpreter not found:\n{_pythonPath}",
            "Invalid Python Path",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private AppSettings buildSettings() => new()
    {
        tabPosition = _tabPosition,
        pythonPath = _pythonPath
    };

    private void executeApply()
    {
        if (!checkPythonPath())
            return;

        _apply(buildSettings());
    }

    /// <summary>
    /// Called after settings are applied so that hasChanges compares
    /// against the latest applied state, not the initial state.
    /// </summary>
    public void updateOriginal(AppSettings applied)
    {
        _original = applied;
        OnPropertyChanged(nameof(hasChanges));
    }

    private void executeSave()
    {
        if (!checkPythonPath())
            return;

        _apply(buildSettings());
        closeRequested?.Invoke(this, EventArgs.Empty);
    }

    private void executeCancel()
    {
        closeRequested?.Invoke(this, EventArgs.Empty);
    }

    private void browsePythonPath()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Python Interpreter",
            Filter = "Python Executable (*.exe)|*.exe|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
            pythonPath = dialog.FileName;
    }
}
