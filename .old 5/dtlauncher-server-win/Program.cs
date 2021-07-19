using DTLib;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static DTLib.NetWork;

namespace dtlauncher_server
{
    class Program
    {
        static Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Console.Title = "dtlauncher server";
                    PublicLog.LogDel += Log;
                    Log("b", $"<{FileWork.ReadFromConfig("server.cfg", "server ip")}:{FileWork.ReadFromConfig("server.cfg", "server port")}>\n");
                    mainSocket.Bind(new IPEndPoint(IPAddress.Parse(
                        FileWork.ReadFromConfig("server.cfg", "server ip")),
                        FileWork.ReadFromConfig("server.cfg", "server port").ToInt()));
                    Log("g", "server started succesfully\n");
                    mainSocket.Listen(1000);
                    while (true)
                    {
                        var userSocket = mainSocket.Accept();
                        var userThread = new Thread(new ParameterizedThreadStart(UserHandle));
                        //users.Add(userSocket, userThread);
                        userThread.Start(userSocket);
                    }
                }
                catch (Exception ex)
                {
                    if (mainSocket.IsBound)
                    {
                        mainSocket.Shutdown(SocketShutdown.Both);
                        mainSocket.Close();
                    }
                    Log("r", $"dtlauncher_server.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
                }
                Log("gray", "\n");
            }
        }

        // лог в консоль и файл
        static readonly string logfile = $"logs\\dtlauncher-server-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');
        static void Log(params string[] msg)
        {
            lock (new object())
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

        // запускается для каждого юзера в отдельном потоке
        static void UserHandle(object _handlerSocket)
        {
            Log("g", "user connecting\n");
            Socket handlerSocket = (Socket)_handlerSocket;
            try
            {
                handlerSocket.SendPackage(16, "requesting hash".ToBytes());
                string login;
                lock (new object())
                {
                    login = FileWork.ReadFromConfig("users.db", handlerSocket.GetPackageRaw(32).HashToString());
                }
                handlerSocket.SendPackage(16, "success".ToBytes());
                Log("g", $"user <{login}> succesfully logged\n");
                while (true)
                {
                    if (handlerSocket.Available >= 64)
                    {
                        var request = handlerSocket.GetPackageClear(64).ToStr();
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
                                handlerSocket.FSP_Upload();
                                break;
                            default:
                                throw new Exception("unknown request: " + request);
                        }
                    }
                    else Thread.Sleep(10);
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
