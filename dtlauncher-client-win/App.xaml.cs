using System;
using System.Diagnostics;
using System.Windows;

namespace dtlauncher_client_win
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string[] args = e.Args;
            try
            {
                if (args.Length > 0 && args[0] == "updated")
                {
                    LoginWindow window = new();
                    window.ShowDialog();
                }
                else
                {
                    Process.Start("cmd", "/c timeout 1 && start dtlauncher.exe");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"STARTUP ERROR:\n{ex.Message}\n{ex.StackTrace}");
            }
            //Current.Shutdown();
        }
    }
}
