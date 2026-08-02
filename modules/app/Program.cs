using System;
using System.Windows.Forms;

namespace app;

/// <summary>
/// Application entry point. Creates the main window and starts the message loop.
/// </summary>
internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        using var mainWindow = new MainWindow();
        Application.Run(mainWindow);
    }
}
