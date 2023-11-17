global using DTLib;
global using DTLib.Dtsod;
global using DTLib.Filesystem;
global using DTLib.Extensions;
global using System;
global using System.Diagnostics;
global using System.Net;
global using System.Text;
global using System.Collections.Generic;
global using System.Linq;
global using System.Windows;
global using Launcher.Client;
global using static Launcher.Client.LauncherClient;
global using static Launcher.Client.WPF.LauncherMain;
using DTLib.Ben.Demystifier;
using DTLib.Logging;
using Launcher.Client.WPF.GUI;

namespace Launcher.Client.WPF;

public static class LauncherMain
{
    public static LauncherWindow CurrentLauncherWindow;
    
    public static void _Main(string[] args)
    {
        Console.WriteLine("aaa\nbbb\nccc");
        return;
        LauncherClient.Init();
        CurrentLauncherWindow = new LauncherWindow();
        CurrentLauncherWindow.Show();
    }

    public static void LogError(string context, Exception ex)
    {
        string errmsg = ex.ToStringDemystified();
        MessageBox.Show(errmsg);
        Logger.LogError(context, errmsg);
    }
}