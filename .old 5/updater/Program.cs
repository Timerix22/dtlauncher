using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace updater
{
    class Program
    {
        static void Main(string[] args)
        {

        }

        // вывод лога в консоль и файл
        static readonly string logfile = $"logs-updater\\updater-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');

        static public void Log(params string[] msg)
        {
            if (msg.Length == 1)
            {
                msg[0] = "[" + DateTime.Now.ToString() + "]: " + msg[0];
                FileWork.Log(logfile, msg[0]);
            }
            else if (msg.Length % 2 != 0) throw new Exception("incorrect array to log\n");
            else
            {
                msg[1] = "[" + DateTime.Now.ToString() + "]: " + msg[1];
                var str = new System.Text.StringBuilder();
                for (int i = 0; i < msg.Length; i++) str.Append(msg[++i]);
                FileWork.Log(logfile, str.ToString());
            }
            ColoredConsole.Write(msg);
        }
    }
}
