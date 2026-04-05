using CursorCage.Events;
using CursorCage.Models;
using CursorCage.Services;

namespace CursorCage;

public sealed class CursorCageApp : IDisposable
{
    public IEventBus EventBus { get; }
    public WindowManager WindowManager { get; }
    public ScreenManager ScreenManager { get; }
    public CursorManager CursorManager { get; }
    public SettingsManager SettingsManager { get; }
    public HotkeyManager HotkeyManager { get; }
    public UIManager UiManager { get; }

    private readonly Action<LockStateChanged> _onLockState;

    public CursorCageApp()
    {
        EventBus = new EventBus();
        WindowManager = new WindowManager();
        ScreenManager = new ScreenManager();
        SettingsManager = new SettingsManager();
        SettingsManager.LoadSettings();
        CursorManager = new CursorManager(EventBus, WindowManager, ScreenManager)
        {
            LockMode = LockTargetMode.ScreenUnderCursor
        };
        SettingsManager.Current.LockTargetMode = LockTargetMode.ScreenUnderCursor;
        HotkeyManager = new HotkeyManager(EventBus, SettingsManager);
        UiManager = new UIManager();
        _onLockState = e => UiManager.UpdateTrayIcon(e.IsLocked);
        EventBus.Subscribe(_onLockState);
    }

    public void AttachMainWindow(MainWindow window)
    {
        UiManager.InitializeTray(window);
    }

    public void Dispose()
    {
        HotkeyManager.Unregister();
        EventBus.Unsubscribe(_onLockState);
        UiManager.Dispose();
    }
}
