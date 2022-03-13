using System;
using System.Diagnostics;
using System.Windows;
using DTLib.Filesystem;

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
            try
            {
                if (e.Args.Length > 0 && e.Args[0] == "updated")
                {
                    LoginWindow window = new();
                    window.ShowDialog();
                }
                else
                {
                    if (!File.Exists("updater.exe")) throw new Exception("file <updater.exe> not found");
                    Process.Start("cmd", "/c timeout 1 && start updater.exe");
                }
            }
            catch (Exception ex)
            { MessageBox.Show($"STARTUP ERROR:\n{ex.Message}\n{ex.StackTrace}"); }
            //Current.Shutdown();
        }
    }
}
