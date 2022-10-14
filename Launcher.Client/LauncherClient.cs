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

namespace Launcher.Client;

public static class LauncherClient
{
    public static LauncherConfig Config;
    public static readonly LauncherLogger Logger = new();

    public static void Init()
    {
        Logger.Log("launcher starting...");
        Config = new LauncherConfig();
#if DEBUG
        const string debug_assets = "debug_assets";
        foreach (string file in Directory.GetFiles(debug_assets))
            File.Copy(file, file.Remove(0, file.LastIndexOf(Путь.Разд) + 1), true);
        foreach (string subdir in Directory.GetDirectories(debug_assets))
            Directory.Copy(subdir, subdir.Remove(0, subdir.LastIndexOf(Путь.Разд) + 1), true);
        Directory.Delete(debug_assets);
#endif
        Directory.Create("descriptors");
        Directory.Create("icons");
        Directory.Create("backgrounds");
        Directory.Create("installed");
        Directory.Create("settings");
        File.WriteAllText($"descriptors{Путь.Разд}default.descriptor.template",
            EmbeddedResources.ReadText("Launcher.Client.Resources.default.descriptor.template"));
    }
}