using System.Windows.Threading;
using CursorCage.Events;
using CursorCage.Models;
using CursorCage.Native;

namespace CursorCage.Services;

public sealed class CursorManager
{
    private readonly IEventBus _eventBus;
    private readonly ScreenManager _screenManager;
    private readonly DispatcherTimer _refreshTimer;
    private string? _lockedScreenId;
    private RECT? _lastClipRect;
    private LockTargetMode _lockMode = LockTargetMode.ScreenUnderCursor;

    public CursorManager(IEventBus eventBus, WindowManager windowManager, ScreenManager screenManager)
    {
        _eventBus = eventBus;
        _screenManager = screenManager;
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
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
        LockToCurrentScreen();
    }

    public void LockToWindow(string windowId)
    {
        LockToCurrentScreen();
    }

    public void LockToScreen(string screenId)
    {
        var bounds = _screenManager.GetScreenBounds(screenId);
        if (bounds is not { } rect)
            return;
        ApplyClip(rect);
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
        _lockedScreenId = null;
        _lastClipRect = null;
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

        LockToCurrentScreen();
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
        if (_lockedScreenId is not null)
            rect = _screenManager.GetScreenBounds(_lockedScreenId);

        if (rect is { } r)
            ApplyClip(r);
        else if (_lastClipRect is { } last)
            ApplyClip(last);
        else
            Unlock();
    }

    private void LockToCurrentScreen()
    {
        if (!Win32.GetCursorPos(out var pt))
            return;
        var rect = _screenManager.GetBoundsForPoint(pt.X, pt.Y);
        if (rect is not { } r)
            return;

        var id = _screenManager.GetScreenIdContainingPoint(pt.X, pt.Y);
        if (id is null)
            return;

        ApplyClip(r);
        _lockedScreenId = id;
        var wasUnlocked = !IsLocked;
        IsLocked = true;
        _refreshTimer.Start();
        if (wasUnlocked)
            _eventBus.Publish(new LockStateChanged(true));
    }

    private void ApplyClip(RECT rect)
    {
        var r = rect;
        Win32.ClipCursorRect(ref r);
        _lastClipRect = r;
    }
}
