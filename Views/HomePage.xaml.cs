using System.Windows;
using System.Windows.Controls;
using CursorCage.Events;
using CursorCage.Services;

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
        StatusLabel.Text = locked ? TranslationManager.GetString("StrStatusLocked") : TranslationManager.GetString("StrStatusUnlocked");
        StatusLabel.Foreground = locked 
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)) 
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 160, 160));
        
        ModeLabel.Text = TranslationManager.GetString("StrTargetScreen");
        
        BtnLock.IsEnabled = !locked;
        BtnUnlock.IsEnabled = locked;
        
        BtnLock.Style = (Style)FindResource(locked ? typeof(System.Windows.Controls.Button) : "AccentButton");
        BtnUnlock.Style = (Style)FindResource(!locked ? typeof(System.Windows.Controls.Button) : "AccentButton");
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
