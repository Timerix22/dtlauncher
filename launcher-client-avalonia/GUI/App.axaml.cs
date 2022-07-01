global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Media;
global using Avalonia.Media.Imaging;
global using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;

namespace Launcher.Client.Avalonia.GUI
{
    public partial class App : Application
    {
        public static SolidColorBrush MyDark, MySoftDark, MyWhite, MyGreen, MyOrange, MySelectionColor;
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            MyDark = (SolidColorBrush)Resources["MyDarkTr"];
            MySoftDark = (SolidColorBrush)Resources["MyGray"];
            MyWhite = (SolidColorBrush)Resources["MyWhite"];
            MyGreen = (SolidColorBrush)Resources["MyGreen"];
            MyOrange = (SolidColorBrush)Resources["MySelectionColor"];
            MySelectionColor = (SolidColorBrush)Resources["MySelectionColor"];
        }
        
        
       public override void OnFrameworkInitializationCompleted()
       {
           if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
           {
               CurrentLauncherWindow = new LauncherWindow();
               desktop.MainWindow = CurrentLauncherWindow;
               CurrentLauncherWindow.Show(); 
           }

           base.OnFrameworkInitializationCompleted();
       }
    }
}