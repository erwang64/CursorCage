using System.Runtime.InteropServices;
using CursorCage.Models;
using CursorCage.Native;

namespace CursorCage.Services;

public sealed class ScreenManager
{
    public IReadOnlyList<ScreenInfo> GetScreens()
    {
        var list = new List<ScreenInfo>();
        MonitorEnumState.Current = list;
        try
        {
            Win32.EnumDisplayMonitors(0, 0, MonitorCallback, 0);
        }
        finally
        {
            MonitorEnumState.Current = null;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var s = list[i];
            if (string.IsNullOrEmpty(s.Id))
                list[i] = new ScreenInfo { Id = $"Monitor{i}", Bounds = s.Bounds };
        }

        return list;
    }

    private static bool MonitorCallback(nint hMonitor, nint _, ref RECT __, nint ___)
    {
        var target = MonitorEnumState.Current!;
        var mi = new Win32.MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<Win32.MONITORINFOEX>() };
        if (Win32.GetMonitorInfo(hMonitor, ref mi))
        {
            target.Add(new ScreenInfo
            {
                Id = mi.szDevice,
                Bounds = mi.rcMonitor
            });
        }

        return true;
    }

    private static class MonitorEnumState
    {
        [ThreadStatic]
        public static List<ScreenInfo>? Current;
    }

    public RECT? GetScreenBounds(string screenId)
    {
        foreach (var s in GetScreens())
        {
            if (string.Equals(s.Id, screenId, StringComparison.OrdinalIgnoreCase))
                return s.Bounds;
        }

        return null;
    }

    public string? GetScreenIdContainingPoint(int x, int y)
    {
        foreach (var s in GetScreens())
        {
            var b = s.Bounds;
            if (x >= b.Left && x < b.Right && y >= b.Top && y < b.Bottom)
                return s.Id;
        }

        return null;
    }

    public RECT? GetBoundsForPoint(int x, int y)
    {
        var id = GetScreenIdContainingPoint(x, y);
        return id is null ? null : GetScreenBounds(id);
    }
}
