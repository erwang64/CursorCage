using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CursorCage.Models;
using CursorCage.Services;

namespace CursorCage.Views;

public partial class SettingsPage : Page
{
    private readonly CursorCageApp _app;
    private HotkeyDefinition _pendingHotkey;

    public SettingsPage(CursorCageApp app)
    {
        _app = app;
        _pendingHotkey = Clone(_app.SettingsManager.GetLockHotkey());
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    private static HotkeyDefinition Clone(HotkeyDefinition h) =>
        new() { Modifiers = h.Modifiers, VirtualKey = h.VirtualKey };

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        HotkeyDisplay.Text = HotkeyFormatting.ToDisplayString(_pendingHotkey);
        ModeCombo.SelectedIndex =
            _app.SettingsManager.Current.LockTargetMode == LockTargetMode.ActiveWindow ? 0 : 1;
        AutoGameCheck.IsChecked = _app.SettingsManager.Current.AutoLockOnGameLaunch;
        HotkeyStatus.Visibility = Visibility.Collapsed;
    }

    private void HotkeyDisplay_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        if (e.Key == Key.Escape)
        {
            _pendingHotkey = Clone(HotkeyDefinition.Default);
            HotkeyDisplay.Text = HotkeyFormatting.ToDisplayString(_pendingHotkey);
            HotkeyStatus.Visibility = Visibility.Collapsed;
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var mods = Keyboard.Modifiers;
        if (mods == ModifierKeys.None)
        {
            HotkeyStatus.Text = "Utilisez au moins Ctrl, Alt, Maj ou Win avec une touche.";
            HotkeyStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));
            HotkeyStatus.Visibility = Visibility.Visible;
            return;
        }

        _pendingHotkey = HotkeyFormatting.FromKeyGesture(key, mods);
        HotkeyDisplay.Text = HotkeyFormatting.ToDisplayString(_pendingHotkey);
        HotkeyStatus.Visibility = Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _app.SettingsManager.Current.LockHotkey = Clone(_pendingHotkey);
        _app.SettingsManager.Current.LockTargetMode = ModeCombo.SelectedIndex == 0
            ? LockTargetMode.ActiveWindow
            : LockTargetMode.ScreenUnderCursor;
        _app.SettingsManager.Current.AutoLockOnGameLaunch = AutoGameCheck.IsChecked == true;
        _app.SettingsManager.SaveSettings();
        _app.CursorManager.LockMode = _app.SettingsManager.Current.LockTargetMode;

        if (!_app.HotkeyManager.RegisterFromSettings())
        {
            HotkeyStatus.Text =
                "Impossible d'enregistrer ce raccourci (déjà pris par une autre application ou invalide). Essayez une autre combinaison.";
            HotkeyStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));
            HotkeyStatus.Visibility = Visibility.Visible;
        }
        else
        {
            HotkeyStatus.Text = "Paramètres enregistrés.";
            HotkeyStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1B, 0x5E, 0x20));
            HotkeyStatus.Visibility = Visibility.Visible;
        }
    }

    private void ResetHotkey_Click(object sender, RoutedEventArgs e)
    {
        _pendingHotkey = Clone(HotkeyDefinition.Default);
        HotkeyDisplay.Text = HotkeyFormatting.ToDisplayString(_pendingHotkey);
        HotkeyStatus.Visibility = Visibility.Collapsed;
    }
}
