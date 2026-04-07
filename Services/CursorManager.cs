using CursorCage.Events;
using CursorCage.Models;
using CursorCage.Native;

namespace CursorCage.Services;

public sealed class CursorManager
{
    /// <summary>Marge intérieure (px) pour limiter les dépassements sur le moniteur voisin avec souris très rapide.</summary>
    private const int ClipInsetPx = 3;

    /// <summary>Période de la boucle de clip (ms). Plus bas = plus sûr en jeu, un peu plus de CPU.</summary>
    private const int ClipLoopPeriodMs = 1;

    private readonly IEventBus _eventBus;
    private readonly ScreenManager _screenManager;
    private readonly object _clipLoopLock = new();
    private CancellationTokenSource? _clipLoopCts;
    private Task? _clipLoopTask;
    private bool _highResTimerActive;
    private string? _lockedScreenId;
    private RECT? _lastClipRect;
    private LockTargetMode _lockMode = LockTargetMode.ScreenUnderCursor;

    public CursorManager(IEventBus eventBus, WindowManager windowManager, ScreenManager screenManager)
    {
        _eventBus = eventBus;
        _screenManager = screenManager;
        _eventBus.Subscribe<ToggleLockRequested>(_ => ToggleLock());
    }

    public bool IsLocked { get; private set; }

    public LockTargetMode LockMode
    {
        get => _lockMode;
        set => _lockMode = value;
    }

    public void LockToActiveWindow() => LockToCurrentScreen();

    public void LockToWindow(string windowId) => LockToCurrentScreen();

    public void LockToScreen(string screenId)
    {
        var bounds = _screenManager.GetScreenBounds(screenId);
        if (bounds is not { } rect)
            return;
        _lastClipRect = rect;
        _lockedScreenId = screenId;
        var wasUnlocked = !IsLocked;
        IsLocked = true;
        ApplyClipOnce();
        StartClipLoop();
        if (wasUnlocked)
            _eventBus.Publish(new LockStateChanged(true));
    }

    public void Unlock()
    {
        StopClipLoop();
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

        _lastClipRect = r;
        _lockedScreenId = id;
        var wasUnlocked = !IsLocked;
        IsLocked = true;
        ApplyClipOnce();
        StartClipLoop();
        if (wasUnlocked)
            _eventBus.Publish(new LockStateChanged(true));
    }

    private void StartClipLoop()
    {
        lock (_clipLoopLock)
        {
            StopClipLoopCore();
            _ = Win32.timeBeginPeriod(1);
            _highResTimerActive = true;
            _clipLoopCts = new CancellationTokenSource();
            var ct = _clipLoopCts.Token;
            _clipLoopTask = Task.Run(() => ClipLoop(ct), ct);
        }
    }

    private void StopClipLoop()
    {
        lock (_clipLoopLock)
            StopClipLoopCore();
    }

    private void StopClipLoopCore()
    {
        _clipLoopCts?.Cancel();
        try
        {
            _clipLoopTask?.Wait(500);
        }
        catch (AggregateException)
        {
            // tâche annulée
        }

        _clipLoopCts?.Dispose();
        _clipLoopCts = null;
        _clipLoopTask = null;
        if (_highResTimerActive)
        {
            _ = Win32.timeEndPeriod(1);
            _highResTimerActive = false;
        }
    }

    private void ClipLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!IsLocked)
                    break;

                RECT? full = null;
                if (_lockedScreenId is not null)
                    full = _screenManager.GetScreenBounds(_lockedScreenId);
                full ??= _lastClipRect;
                if (full is not { } f)
                    break;

                var clip = ToClipRect(f);
                Win32.ClipCursorRect(ref clip);

                if (Win32.GetCursorPos(out var pt) && !PointInRect(pt, clip))
                {
                    var clamped = ClampToRect(pt, clip);
                    Win32.SetCursorPos(clamped.X, clamped.Y);
                }
            }
            catch
            {
                // ignorer : ne pas faire tomber la boucle sur erreur transitoire
            }

            Thread.Sleep(ClipLoopPeriodMs);
        }
    }

    private void ApplyClipOnce()
    {
        if (_lastClipRect is not { } full)
            return;
        var clip = ToClipRect(full);
        Win32.ClipCursorRect(ref clip);
    }

    private static RECT ToClipRect(RECT full)
    {
        var m = ClipInsetPx;
        if (full.Width <= 2 * m + 2 || full.Height <= 2 * m + 2)
            m = 0;
        return new RECT
        {
            Left = full.Left + m,
            Top = full.Top + m,
            Right = full.Right - m,
            Bottom = full.Bottom - m
        };
    }

    /// <summary><paramref name="r"/> : <c>Right</c> / <c>Bottom</c> exclus (comme le hit-test Windows).</summary>
    private static bool PointInRect(Win32.POINT p, RECT r) =>
        p.X >= r.Left && p.X < r.Right && p.Y >= r.Top && p.Y < r.Bottom;

    private static Win32.POINT ClampToRect(Win32.POINT p, RECT r)
    {
        var maxX = Math.Max(r.Left, r.Right - 1);
        var maxY = Math.Max(r.Top, r.Bottom - 1);
        return new Win32.POINT
        {
            X = Math.Clamp(p.X, r.Left, maxX),
            Y = Math.Clamp(p.Y, r.Top, maxY)
        };
    }
}
