using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace CursorCage.Services;

public sealed class UIManager : IDisposable
{
    private NotifyIcon? _tray;
    private Window? _mainWindow;

    public void InitializeTray(Window mainWindow)
    {
        _mainWindow = mainWindow;
        _tray = new NotifyIcon
        {
            Visible = true,
            Text = "CursorCage",
            ContextMenuStrip = BuildMenu()
        };
        _tray.DoubleClick += (_, _) => ShowMainWindow();
        UpdateTrayIcon(false);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Ouvrir", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Paramètres", null, (_, _) =>
        {
            ShowMainWindow();
            if (_mainWindow is MainWindow mw)
                mw.NavigateToSettings();
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => Application.Current.Shutdown());
        return menu;
    }

    public void ShowSettings()
    {
        ShowMainWindow();
        if (_mainWindow is MainWindow mw)
            mw.NavigateToSettings();
    }

    public void UpdateTrayIcon(bool isLocked)
    {
        if (_tray is null)
            return;
        try 
        {
            var appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application;
            _tray.Icon = isLocked ? SystemIcons.Shield : appIcon;
        } 
        catch 
        {
            _tray.Icon = isLocked ? SystemIcons.Shield : SystemIcons.Application;
        }
        _tray.Text = isLocked ? "CursorCage — VERROUILLÉ" : "CursorCage — Déverrouillé";
    }

    public void ShowNotification(string message)
    {
        if (_tray is null)
            return;
        _tray.ShowBalloonTip(3000, "CursorCage", message, ToolTipIcon.Info);
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
            return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void Dispose()
    {
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }
    }
}
