using System.Text;
using System.Windows.Input;
using CursorCage.Models;
using CursorCage.Native;

namespace CursorCage.Services;

public static class HotkeyFormatting
{
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

    public static HotkeyDefinition FromKeyGesture(Key key, ModifierKeys modifiers)
    {
        uint m = HotkeyConstants.MOD_NOREPEAT;
        if (modifiers.HasFlag(ModifierKeys.Control))
            m |= HotkeyConstants.MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Alt))
            m |= HotkeyConstants.MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Shift))
            m |= HotkeyConstants.MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows))
            m |= HotkeyConstants.MOD_WIN;
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        return new HotkeyDefinition { Modifiers = m, VirtualKey = vk };
    }

    private static string VkToLabel(uint vk)
    {
        if (vk >= 0x30 && vk <= 0x39)
            return ((char)vk).ToString();
        if (vk >= 0x41 && vk <= 0x5A)
            return ((char)vk).ToString();
        return $"0x{vk:X}";
    }
}
