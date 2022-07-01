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
global using static Launcher.Client.Avalonia.LauncherMain;
using Launcher.Client.Avalonia.GUI;

namespace Launcher.Client.Avalonia;

public static class LauncherMain
{
    public static LauncherConfig Config;
    public static readonly LauncherLogger Logger = new();
    public static LauncherWindow CurrentLauncherWindow;

    public static void Main(string[] args)
    {
        try
        {
            Config = new LauncherConfig();
            Directory.Create("descriptors");
            Directory.Create("icons");
            Directory.Create("backgrounds");
            Directory.Create("installed");
            Directory.Create("settings");
            File.WriteAllText($"descriptors{Путь.Разд}default.descriptor.template",
                EmbeddedResources.ReadText("Launcher.Client.Avalonia.Resources.default.descriptor.template"));
            
            var traceHandler = new ConsoleTraceListener();
            Trace.AutoFlush = true;
            Trace.Listeners.Add(traceHandler);
            
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        { 
            LogError("STARTUP", ex);
        }
    }

    public static void LogError(string context, Exception ex)
    {
        string errmsg = $"{ex.Message}\n{ex.StackTrace}";
        //MessageBox.Show($"{context} ERROR", errmsg);
        Logger.Log(errmsg);
    }
}