using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CursorCage.Native;
using CursorCage.Models;
using CursorCage.Services;

namespace CursorCage.Views;

public partial class SettingsPage : Page
{
    private readonly CursorCageApp _app;
    private HotkeyDefinition _pendingHotkey;
    private readonly List<HotkeyOption> _keyOptions = [];

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
        BuildKeyOptions();
        HotkeyDisplay.Text = HotkeyFormatting.ToDisplayString(_pendingHotkey);
        SyncManualControlsFromPending();
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
        SyncManualControlsFromPending();
        HotkeyStatus.Visibility = Visibility.Collapsed;
    }

    private void ApplyManualHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedVirtualKey(out var vk))
        {
            SetStatus("Sélectionnez une touche dans la liste.", false);
            return;
        }

        var hasModifiers = CtrlCheck.IsChecked == true ||
                           AltCheck.IsChecked == true ||
                           ShiftCheck.IsChecked == true ||
                           WinCheck.IsChecked == true;
        if (!hasModifiers)
        {
            SetStatus("Cochez au moins Ctrl, Alt, Maj ou Win.", false);
            return;
        }

        _pendingHotkey = HotkeyFormatting.FromModifierAndVk(
            CtrlCheck.IsChecked == true,
            AltCheck.IsChecked == true,
            ShiftCheck.IsChecked == true,
            WinCheck.IsChecked == true,
            vk);

        HotkeyDisplay.Text = HotkeyFormatting.ToDisplayString(_pendingHotkey);
        SetStatus("Combinaison prête. Cliquez sur Enregistrer pour appliquer.", true);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!IsHotkeyValid(_pendingHotkey))
        {
            SetStatus("Raccourci invalide. Choisissez au moins un modificateur (Ctrl/Alt/Maj/Win) + une touche.", false);
            return;
        }

        _app.SettingsManager.Current.LockHotkey = Clone(_pendingHotkey);
        _app.SettingsManager.Current.LockTargetMode = LockTargetMode.ScreenUnderCursor;
        _app.SettingsManager.Current.AutoLockOnGameLaunch = AutoGameCheck.IsChecked == true;
        _app.SettingsManager.SaveSettings();
        _app.CursorManager.LockMode = LockTargetMode.ScreenUnderCursor;

        if (!_app.HotkeyManager.RegisterFromSettings())
        {
            SetStatus(
                "Impossible d'enregistrer ce raccourci (déjà pris par une autre application ou invalide). Essayez une autre combinaison.",
                false);
        }
        else
        {
            SetStatus("Paramètres enregistrés.", true);
        }
    }

    private void ResetHotkey_Click(object sender, RoutedEventArgs e)
    {
        _pendingHotkey = Clone(HotkeyDefinition.Default);
        HotkeyDisplay.Text = HotkeyFormatting.ToDisplayString(_pendingHotkey);
        SyncManualControlsFromPending();
        HotkeyStatus.Visibility = Visibility.Collapsed;
    }

    private void BuildKeyOptions()
    {
        if (_keyOptions.Count > 0)
            return;

        foreach (var vk in HotkeyFormatting.GetCommonVirtualKeys())
            _keyOptions.Add(new HotkeyOption(vk, HotkeyFormatting.VirtualKeyToLabel(vk)));

        KeyCombo.ItemsSource = _keyOptions;
    }

    private void SyncManualControlsFromPending()
    {
        CtrlCheck.IsChecked = (_pendingHotkey.Modifiers & HotkeyConstants.MOD_CONTROL) != 0;
        AltCheck.IsChecked = (_pendingHotkey.Modifiers & HotkeyConstants.MOD_ALT) != 0;
        ShiftCheck.IsChecked = (_pendingHotkey.Modifiers & HotkeyConstants.MOD_SHIFT) != 0;
        WinCheck.IsChecked = (_pendingHotkey.Modifiers & HotkeyConstants.MOD_WIN) != 0;
        KeyCombo.SelectedValue = _pendingHotkey.VirtualKey;
    }

    private static bool IsHotkeyValid(HotkeyDefinition hotkey)
    {
        var hasMod = (hotkey.Modifiers & (HotkeyConstants.MOD_CONTROL | HotkeyConstants.MOD_ALT |
                                          HotkeyConstants.MOD_SHIFT | HotkeyConstants.MOD_WIN)) != 0;
        return hasMod && hotkey.VirtualKey != 0;
    }

    private bool TryGetSelectedVirtualKey(out uint vk)
    {
        vk = 0;
        if (KeyCombo.SelectedValue is uint u)
        {
            vk = u;
            return true;
        }

        if (KeyCombo.SelectedValue is int i && i > 0)
        {
            vk = (uint)i;
            return true;
        }

        return false;
    }

    private void SetStatus(string message, bool success)
    {
        HotkeyStatus.Text = message;
        HotkeyStatus.Foreground = success
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1B, 0x5E, 0x20))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));
        HotkeyStatus.Visibility = Visibility.Visible;
    }

    private sealed class HotkeyOption(uint virtualKey, string label)
    {
        public uint VirtualKey { get; } = virtualKey;
        public string Label { get; } = label;
    }
}
