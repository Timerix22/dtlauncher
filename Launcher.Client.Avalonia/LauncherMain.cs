global using System;
global using System.Diagnostics;
global using System.Net;
global using System.Text;
global using System.Collections.Generic;
global using System.Linq;
global using DTLib;
global using DTLib.Dtsod;
global using DTLib.Filesystem;
global using DTLib.Extensions;
global using Launcher.Client;
global using static Launcher.Client.LauncherClient;
global using static Launcher.Client.Avalonia.LauncherMain;
using DTLib.Ben.Demystifier;
using DTLib.Logging;
using Launcher.Client.Avalonia.GUI;

namespace Launcher.Client.Avalonia;

public static class LauncherMain
{
    public static LauncherWindow CurrentLauncherWindow;

    //it's being used by Avalonia xml preview
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    
    public static void Main(string[] args)
    {
        try
        {
            LauncherClient.Init();
            var traceHandler = new ConsoleTraceListener();
            Trace.AutoFlush = true;
            Trace.Listeners.Add(traceHandler);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        { 
            LogError("STARTUP", ex);
        }
    }

    public static void LogError(string context, Exception ex)
    {
        string errmsg = ex.ToStringDemystified();
        MessageBox.Show($"{context} ERROR", errmsg);
        Logger.LogError("Main", errmsg);
    }
}