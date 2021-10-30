using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DTLib;
using DTLib.Filesystem;
using DTLib.Network;

namespace updater
{
    class Updater
    {
        static readonly string logfile = $"logs\\updater-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');
        static Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static readonly string server_domain = "m1net.keenetic.pro";
        static readonly int server_port = 25001;

        static void Main(string[] args)
        {
            try
            {
                Console.Title = "dtlauncher updater";
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;
                PublicLog.LogEvent += Log;
                PublicLog.LogNoTimeEvent += Log;
                // подключение к центральному серверу
                while (true)
                {
                    try
                    {
                        Log("b", "server address: <", "c", server_domain, "b",
                            ">\nserver port: <", "c", server_port.ToString(), "b", ">\n");
                        mainSocket.Connect(new IPEndPoint(Dns.GetHostAddresses(server_domain)[0], server_port));
                        Log("g", "connected to server\n");
                        break;
                    }
                    catch (SocketException ex)
                    {
                        Log("r", $"updater.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
                    }
                }
                var fsp = new FSP(mainSocket);
                string recieved = mainSocket.GetPackage().BytesToString();
                if (recieved != "requesting hash") throw new Exception("invalid server request");
                mainSocket.SendPackage(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
                recieved = mainSocket.GetPackage().BytesToString();
                if (recieved != "updater") throw new Exception($"invalid central server answer <{recieved}>");
                // обновление апдейтера
                if (args.Length == 0 || args[0] != "updated")
                {
                    fsp.DownloadFile("dtlauncher.exe", "TEMP\\dtlauncher.exe");
                    Log("g", "dtlauncher.exe downloaded\n");
                    fsp.DownloadFile("DTLib.dll", "TEMP\\DTLib.dll");
                    Log("g", "DTLib.dll downloaded\n");
                    Process.Start("cmd", "/c timeout 0 && copy TEMP\\dtlauncher.exe dtlauncher.exe && copy TEMP\\DTLib.dll DTLib.dll && start dtlauncher.exe updated");
                }
                else
                {

                    // установка шрифтов
                    /*Log("installing fonts\n");
                    Process.Start("fonts\\fontinst.exe");
                    Directory.Delete("TEMP");*/
                    Log("deleted TEMP\n");
                    fsp.DownloadFile("dtlauncher-client-win.exe", "dtlauncher-client-win.exe");
                    Process.Start("dtlauncher-client-win.exe", "updated");
                }
            }
            catch (Exception ex)
            {
                Log("r", $"updater.Main() error:\n{ex.Message}\n{ex.StackTrace}\n", "gray", "press any key to close...");
                Console.ReadKey();
            }
            Log("gray", " \n");
        }

        // вывод лога в консоль и файл
        public static void Log(params string[] msg)
        {
            if (msg.Length == 1) msg[0] = "[" + DateTime.Now.ToString() + "]: " + msg[0];
            else msg[1] = "[" + DateTime.Now.ToString() + "]: " + msg[1];
            LogNoTime(msg);
        }
        public static void LogNoTime(params string[] msg)
        {
            lock (new object())
            {
                if (msg.Length == 1) OldFilework.LogToFile(logfile, msg[0]);
                else if (msg.Length % 2 != 0) throw new Exception("incorrect array to log\n");
                else OldFilework.LogToFile(logfile, msg.MergeToString());
                ColoredConsole.Write(msg);
            }
        }
    }
}
