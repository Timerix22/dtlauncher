global using System;
global using System.Diagnostics;
global using System.Net;
global using System.Text;
global using System.Collections.Generic;
global using System.Threading;
global using System.Linq;
global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Media;
global using DTLib;
global using DTLib.Dtsod;
global using DTLib.Filesystem;
global using DTLib.Extensions;
global using static launcher_client_avalonia.Launcher;
using System.Reflection;
using launcher_client_avalonia.GUI;

namespace launcher_client_avalonia;

public static class Launcher
{
    public static SolidColorBrush MyDark, MySoftDark, MyWhite, MyGreen, MyOrange, MySelectionColor;
    public static LauncherConfig Config;
    public static readonly LauncherLogger Logger = new();
    public static LauncherWindow CurrentLauncherWindow;

    public static void Main(string[] args)
    {
        try
        {
            var resources = Application.Current.Resources;
            MyDark = (SolidColorBrush)resources["MyDarkTr"];
            MySoftDark = (SolidColorBrush)resources["MyGray"];
            MyWhite = (SolidColorBrush)resources["MyWhite"];
            MyGreen = (SolidColorBrush)resources["MyGreen"];
            MyOrange = (SolidColorBrush)resources["MySelectionColor"];
            MySelectionColor = (SolidColorBrush)resources["MySelectionColor"];
            
            Logger.Enable();
            
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);

            Config = new LauncherConfig();
            Directory.Create("descriptors");
            Directory.Create("icons");
            Directory.Create("backgrounds");
            Directory.Create("installed");
            Directory.Create("settings");
            File.WriteAllText($"descriptors{Путь.Разд}default.descriptor.template",
                ReadResource("launcher_client_avalonia.Resources.default.descriptor.template"));
            CurrentLauncherWindow = new LauncherWindow();
            CurrentLauncherWindow.Show();

        }
        catch (Exception ex)
        {
            LogError("STARTUP", ex);
        }
    }

    public static string ReadResource(string resource_path)
    {
        using var resourceStreamReader = new System.IO.StreamReader(
            Assembly.GetExecutingAssembly().GetManifestResourceStream(resource_path)
            ?? throw new Exception($"embedded resource <{resource_path}> not found"),
            Encoding.UTF8);
        return resourceStreamReader.ReadToEnd();
    }

    public static void LogError(string context, Exception ex)
    {
        string errmsg = $"{context} ERROR:\n{ex}";
        MessageBox.Show(errmsg);
        Logger.Log(errmsg);
    }
}