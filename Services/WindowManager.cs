using System.Text;
using CursorCage.Models;
using CursorCage.Native;

namespace CursorCage.Services;

public sealed class WindowManager
{
    public WindowInfo? GetActiveWindow()
    {
        var hwnd = Win32.GetForegroundWindow();
        if (hwnd == 0)
            return null;
        if (!Win32.IsWindow(hwnd) || !Win32.IsWindowVisible(hwnd))
            return null;

        var len = Win32.GetWindowTextLength(hwnd);
        var sb = len > 0 ? new StringBuilder(len + 1) : new StringBuilder(256);
        Win32.GetWindowText(hwnd, sb, sb.Capacity);
        return new WindowInfo
        {
            Id = hwnd.ToString("X"),
            Handle = hwnd,
            Title = sb.ToString()
        };
    }

    public RECT? GetWindowBounds(string windowId)
    {
        if (!TryParseHandle(windowId, out var hwnd))
            return null;
        if (!Win32.GetWindowRect(hwnd, out var rect))
            return null;
        return rect;
    }

    public bool IsValidWindow(string windowId)
    {
        if (!TryParseHandle(windowId, out var hwnd))
            return false;
        return Win32.IsWindow(hwnd) && Win32.IsWindowVisible(hwnd);
    }

    private static bool TryParseHandle(string windowId, out nint hwnd)
    {
        hwnd = 0;
        if (string.IsNullOrWhiteSpace(windowId))
            return false;
        if (windowId.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            windowId = windowId[2..];
        try
        {
            hwnd = (nint)Convert.ToInt64(windowId, 16);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
