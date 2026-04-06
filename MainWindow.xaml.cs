using System.Windows;
using System.Windows.Interop;
using CursorCage.Native;
using CursorCage.Services;
using CursorCage.Views;

namespace CursorCage;

public partial class MainWindow : Window
{
    private readonly CursorCageApp _app;

    public MainWindow(CursorCageApp app)
    {
        _app = app;
        InitializeComponent();
        SidebarVersionText.Text = "v" + AppInfo.DisplayVersion;
        NavFrame.Navigate(new HomePage(_app));
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
    }

    public void NavigateToHome() => NavFrame.Navigate(new HomePage(_app));

    public void NavigateToSettings() => NavFrame.Navigate(new SettingsPage(_app));

    private void BtnHome_Click(object sender, RoutedEventArgs e) => NavigateToHome();

    private void BtnSettings_Click(object sender, RoutedEventArgs e) => NavigateToSettings();

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (System.Windows.Application.Current is App app && app.ShutdownRequested)
            return;
        e.Cancel = true;
        Hide();
    }

    /// <summary>Si la fenêtre est réellement fermée sans sortie volontaire (cas anormal), on arrête le processus pour éviter un fantôme sans icône.</summary>
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (System.Windows.Application.Current is App app && !app.ShutdownRequested)
            app.RequestShutdown();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        if (hwndSource is null)
            return;
        _app.HotkeyManager.AttachWindow(hwndSource.Handle);
        if (!_app.HotkeyManager.RegisterFromSettings())
            _app.UiManager.ShowNotification(TranslationManager.GetString("StrHotkeyRegisterFailed"));
        hwndSource.AddHook(WndProc);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == Win32.WM_HOTKEY && (int)wParam == HotkeyManager.HotKeyId)
        {
            _app.HotkeyManager.OnWmHotkey((int)wParam);
            handled = true;
        }

        return nint.Zero;
    }
}
