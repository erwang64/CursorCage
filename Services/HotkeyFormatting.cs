using System.Text;
using System.Windows.Input;
using CursorCage.Models;
using CursorCage.Native;

namespace CursorCage.Services;

public static class HotkeyFormatting
{
    private static readonly uint[] CommonVirtualKeys =
    [
        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D,
        0x4E, 0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A,
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
        0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B,
        0x20, 0x09, 0x0D, 0x1B, 0x2E, 0x24, 0x23, 0x21, 0x22, 0x25, 0x26, 0x27, 0x28
    ];

    public static string ToDisplayString(HotkeyDefinition hk)
    {
        var sb = new StringBuilder();
        if ((hk.Modifiers & HotkeyConstants.MOD_CONTROL) != 0)
            sb.Append("Ctrl+");
        if ((hk.Modifiers & HotkeyConstants.MOD_ALT) != 0)
            sb.Append("Alt+");
        if ((hk.Modifiers & HotkeyConstants.MOD_SHIFT) != 0)
            sb.Append("Shift+");
        if ((hk.Modifiers & HotkeyConstants.MOD_WIN) != 0)
            sb.Append("Win+");
        sb.Append(VkToLabel(hk.VirtualKey));
        return sb.ToString().TrimEnd('+');
    }

    public static IReadOnlyList<uint> GetCommonVirtualKeys() => CommonVirtualKeys;

    public static string VirtualKeyToLabel(uint vk) => VkToLabel(vk);

    public static HotkeyDefinition FromKeyGesture(Key key, ModifierKeys modifiers)
    {
        uint m = BuildModifiers(
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Alt),
            modifiers.HasFlag(ModifierKeys.Shift),
            modifiers.HasFlag(ModifierKeys.Windows));
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        return new HotkeyDefinition { Modifiers = m, VirtualKey = vk };
    }

    public static HotkeyDefinition FromModifierAndVk(bool ctrl, bool alt, bool shift, bool win, uint vk) =>
        new() { Modifiers = BuildModifiers(ctrl, alt, shift, win), VirtualKey = vk };

    public static uint BuildModifiers(bool ctrl, bool alt, bool shift, bool win)
    {
        uint m = HotkeyConstants.MOD_NOREPEAT;
        if (ctrl)
            m |= HotkeyConstants.MOD_CONTROL;
        if (alt)
            m |= HotkeyConstants.MOD_ALT;
        if (shift)
            m |= HotkeyConstants.MOD_SHIFT;
        if (win)
            m |= HotkeyConstants.MOD_WIN;
        return m;
    }

    private static string VkToLabel(uint vk)
    {
        if (vk >= 0x70 && vk <= 0x7B)
            return $"F{vk - 0x6F}";
        if (vk >= 0x30 && vk <= 0x39)
            return ((char)vk).ToString();
        if (vk >= 0x41 && vk <= 0x5A)
            return ((char)vk).ToString();
        return vk switch
        {
            0x20 => "Space",
            0x09 => "Tab",
            0x0D => "Enter",
            0x1B => "Escape",
            0x2E => "Delete",
            0x24 => "Home",
            0x23 => "End",
            0x21 => "PageUp",
            0x22 => "PageDown",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            _ => $"0x{vk:X}"
        };
    }
}
