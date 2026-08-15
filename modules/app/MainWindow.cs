using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using app_bridge;
using app_password;
using app_scheduler;
using app_settings;
using bridge;
using script_func;

namespace app;

/// <summary>
/// Main application window hosting a WebView2 control.
/// Implements IServer so cm-script can call back into the UI.
/// Implements IWebViewBridge and IScriptRunner for decoupled module communication.
/// Frameless window with DWM-extended frame for native animations.
/// </summary>
public class MainWindow : Form, IServer, IWebViewBridge, IScriptRunner
{
    private WebView2 _webView = null!;
    private ServerBridge _serverBridge = null!;
    private cm_script.Script? _script;
    private ScheduledTaskRunner? _taskRunner;
    private readonly SettingsManager _settingsManager = new();
    private readonly AppPasswordManager _passwordManager = new();

    // ���� Window styles ����������������������������������������������������������������������������������������
    private const int WS_THICKFRAME  = 0x00040000;
    private const int WS_CAPTION     = 0x00C00000;
    private const int WS_MAXIMIZEBOX = 0x00010000;

    // ���� WndProc messages ����������������������������������������������������������������������������������
    private const int WM_NCCALCSIZE = 0x0083;
    private const int WM_NCHITTEST  = 0x0084;
    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    // ���� DWM ������������������������������������������������������������������������������������������������������������
    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(
        IntPtr hwnd, ref MARGINS pMarInset);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    // ���� For JS-initiated drag ������������������������������������������������������������������������
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_LBUTTONDBLCLK = 0x00A3;

    // Thickness of the invisible resize border around the window.
    // Must match the form's Padding so the resize area is outside the WebView2
    // and the form receives WM_NCHITTEST directly (bypassing Chromium's HWND).
    private const int ResizeBorderThickness = 8;

    /// <summary>
    /// Shared Script instance for running cm-script sources.
    /// </summary>
    public cm_script.Script? Script => _script;

    /// <summary>
    /// Singleton accessor for the MainWindow as IServer.
    /// </summary>
    public static MainWindow? Instance { get; private set; }

    /// <summary>
    /// Gets the settings manager instance.
    /// </summary>
    public SettingsManager SettingsManager => _settingsManager;

    /// <summary>
    /// Gets the password manager instance.
    /// </summary>
    public AppPasswordManager PasswordManager => _passwordManager;

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        _settingsManager.Load();
    }

    /// <summary>
    /// Set window styles during handle creation.
    /// WS_THICKFRAME - resize grips, DWM maximize/minimize animations.
    /// WS_CAPTION    - DWM animations, double-click-to-maximize.
    /// WS_MAXIMIZEBOX - enables maximize animation.
    /// The visible border from WS_CAPTION is removed by handling WM_NCCALCSIZE
    /// to collapse the non-client area to zero.
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.Style |= WS_THICKFRAME | WS_CAPTION | WS_MAXIMIZEBOX;
            return cp;
        }
    }

    private void InitializeComponent()
    {
        Text = "Cosmos";
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;

        // Set the form icon for the taskbar (ApplicationIcon only sets the .exe icon)
        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);

        // Scale initial window size relative to the working area for multi-resolution support
        var workArea = Screen.PrimaryScreen?.WorkingArea
                       ?? new Rectangle(0, 0, 1920, 1080);
        Width = (int)(workArea.Width * 0.65);
        Height = (int)(workArea.Height * 0.82);

        // Padding creates a gap between the form edges and the WebView2 control.
        // The form receives WM_NCHITTEST directly in this gap (the Chromium HWND
        // inside WebView2 doesn't cover it), enabling edge resize without relying
        // on HTTRANSPARENT forwarding. BackColor blends with the page background.
        Padding = new Padding(ResizeBorderThickness);
        BackColor = Color.FromArgb(236, 238, 241);

        // Create WebView2 control (Dock=Fill respects the form's Padding)
        _webView = new ResizeAwareWebView
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(_webView);

        Load += async (_, _) => await InitializeAsync();
        FormClosing += (_, _) => Cleanup();
    }

    /// <summary>
    /// Called after the native window handle is created.
    /// Extends the DWM frame into the client area so that
    /// maximize/minimize animations are native and smooth.
    /// Using cyTopHeight = -1 is the Chromium trick: it tells DWM
    /// to use the legacy rendering path while still extending the
    /// frame, giving us rounded corners and smooth animations.
    /// </summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // cyTopHeight = -1 is the Chromium trick for DWM frame extension.
        // It provides rounded corners and native window animations.
        // Edge resize is handled by the form's Padding (not DWM margins),
        // so the DWM doesn't interfere with resize borders.
        var margins = new MARGINS
        {
            cxLeftWidth = 0,
            cxRightWidth = 0,
            cyTopHeight = -1,
            cyBottomHeight = 0
        };
        DwmExtendFrameIntoClientArea(Handle, ref margins);
    }

    /// <summary>
    /// Handle WM_NCCALCSIZE to collapse the non-client area to zero.
    /// This removes the visible caption while keeping its effects
    /// (animations, drag, double-click-to-maximize).
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCCALCSIZE && m.WParam != IntPtr.Zero)
        {
            // Collapse non-client area to zero
            m.Result = IntPtr.Zero;
            return;
        }

        if (m.Msg == WM_NCHITTEST)
        {
            var point = new Point(
                (int)(m.LParam.ToInt64() & 0xFFFF),
                (int)((m.LParam.ToInt64() >> 16) & 0xFFFF));

            var clientPoint = PointToClient(point);

            // Resize borders
            if (WindowState != FormWindowState.Maximized)
            {
                if (clientPoint.X < ResizeBorderThickness && clientPoint.Y < ResizeBorderThickness)
                { m.Result = (IntPtr)HTTOPLEFT; return; }
                if (clientPoint.X < ResizeBorderThickness && clientPoint.Y > Height - ResizeBorderThickness)
                { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
                if (clientPoint.X > Width - ResizeBorderThickness && clientPoint.Y < ResizeBorderThickness)
                { m.Result = (IntPtr)HTTOPRIGHT; return; }
                if (clientPoint.X > Width - ResizeBorderThickness && clientPoint.Y > Height - ResizeBorderThickness)
                { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
                if (clientPoint.X < ResizeBorderThickness)
                { m.Result = (IntPtr)HTLEFT; return; }
                if (clientPoint.X > Width - ResizeBorderThickness)
                { m.Result = (IntPtr)HTRIGHT; return; }
                if (clientPoint.Y < ResizeBorderThickness)
                { m.Result = (IntPtr)HTTOP; return; }
                if (clientPoint.Y > Height - ResizeBorderThickness)
                { m.Result = (IntPtr)HTBOTTOM; return; }
            }

            m.Result = (IntPtr)HTCLIENT;
            return;
        }

        base.WndProc(ref m);
    }

    private async Task InitializeAsync()
    {
        // Enable file:// access from web context (needed for ringtone audio playback)
        var envOptions = new CoreWebView2EnvironmentOptions(
            additionalBrowserArguments: "--allow-file-access-from-files");
        var env = await CoreWebView2Environment.CreateAsync(null, null, envOptions);
        await _webView.EnsureCoreWebView2Async(env);

        // Register bridge for the virtual host
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "app.local",
            Path.Combine(AppContext.BaseDirectory, "wwwroot"),
            CoreWebView2HostResourceAccessKind.Allow);

        _webView.Source = new Uri("https://app.local/index.html");

        // Create bridge
        _serverBridge = new ServerBridge(_webView, this, _passwordManager, (level, message, sender) =>
        {
            try
            {
                _serverBridge.SendInternalLog(level, message, sender);
            }
            catch { }
        });

        // Wire up web message handler
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        // Send settings, tasks, script output, and start config to frontend
        _webView.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            var tasks = DataStore.Load<List<ScheduledTask>>("tasks.json") ?? new List<ScheduledTask>();
            var scriptOutput = DataStore.Load<List<ScriptOutputEntry>>("script-output.json");
            var startConfig = DataStore.Load<StartConfig>("start-config.json") ?? new StartConfig();

            var settingsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "settingsLoaded",
                settings = _settingsManager.Current,
                scheduledTasks = tasks,
                scriptOutput = scriptOutput,
                startConfig = startConfig
            });
            _webView.CoreWebView2.PostWebMessageAsJson(settingsJson);
        };

        // Initialize cm-script engine
        var pythonPath = _settingsManager.Current.PythonPath;
        if (!string.IsNullOrWhiteSpace(pythonPath))
        {
            pythonPath = Environment.ExpandEnvironmentVariables(pythonPath);
        }
        _script = new cm_script.Script(this, pythonPath);

        // Keep Script's Python path in sync when settings change
        _settingsManager.SettingsChanged += (newSettings) =>
        {
            if (_script != null)
            {
                var newPath = newSettings.PythonPath;
                if (!string.IsNullOrWhiteSpace(newPath))
                {
                    newPath = Environment.ExpandEnvironmentVariables(newPath);
                }
                _script.python = newPath;
            }
        };

        // Register built-in script functions
        _script.AddFunction("MessageBox", ScriptFunctions.MessageBox);
        _script.AddFunction("Log", ScriptFunctions.Log);
        _script.AddFunction("Warning", ScriptFunctions.Warning);
        _script.AddFunction("Error", ScriptFunctions.Error);
        _script.AddFunction("GetUserName", ScriptFunctions.GetUserName);
        _script.AddFunction("MessageBar", ScriptFunctions.MessageBar);
        _script.AddFunction("PlayRingtone", ScriptFunctions.PlayRingtone);
        _script.AddFunction("OpenRegisteredApp", ScriptFunctions.OpenRegisteredApp);

        // Start the scheduled task runner
        _taskRunner = new ScheduledTaskRunner(_settingsManager, (level, message, sender) =>
        {
            try
            {
                _serverBridge.SendInternalLog(level, message, sender);
            }
            catch { }
        }, scriptRunner: this, webViewBridge: this);
        _taskRunner.Start();

        // Pass the runner to the bridge
        _serverBridge.SetTaskRunner(_taskRunner);

        // Initialize the script playground runner
        var scriptRunner = new app_script.ScriptRunner(this, this, (level, message, sender) =>
        {
            try
            {
                _serverBridge.SendInternalLog(level, message, sender);
            }
            catch { }
        });
        _serverBridge.SetScriptRunner(scriptRunner);

        // Run startup script if configured
        var startupScript = _settingsManager.Current.StartupScriptPath;
        if (!string.IsNullOrWhiteSpace(startupScript))
        {
            startupScript = Environment.ExpandEnvironmentVariables(startupScript);

            if (startupScript.EndsWith(".cms", StringComparison.OrdinalIgnoreCase))
            {
                // cm-script file - run through the script engine
                RunStartupScript(startupScript);
            }
            else
            {
                _serverBridge.SendInternalLog("Warning", $"Startup script must be a .cms file. Ignoring: {startupScript}", "program");
            }
        }
    }

    /// <summary>
    /// Run a cm-script file as startup script.
    /// </summary>
    private void RunStartupScript(string scriptPath)
    {
        Task.Run(async () =>
        {
            try
            {
                if (_script != null)
                {
                    var source = await File.ReadAllTextAsync(scriptPath);
                    _serverBridge.SendInternalLog("Info", $"Running startup cm-script: {scriptPath}", "program");
                    await _script.Run(source);
                    _serverBridge.SendInternalLog("Info", "Startup cm-script completed", "program");
                }
                else
                {
                    _serverBridge.SendInternalLog("Error", "Script engine not initialized", "program");
                }
            }
            catch (Exception ex)
            {
                _serverBridge.SendInternalLog("Error", $"Failed to run startup cm-script: {ex.Message}", "program");
            }
        });
    }


    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Plain string messages from window controls / drag handler
        var text = e.TryGetWebMessageAsString();
        switch (text)
        {
            case "window:minimize":
                WindowState = FormWindowState.Minimized;
                return;
            case "window:maximize":
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
                return;
            case "window:close":
                Close();
                return;
            case "window:drag":
                // JS detected mousedown on the drag area - start native drag
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                return;
            case "window:dblclick-maximize":
                // JS detected double-click on the drag area - toggle maximize
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
                return;
        }

        // Structured JSON messages from other frontend features
        var message = e.WebMessageAsJson;
        _serverBridge.HandleFrontendMessage(message);
    }

    private void Cleanup()
    {
        _taskRunner?.Dispose();
        _webView?.Dispose();
    }

    /// <summary>
    /// IServer.Execute - receives requests from cm-script and returns a response.
    /// </summary>
    public string Execute(string requests)
    {
        return _serverBridge.Execute(requests);
    }

    /// <summary>
    /// IWebViewBridge.PostMessage - post a JSON message to the WebView2 frontend.
    /// </summary>
    public void PostMessage(string json)
    {
        try
        {
            if (_webView.InvokeRequired)
            {
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(json));
            }
            else
            {
                _webView.CoreWebView2.PostWebMessageAsJson(json);
            }
        }
        catch { }
    }

    /// <summary>
    /// IScriptRunner.Run - run a cm-script source string.
    /// </summary>
    public async Task Run(string source)
    {
        if (_script == null)
            throw new InvalidOperationException("Script engine not initialized");

        await _script.Run(source);
    }
}

/// <summary>
/// WebView2 subclass that is transparent to mouse input at window edges,
/// allowing the parent form's WM_NCHITTEST to handle resize borders.
/// This acts as a secondary defense — the primary resize mechanism is the
/// form's Padding, which keeps the WebView2 inset so the form receives
/// WM_NCHITTEST directly at the edges without needing HTTRANSPARENT.
/// </summary>
internal sealed class ResizeAwareWebView : WebView2
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;

    private const int ResizeBorder = 8;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST && Parent is Control parent)
        {
            // Get cursor position in screen coordinates
            var screenPos = Cursor.Position;
            var localPos = parent.PointToClient(screenPos);

            // If cursor is within the resize border of the parent's edges,
            // return HTTRANSPARENT so the parent form handles the hit-test.
            if (localPos.X < ResizeBorder || localPos.X > parent.Width - ResizeBorder ||
                localPos.Y < ResizeBorder || localPos.Y > parent.Height - ResizeBorder)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }
        }

        base.WndProc(ref m);
    }
}


