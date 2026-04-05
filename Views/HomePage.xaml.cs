using System.Windows;
using System.Windows.Controls;
using CursorCage.Events;

namespace CursorCage.Views;

public partial class HomePage : Page
{
    private readonly CursorCageApp _app;
    private readonly Action<LockStateChanged> _onLock;

    public HomePage(CursorCageApp app)
    {
        _app = app;
        InitializeComponent();
        _onLock = _ => Dispatcher.Invoke(RefreshUi);
        _app.EventBus.Subscribe(_onLock);
        Unloaded += (_, _) => _app.EventBus.Unsubscribe(_onLock);
        Loaded += (_, _) => RefreshUi();
    }

    private void RefreshUi()
    {
        var locked = _app.CursorManager.IsLocked;
        StatusLabel.Text = locked ? "État : verrouillé" : "État : déverrouillé";
        ModeLabel.Text = "Cible : écran courant (autres écrans bloqués)";
        BtnLock.IsEnabled = !locked;
        BtnUnlock.IsEnabled = locked;
    }

    private void BtnLock_Click(object sender, RoutedEventArgs e)
    {
        _app.CursorManager.LockUnderCurrentMode();
        RefreshUi();
    }

    private void BtnUnlock_Click(object sender, RoutedEventArgs e)
    {
        _app.CursorManager.Unlock();
        RefreshUi();
    }

    private void BtnToggle_Click(object sender, RoutedEventArgs e)
    {
        _app.CursorManager.ToggleLock();
        RefreshUi();
    }
}
