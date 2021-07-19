using DTLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static DTLib.NetWork;

namespace dtlauncher_server
{
    class DtlauncherServer
    {
        static readonly string logfile = $"logs\\dtlauncher-server-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');
        static readonly Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static Dtsod config;
        static readonly Dictionary<Socket, Thread> users = new();

        static void Main()
        {
            try
            {
                Console.Title = "dtlauncher server";
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;
                PublicLog.LogDel += Log;
                config = config = new(File.ReadAllText("server.dtsod"));
                int f = (int)config["server_port"];
                Log("b", "local address: <", "c", config["server_ip"], "b",
                    ">\npublic address: <", "c", GetPublicIP(), "b",
                    ">\nport: <", "c", config["server_port"].ToString(), "b", ">\n");
                mainSocket.Bind(new IPEndPoint(IPAddress.Parse(config["server_ip"]), (int)config["server_port"]));
                mainSocket.Listen(1000);
                Log("g", "server started succesfully\n");
                FileWork.CreateManifest("share\\client\\");
                Log("g", "client files manifest created\n");
                //
                /*DTLib.Timer userCkeckTimer = new(true, 3000, () => 
                {
                    foreach (Socket usr in users.Keys)
                    {
                        if (usr.)
                        {
                            Log("y", $"closing unused user <{usr.RemoteEndPoint.Serialize()[0]}> thread\n");
                            users[usr].Abort();
                            users.Remove(usr);
                        }
                    }
                });*/
                // запуск отдельного потока для каждого юзера
                while (true)
                {
                    var userSocket = mainSocket.Accept();
                    var userThread = new Thread(new ParameterizedThreadStart((obj) => UserHandle((Socket)obj)));
                    //users.Add(userSocket, userThread);
                    userThread.Start(userSocket);
                }
            }
            catch (Exception ex)
            {
                Log("r", $"dtlauncher_server.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
                mainSocket.Close();
            }
            Log("press any key to close... ");
            Console.ReadKey();
            Log("gray", "\n");
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
                if (msg.Length == 1) FileWork.Log(logfile, msg[0]);
                else if (msg.Length % 2 != 0) throw new Exception("incorrect array to log\n");
                else FileWork.Log(logfile, SimpleConverter.AutoBuild(msg));
                ColoredConsole.Write(msg);
            }
        }

        // запускается для каждого юзера в отдельном потоке
        static void UserHandle(Socket handlerSocket)
        {
            Log("b", "user connecting...  ");
            //Socket fspSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //fspSocket.Bind()
            try
            {
                handlerSocket.SendPackage("requesting hash".ToBytes());
                var hash = handlerSocket.GetPackage();
                // запрос от апдейтера
                if (hash.HashToString() == "ffffffffffffffff")
                {
                    LogNoTime("c", "client is updater\n");
                    handlerSocket.SendPackage("updater".ToBytes());
                    while (true)
                    {
                        if (handlerSocket.Available >= 2)
                        {
                            var request = handlerSocket.GetPackage().ToStr();
                            switch (request)
                            {
                                case "requesting file download":
                                    string filePath = "share\\client\\" + handlerSocket.GetPackage().ToStr();
                                    handlerSocket.FSP_Upload(filePath);
                                    break;
                                default:
                                    throw new Exception("unknown request: " + request);
                            }
                        }
                        else Thread.Sleep(10);
                    }
                }
                // запрос от лаунчера
                else
                {
                    LogNoTime("g", "client is launcher\n");
                    string login;
                    lock (new object())
                    {
                        login = FileWork.ReadFromConfig("users.db", hash.HashToString());
                    }
                    handlerSocket.SendPackage("success".ToBytes());
                    Log("g", $"user <{login}> succesfully logged\n");
                    while (true)
                    {
                        if (handlerSocket.Available >= 64)
                        {
                            var request = handlerSocket.GetPackage().ToStr();
                            switch (request)
                            {
                                // ответ на NetWork.Ping()
                                /*case "ping":
                                    handlerSocket.Send("pong".ToBytes());
                                    break;*/
                                // отправка списка активных серверов
                                /*case "requesting servers list":

                                    break;*/
                                case "requesting file download":
                                    handlerSocket.FSP_Upload("share\\public\\" + handlerSocket.GetPackage().ToStr());
                                    break;
                                default:
                                    throw new Exception("unknown request: " + request);
                            }
                        }
                        else Thread.Sleep(10);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("y", $"UserStart() error:\n message:\n  {ex.Message}\n{ex.StackTrace}\n");
                handlerSocket.Shutdown(SocketShutdown.Both);
                handlerSocket.Close();
                Thread.CurrentThread.Abort();
            }
        }
    }
}
