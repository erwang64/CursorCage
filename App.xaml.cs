using Microsoft.Win32;

namespace CursorCage;

public partial class App : System.Windows.Application
{
    public CursorCageApp Cage { get; private set; } = null!;

    /// <summary>Indique qu’une sortie volontaire est en cours — la fenêtre principale ne doit pas annuler <see cref="Shutdown"/>.</summary>
    public bool ShutdownRequested { get; private set; }

    public void RequestShutdown()
    {
        if (ShutdownRequested)
            return;
        ShutdownRequested = true;
        Shutdown();
    }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);
        SystemEvents.SessionEnding += OnSessionEnding;
        Cage = new CursorCageApp();
        var window = new MainWindow(Cage);
        MainWindow = window;
        Cage.AttachMainWindow(window);
        window.Show();
        Cage.UpdateService.ScheduleStartupCheck();
    }

    private void OnSessionEnding(object? sender, SessionEndingEventArgs args)
    {
        RequestShutdown();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        SystemEvents.SessionEnding -= OnSessionEnding;
        Cage.Dispose();
        base.OnExit(e);
    }
}
