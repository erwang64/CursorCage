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
    private readonly object _stateLock = new();
    private CancellationTokenSource? _clipLoopCts;
    private Task? _clipLoopTask;
    private bool _highResTimerActive;
    private bool _isLocked;
    private string? _lockedScreenId;
    private RECT? _lastClipRect;
    private LockTargetMode _lockMode = LockTargetMode.ScreenUnderCursor;

    public CursorManager(IEventBus eventBus, WindowManager windowManager, ScreenManager screenManager)
    {
        _eventBus = eventBus;
        _screenManager = screenManager;
        _eventBus.Subscribe<ToggleLockRequested>(_ => ToggleLock());
    }

    public bool IsLocked
    {
        get
        {
            lock (_stateLock)
                return _isLocked;
        }
    }

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
        if (bounds is not { } rect || !IsUsableRect(rect))
            return;

        bool wasUnlocked;
        lock (_stateLock)
        {
            _lastClipRect = rect;
            _lockedScreenId = screenId;
            wasUnlocked = !_isLocked;
            _isLocked = true;
        }

        ApplyClipOnce();
        StartClipLoop();
        if (wasUnlocked)
            _eventBus.Publish(new LockStateChanged(true));
    }

    public void Unlock()
    {
        StopClipLoop();

        bool wasLocked;
        lock (_stateLock)
        {
            _lockedScreenId = null;
            _lastClipRect = null;
            wasLocked = _isLocked;
            _isLocked = false;
        }

        Win32.ClipCursorRelease(0);
        if (wasLocked)
            _eventBus.Publish(new LockStateChanged(false));
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
        if (rect is not { } r || !IsUsableRect(r))
            return;

        var id = _screenManager.GetScreenIdContainingPoint(pt.X, pt.Y);
        if (id is null)
            return;

        bool wasUnlocked;
        lock (_stateLock)
        {
            _lastClipRect = r;
            _lockedScreenId = id;
            wasUnlocked = !_isLocked;
            _isLocked = true;
        }

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
                bool isLocked;
                RECT? lastRect;
                lock (_stateLock)
                {
                    isLocked = _isLocked;
                    lastRect = _lastClipRect;
                }

                if (!isLocked)
                    break;

                // Important: on garde un rectangle fixe capturé à l'activation.
                // Recalculer le monitor en boucle peut produire des changements instables
                // en plein écran (menus/alt-state) et bloquer la souris au centre.
                if (lastRect is not { } f || !IsUsableRect(f))
                    break;

                var clip = ToClipRect(f);
                Win32.ClipCursorRect(ref clip);
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
        RECT? full;
        lock (_stateLock)
            full = _lastClipRect;

        if (full is not { } f || !IsUsableRect(f))
            return;

        var clip = ToClipRect(f);
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

    private static bool IsUsableRect(RECT r)
    {
        const int minSize = 64;
        return r.Width >= minSize && r.Height >= minSize;
    }
}
