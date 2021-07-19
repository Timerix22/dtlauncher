using DTLib;
using System;

namespace DTScript
{
    public class MainClass
    {
        static void Main(string[] args)
        {
            try
            {
                FileWork.DirCreate("dtscript-logs");
                PublicLog.LogDel += Log;
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
            catch (Exception e)
            {
                Log("r", $"dtscript Main() error:\n{e.Message}\n{e.StackTrace}\n");
            }
            Log("gray", " \n");
        }

        // вывод лога в консоль и файл
        static readonly string logfile = $"dtscript-logs\\dtscript-log-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');

        static void Log(params string[] msg)
        {
            if (msg.Length != 1 && msg.Length % 2 != 0) throw new Exception("ыыы нечётное количество элементов выводимого массива");
            ColoredConsole.Write(msg);
            if (msg.Length == 1) FileWork.Log(logfile, msg[0]);
            else
            {
                var mergmsg = "";
                for (int i = 0; i < msg.Length; i++) mergmsg += msg[++i];
                FileWork.Log(logfile, mergmsg);
            }
        }
    }
}
