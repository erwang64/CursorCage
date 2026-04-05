using CursorCage.Events;
using CursorCage.Models;
using CursorCage.Native;

namespace CursorCage.Services;

public sealed class HotkeyManager
{
    public const int HotKeyId = 0xC0DE;

    private readonly IEventBus _eventBus;
    private readonly SettingsManager _settings;
    private nint _hwnd;
    private bool _registered;

    public HotkeyManager(IEventBus eventBus, SettingsManager settings)
    {
        _eventBus = eventBus;
        _settings = settings;
    }

    public void AttachWindow(nint hwnd)
    {
        if (_hwnd == hwnd)
            return;
        if (_hwnd != 0 && _registered)
            Unregister();
        _hwnd = hwnd;
        RegisterFromSettings();
    }

    public bool RegisterFromSettings()
    {
        if (_hwnd == 0)
            return false;
        if (_registered)
            Unregister();
        var hk = _settings.GetLockHotkey();
        var mods = hk.Modifiers | HotkeyConstants.MOD_NOREPEAT;
        _registered = Win32.RegisterHotKey(_hwnd, HotKeyId, mods, hk.VirtualKey);
        return _registered;
    }

    public void Unregister()
    {
        if (_hwnd != 0 && _registered)
        {
            Win32.UnregisterHotKey(_hwnd, HotKeyId);
            _registered = false;
        }
    }

    public void OnWmHotkey(int id)
    {
        if (id != HotKeyId)
            return;
        _eventBus.Publish(new ToggleLockRequested());
    }

    public bool IsRegistered => _registered;
}
