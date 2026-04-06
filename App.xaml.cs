namespace CursorCage;

public partial class App : System.Windows.Application
{
    public CursorCageApp Cage { get; private set; } = null!;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        Cage = new CursorCageApp();
        var window = new MainWindow(Cage);
        MainWindow = window;
        Cage.AttachMainWindow(window);
        window.Show();
        Cage.UpdateService.ScheduleStartupCheck();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        Cage.Dispose();
        base.OnExit(e);
    }
}
