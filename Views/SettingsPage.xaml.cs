using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CursorCage.Models;
using CursorCage.Native;
using CursorCage.Services;

namespace CursorCage.Views;

public partial class SettingsPage : Page
{
    private readonly CursorCageApp _app;
    private HotkeyDefinition _pendingHotkey;
    private readonly List<HotkeyOption> _keyOptions = [];
    private bool _loaded;
    private bool _suppressAutoSave;
    private DispatcherTimer? _saveDebounce;

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
        SettingsPathText.Text = "Fichier : " + _app.SettingsManager.SettingsFilePath;
        
        var lang = _app.SettingsManager.Current.Language;
        foreach (ComboBoxItem item in LanguageCombo.Items)
        {
            if (item.Tag is string itemLang && itemLang == lang)
            {
                LanguageCombo.SelectedItem = item;
                break;
            }
        }
        
        HotkeyStatus.Visibility = Visibility.Collapsed;
        _loaded = true;
    }

    private void HotkeyDisplay_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        if (e.Key == Key.Escape)
        {
            _pendingHotkey = Clone(HotkeyDefinition.Default);
            HotkeyDisplay.Text = HotkeyFormatting.ToDisplayString(_pendingHotkey);
            SyncManualControlsFromPending();
            HotkeyStatus.Visibility = Visibility.Collapsed;
            PersistSettingsNow();
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
        PersistSettingsNow();
    }

    private void ModifierOrKey_Changed(object sender, RoutedEventArgs e) => ScheduleAutoSave();

    private void KeyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => ScheduleAutoSave();

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || _suppressAutoSave) return;
        if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string lang)
        {
            _app.SettingsManager.Current.Language = lang;
            TranslationManager.ApplyLanguage(lang);
            ScheduleAutoSave();
        }
    }

    private void ResetHotkey_Click(object sender, RoutedEventArgs e)
    {
        _pendingHotkey = Clone(HotkeyDefinition.Default);
        HotkeyDisplay.Text = HotkeyFormatting.ToDisplayString(_pendingHotkey);
        SyncManualControlsFromPending();
        HotkeyStatus.Visibility = Visibility.Collapsed;
        PersistSettingsNow();
    }

    private void ScheduleAutoSave()
    {
        if (!_loaded || _suppressAutoSave)
            return;

        _saveDebounce ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        _saveDebounce.Stop();
        _saveDebounce.Tick -= SaveDebounce_OnTick;
        _saveDebounce.Tick += SaveDebounce_OnTick;
        _saveDebounce.Start();
    }

    private void SaveDebounce_OnTick(object? sender, EventArgs e)
    {
        if (_saveDebounce is not null)
            _saveDebounce.Stop();
        PersistSettingsNow();
    }

    private void MergePendingFromUi()
    {
        if (TryGetSelectedVirtualKey(out var vkSave))
        {
            var hasMods = CtrlCheck.IsChecked == true || AltCheck.IsChecked == true ||
                          ShiftCheck.IsChecked == true || WinCheck.IsChecked == true;
            if (hasMods)
            {
                _pendingHotkey = HotkeyFormatting.FromModifierAndVk(
                    CtrlCheck.IsChecked == true,
                    AltCheck.IsChecked == true,
                    ShiftCheck.IsChecked == true,
                    WinCheck.IsChecked == true,
                    vkSave);
                HotkeyDisplay.Text = HotkeyFormatting.ToDisplayString(_pendingHotkey);
            }
        }
    }

    private void PersistSettingsNow()
    {
        if (!_loaded || _suppressAutoSave)
            return;

        MergePendingFromUi();

        if (!IsHotkeyValid(_pendingHotkey))
            return;

        _app.SettingsManager.Current.LockHotkey = Clone(_pendingHotkey);
        _app.SettingsManager.Current.LockTargetMode = LockTargetMode.ScreenUnderCursor;
        _app.SettingsManager.Current.AutoLockOnGameLaunch = AutoGameCheck.IsChecked == true;

        if (!_app.SettingsManager.TrySaveSettings(out var err))
        {
            SetStatus("Échec de sauvegarde : " + err, false);
            return;
        }

        _app.CursorManager.LockMode = LockTargetMode.ScreenUnderCursor;

        if (!_app.HotkeyManager.RegisterFromSettings())
        {
            SetStatus(
                "Combinaison enregistrée sur le disque, mais Windows refuse le raccourci global (déjà pris). Essayez une autre touche.",
                false);
            return;
        }

        SetStatus("Sauvegarde automatique OK.", true);
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
        _suppressAutoSave = true;
        try
        {
            CtrlCheck.IsChecked = (_pendingHotkey.Modifiers & HotkeyConstants.MOD_CONTROL) != 0;
            AltCheck.IsChecked = (_pendingHotkey.Modifiers & HotkeyConstants.MOD_ALT) != 0;
            ShiftCheck.IsChecked = (_pendingHotkey.Modifiers & HotkeyConstants.MOD_SHIFT) != 0;
            WinCheck.IsChecked = (_pendingHotkey.Modifiers & HotkeyConstants.MOD_WIN) != 0;
            KeyCombo.SelectedValue = _pendingHotkey.VirtualKey;
        }
        finally
        {
            _suppressAutoSave = false;
        }
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
