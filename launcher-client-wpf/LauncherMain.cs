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
global using  static Launcher.Client.WPF.LauncherMain;
using Launcher.Client.WPF.GUI;

namespace Launcher.Client.WPF;

public static class LauncherMain
{
    public static LauncherConfig Config;
    public static readonly LauncherLogger Logger = new();
    public static LauncherWindow CurrentLauncherWindow;
    
    public static void _Main(string[] args)
    {
        Config = new LauncherConfig();
        Directory.Create("descriptors");
        Directory.Create("icons");
        Directory.Create("backgrounds");
        Directory.Create("installed");
        Directory.Create("settings");
        File.WriteAllText($"descriptors{Путь.Разд}default.descriptor.template",
            EmbeddedResources.ReadText("Launcher.Client.WPF.Resources.default.descriptor.template"));
        CurrentLauncherWindow = new LauncherWindow();
        CurrentLauncherWindow.Show();
    }

    public static void LogError(string context, Exception ex)
    {
        string errmsg = $"{context} ERROR:\n{ex}";
        MessageBox.Show(errmsg);
        Logger.Log(errmsg);
    }
}