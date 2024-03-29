﻿using System.Windows.Media;

namespace Launcher.Client.WPF.GUI;

public partial class App : Application
{
    public static SolidColorBrush MyDark,MySoftDark, MyWhite, MyGreen, MyOrange, MySelectionColor;
    
    
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
            MyDark = (SolidColorBrush)Resources["MyDarkTr"];
            MySoftDark = (SolidColorBrush)Resources["MyGray"];
            MyWhite = (SolidColorBrush)Resources["MyWhite"];
            MyGreen = (SolidColorBrush)Resources["MyGreen"];
            MyOrange = (SolidColorBrush)Resources["MySelectionColor"];
            MySelectionColor = (SolidColorBrush)Resources["MySelectionColor"];
            _Main(e.Args);
        }
        catch(Exception ex)
        { 
            LogError("STARTUP",ex);
            Shutdown();
        }
    }
}

