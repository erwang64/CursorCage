using System.Windows.Threading;
using CursorCage.Events;
using CursorCage.Models;
using CursorCage.Native;

namespace CursorCage.Services;

public sealed class CursorManager
{
    private readonly IEventBus _eventBus;
    private readonly WindowManager _windowManager;
    private readonly ScreenManager _screenManager;
    private readonly DispatcherTimer _refreshTimer;
    private string? _lockedWindowId;
    private string? _lockedScreenId;
    private LockTargetMode _lockMode = LockTargetMode.ActiveWindow;

    public CursorManager(IEventBus eventBus, WindowManager windowManager, ScreenManager screenManager)
    {
        _eventBus = eventBus;
        _windowManager = windowManager;
        _screenManager = screenManager;
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _refreshTimer.Tick += (_, _) => RefreshClip();
        _eventBus.Subscribe<ToggleLockRequested>(_ => ToggleLock());
    }

    public bool IsLocked { get; private set; }

    public LockTargetMode LockMode
    {
        get => _lockMode;
        set => _lockMode = value;
    }

    public void LockToActiveWindow()
    {
        var w = _windowManager.GetActiveWindow();
        if (w is null)
            return;
        LockToWindow(w.Id);
    }

    public void LockToWindow(string windowId)
    {
        if (!_windowManager.IsValidWindow(windowId))
            return;
        var bounds = _windowManager.GetWindowBounds(windowId);
        if (bounds is not { } rect)
            return;
        ApplyClip(rect);
        _lockedWindowId = windowId;
        _lockedScreenId = null;
        var wasUnlocked = !IsLocked;
        IsLocked = true;
        _refreshTimer.Start();
        if (wasUnlocked)
            _eventBus.Publish(new LockStateChanged(true));
    }

    public void LockToScreen(string screenId)
    {
        var bounds = _screenManager.GetScreenBounds(screenId);
        if (bounds is not { } rect)
            return;
        ApplyClip(rect);
        _lockedWindowId = null;
        _lockedScreenId = screenId;
        var wasUnlocked = !IsLocked;
        IsLocked = true;
        _refreshTimer.Start();
        if (wasUnlocked)
            _eventBus.Publish(new LockStateChanged(true));
    }

    public void Unlock()
    {
        _refreshTimer.Stop();
        _lockedWindowId = null;
        _lockedScreenId = null;
        Win32.ClipCursorRelease(0);
        if (IsLocked)
        {
            IsLocked = false;
            _eventBus.Publish(new LockStateChanged(false));
        }
    }

    public void LockUnderCurrentMode()
    {
        if (IsLocked)
            return;

        if (_lockMode == LockTargetMode.ScreenUnderCursor)
        {
            if (!Win32.GetCursorPos(out var pt))
                return;
            var rect = _screenManager.GetBoundsForPoint(pt.X, pt.Y);
            if (rect is not { } r)
                return;
            var id = _screenManager.GetScreenIdContainingPoint(pt.X, pt.Y);
            ApplyClip(r);
            _lockedWindowId = null;
            _lockedScreenId = id;
            IsLocked = true;
            _refreshTimer.Start();
            _eventBus.Publish(new LockStateChanged(true));
            return;
        }

        LockToActiveWindow();
    }

    public void ToggleLock()
    {
        if (IsLocked)
            Unlock();
        else
            LockUnderCurrentMode();
    }

    private void RefreshClip()
    {
        if (!IsLocked)
            return;

        RECT? rect = null;
        if (_lockedWindowId is not null && _windowManager.IsValidWindow(_lockedWindowId))
            rect = _windowManager.GetWindowBounds(_lockedWindowId);
        else if (_lockedScreenId is not null)
            rect = _screenManager.GetScreenBounds(_lockedScreenId);
        else if (_lockMode == LockTargetMode.ActiveWindow && _lockedWindowId is not null)
            rect = _windowManager.GetWindowBounds(_lockedWindowId);

        if (rect is { } r)
            ApplyClip(r);
        else
            Unlock();
    }

    private static void ApplyClip(RECT rect)
    {
        var r = rect;
        Win32.ClipCursorRect(ref r);
    }
}
