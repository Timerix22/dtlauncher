using System;
using DTLib;
using DTLib.Filesystem;

namespace DTScript
{
    public class MainClass
    {
        static void Main(string[] args)
        {
            try
            {
                Directory.Create("dtscript-logs");
                PublicLog.LogEvent += Log;
                PublicLog.LogNoTimeEvent += Log;
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
            {
                Log("r", $"dtscript.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
            }
            Log("gray", " \n");
        }

        // вывод лога в консоль и файл
        static readonly string logfile = $"dtscript-logs\\dtscript-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');

        static void Log(params string[] msg)
        {
            if (msg.Length == 1)
            {
                msg[0] = "[" + DateTime.Now.ToString() + "]: " + msg[0];
                OldFilework.LogToFile(logfile, msg[0]);
            }
            else if (msg.Length % 2 != 0) throw new Exception("incorrect array to log\n");
            else
            {
                msg[1] = "[" + DateTime.Now.ToString() + "]: " + msg[1];
                var str = new System.Text.StringBuilder();
                for (int i = 0; i < msg.Length; i++) str.Append(msg[++i]);
                OldFilework.LogToFile(logfile, str.ToString());
            }
            ColoredConsole.Write(msg);
        }
    }
}
