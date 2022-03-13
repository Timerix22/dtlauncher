namespace launcher_client_win;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
            _Main(e.Args);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"STARTUP ERROR:\n{ex}"); 
            Shutdown();
        }
    }
}

