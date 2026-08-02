using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using bridge;
using script_func;

namespace app;

/// <summary>
/// Main application window hosting a WebView2 control.
/// Implements IServer so cm-script can call back into the UI.
/// Frameless window with DWM-extended frame for native animations.
/// </summary>
public class MainWindow : Form, IServer
{
    private WebView2 _webView = null!;
    private ServerBridge _serverBridge = null!;
    private cm_script.Script? _script;

    // ── Window styles ────────────────────────────────────────────
    private const int WS_THICKFRAME  = 0x00040000;
    private const int WS_CAPTION     = 0x00C00000;
    private const int WS_MAXIMIZEBOX = 0x00010000;

    // ── WndProc messages ─────────────────────────────────────────
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

    // ── DWM ──────────────────────────────────────────────────────
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

    // ── For JS-initiated drag ────────────────────────────────────
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_LBUTTONDBLCLK = 0x00A3;

    private const int ResizeBorderThickness = 8;

    /// <summary>
    /// Shared Script instance for running cm-script sources.
    /// </summary>
    public cm_script.Script? Script => _script;

    /// <summary>
    /// Singleton accessor for the MainWindow as IServer.
    /// </summary>
    public static MainWindow? Instance { get; private set; }

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
    }

    /// <summary>
    /// Set window styles during handle creation.
    /// WS_THICKFRAME — resize grips, DWM maximize/minimize animations.
    /// WS_CAPTION    — DWM animations, double-click-to-maximize.
    /// WS_MAXIMIZEBOX — enables maximize animation.
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
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        // Create WebView2 control
        _webView = new WebView2
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
    /// frame, giving us invisible non-client area with animations.
    /// </summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        var margins = new MARGINS { cxLeftWidth = 0, cxRightWidth = 0, cyTopHeight = -1, cyBottomHeight = 0 };
        DwmExtendFrameIntoClientArea(Handle, ref margins);
    }

    /// <summary>
    /// Handle WM_NCCALCSIZE to collapse the non-client area (removes visible border)
    /// and WM_NCHITTEST to provide resize grips at window edges.
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        // Collapse non-client area — removes the visible border from WS_CAPTION
        // while keeping WS_CAPTION in the style for DWM animations.
        if (m.Msg == WM_NCCALCSIZE && m.WParam != IntPtr.Zero)
        {
            m.Result = IntPtr.Zero;
            return;
        }

        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);
            if (m.Result.ToInt32() == HTCLIENT)
            {
                var screenPos = Cursor.Position;
                var clientPos = PointToClient(screenPos);
                int w = ClientSize.Width;
                int h = ClientSize.Height;
                int t = ResizeBorderThickness;

                bool left = clientPos.X < t;
                bool right = clientPos.X >= w - t;
                bool top = clientPos.Y < t;
                bool bottom = clientPos.Y >= h - t;

                if (top && left) m.Result = (IntPtr)HTTOPLEFT;
                else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
                else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (left) m.Result = (IntPtr)HTLEFT;
                else if (right) m.Result = (IntPtr)HTRIGHT;
                else if (top) m.Result = (IntPtr)HTTOP;
                else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                // Menu bar area stays HTCLIENT — JS handles drag
            }
            return;
        }
        base.WndProc(ref m);
    }

    private async Task InitializeAsync()
    {
        var userDataFolder = Path.Combine(
            Path.GetTempPath(), "cosmos-app");
        var env = await CoreWebView2Environment.CreateAsync(
            null, userDataFolder);

        await _webView.EnsureCoreWebView2Async(env);

        var wwwrootPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "app.local", wwwrootPath,
            CoreWebView2HostResourceAccessKind.Allow);

        _serverBridge = new ServerBridge(_webView, this, (level, msg) =>
        {
            // Forward internal logs from ServerBridge to the frontend Log panel.
            _serverBridge.SendInternalLog(level, msg);
        });
        _script = new cm_script.Script(this, string.Empty);

        // Register script functions that wrap Client interfaces
        _script.AddFunction("ShowWindow", ScriptFunctions.ShowWindow);
        _script.AddFunction("Log", ScriptFunctions.Log);
        _script.AddFunction("GetUserName", ScriptFunctions.GetUserName);

        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        // Notify frontend of initial window state
        _webView.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            NotifyWindowState();
        };

        // Notify frontend when window state changes (maximize/restore)
        Resize += (_, _) => NotifyWindowState();

        _webView.CoreWebView2.Navigate("https://app.local/index.html");
    }

    /// <summary>
    /// Send current window state to the frontend so it can
    /// update the maximize/restore button icon.
    /// </summary>
    private void NotifyWindowState()
    {
        if (_webView?.CoreWebView2 == null) return;
        bool maximized = WindowState == FormWindowState.Maximized;
        _webView.CoreWebView2.PostWebMessageAsJson(
            $"{{\"type\":\"windowStateChanged\",\"maximized\":{maximized.ToString().ToLower()}}}");
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
                // JS detected mousedown on the drag area — start native drag
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                return;
            case "window:dblclick-maximize":
                // JS detected double-click on the drag area — toggle maximize
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
        _webView?.Dispose();
    }

    /// <summary>
    /// IServer.Execute — receives requests from cm-script and returns a response.
    /// </summary>
    public string Execute(string requests)
    {
        return _serverBridge.Execute(requests);
    }
}
