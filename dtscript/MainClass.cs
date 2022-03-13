using System;
using DTLib;
using DTLib.Filesystem;

namespace DTScript;

public class MainClass
{
    static DTLib.Loggers.DefaultLogger Info = new("logs", "dtlaunchet_server");

    static void Main(string[] args)
    {
        try
        {
            Directory.Create("dtscript-logs");
            PublicLog.LogEvent += Info.Log;
            PublicLog.LogNoTimeEvent += Info.Log;
            var scripter = new ScriptRunner();
            if (args.Length == 0 || args.Length > 2) throw new Exception("enter script file path\n");
            else if (args.Length == 1) scripter.RunScriptFile(args[0]);
            else if (args.Length == 2 && args[0] == "-debug")
            {
                scripter.debug = true;
                scripter.Debug("y", "debug is enabled\n");
                scripter.RunScriptFile(args[1]);
            }
            else throw new Exception("unknown args\n");
        }
        catch (Exception ex)
        { Info.Log("r", $"dtscript.Main() error:\n{ex.Message}\n{ex.StackTrace}\n"); }
        Info.Log("gray", " \n");
    }
}
