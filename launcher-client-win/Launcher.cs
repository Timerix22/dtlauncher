global using DTLib;
global using DTLib.Dtsod;
global using DTLib.Filesystem;
global using DTLib.Network;
global using DTLib.Extensions;
global using System;
global using System.Diagnostics;
global using System.Net;
global using System.Net.Sockets;
global using System.Text;
global using System.Collections.Generic;
global using System.Threading;
global using System.Linq;
global using System.Windows;
global using  static launcher_client_win.Launcher;
using System.Reflection;
using launcher_client_win.GUI;

namespace launcher_client_win;

public static class Launcher
{
    public static LauncherConfig Config;
    public static readonly LauncherLogger Logger = new();
    
    
    public static void _Main(string[] args)
    {
        Logger.Enable();
        Config = new LauncherConfig();
        LauncherWindow launcherWindow = new();
        MessageBox.Show("HELLO");
        launcherWindow.Show();
    }
    
    public static string ReadResource(string resource_path)
    {
        using var resourceStreamReader = new System.IO.StreamReader(
            Assembly.GetExecutingAssembly().GetManifestResourceStream(resource_path) 
                ?? throw new Exception($"embedded resource <{resource_path}> not found"), 
            Encoding.UTF8);
        return resourceStreamReader.ReadToEnd();
    }
}