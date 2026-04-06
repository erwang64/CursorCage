using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace CursorCage.Services;

public sealed class UIManager : IDisposable
{
    private NotifyIcon? _tray;
    private Window? _mainWindow;
    private Action? _checkUpdatesRequest;
    private string? _pendingUpdateOpenUrl;
    private bool _lastTrayLocked;

    public void InitializeTray(Window mainWindow, Action? checkUpdatesRequest = null)
    {
        _mainWindow = mainWindow;
        _checkUpdatesRequest = checkUpdatesRequest;
        _tray = new NotifyIcon
        {
            Visible = true,
            Text = TranslationManager.GetString("StrAppTitle"),
            ContextMenuStrip = BuildMenu()
        };
        _tray.DoubleClick += (_, _) => ShowMainWindow();
        UpdateTrayIcon(false);
    }

    /// <summary>Reconstruit le menu contextuel après un changement de langue.</summary>
    public void RebuildTrayMenu()
    {
        if (_tray is null)
            return;
        var old = _tray.ContextMenuStrip;
        _tray.ContextMenuStrip = BuildMenu();
        old?.Dispose();
        UpdateTrayIcon(_lastTrayLocked);
    }

    public void ShowUpdateAvailableBalloon(string version, string openUrl)
    {
        if (_tray is null)
            return;
        _pendingUpdateOpenUrl = openUrl;
        _tray.BalloonTipClicked -= Tray_UpdateBalloonClicked;
        _tray.BalloonTipClicked += Tray_UpdateBalloonClicked;
        var title = TranslationManager.GetString("StrUpdateBalloonTitle");
        var text = string.Format(TranslationManager.GetString("StrUpdateBalloonTextFmt"), version);
        _tray.ShowBalloonTip(12000, title, text, ToolTipIcon.Info);
    }

    private void Tray_UpdateBalloonClicked(object? sender, EventArgs e)
    {
        if (_tray is null)
            return;
        _tray.BalloonTipClicked -= Tray_UpdateBalloonClicked;
        var url = _pendingUpdateOpenUrl;
        _pendingUpdateOpenUrl = null;
        if (string.IsNullOrEmpty(url))
            return;
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(TranslationManager.GetString("StrOpenIcon"), null, (_, _) => ShowMainWindow());
        menu.Items.Add(TranslationManager.GetString("StrSettingsMenu"), null, (_, _) =>
        {
            ShowMainWindow();
            if (_mainWindow is MainWindow mw)
                mw.NavigateToSettings();
        });
        if (_checkUpdatesRequest is not null)
        {
            menu.Items.Add(TranslationManager.GetString("StrCheckUpdatesMenu"), null, (_, _) => _checkUpdatesRequest());
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(TranslationManager.GetString("StrQuit"), null, (_, _) =>
        {
            if (Application.Current is global::CursorCage.App app)
                app.RequestShutdown();
            else
                Application.Current.Shutdown();
        });
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
        _lastTrayLocked = isLocked;
        try 
        {
            var appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application;
            _tray.Icon = isLocked ? SystemIcons.Shield : appIcon;
        } 
        catch 
        {
            _tray.Icon = isLocked ? SystemIcons.Shield : SystemIcons.Application;
        }
        _tray.Text = isLocked ? TranslationManager.GetString("StrTrayLocked") : TranslationManager.GetString("StrTrayUnlocked");
    }

    public void ShowNotification(string message)
    {
        if (_tray is null)
            return;
        _tray.ShowBalloonTip(3000, TranslationManager.GetString("StrAppTitle"), message, ToolTipIcon.Info);
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
            _tray.BalloonTipClicked -= Tray_UpdateBalloonClicked;
            _tray.Visible = false;
            var menu = _tray.ContextMenuStrip;
            _tray.ContextMenuStrip = null;
            menu?.Dispose();
            _tray.Dispose();
            _tray = null;
        }
    }
}
