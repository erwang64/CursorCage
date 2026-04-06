using CursorCage.Native;

namespace CursorCage.Models;

public sealed class AppSettings
{
    public HotkeyDefinition LockHotkey { get; set; } = HotkeyDefinition.Default;
    public bool AutoLockOnGameLaunch { get; set; }
    public LockTargetMode LockTargetMode { get; set; } = LockTargetMode.ActiveWindow;
    public string Language { get; set; } = "en";
}

public sealed class HotkeyDefinition
{
    public uint Modifiers { get; set; }
    public uint VirtualKey { get; set; }

    public static HotkeyDefinition Default { get; } = new()
    {
        Modifiers = HotkeyConstants.MOD_CONTROL | HotkeyConstants.MOD_NOREPEAT,
        VirtualKey = 0x4C // 'L'
    };
}
