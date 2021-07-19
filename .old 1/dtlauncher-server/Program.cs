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
        //static ConsoleGUI gui = new ConsoleGUI(90, 30);
        static string logfile = $"logs\\server-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');
        static Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //static Dictionary<Socket, Thread> users = new Dictionary<Socket, Thread>();

        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Console.Title = "dtlauncher server";
                    //gui.ReadFromFile("gui\\main.gui");
                    //gui.ShowAll();
                    NetWork.Log += Log;
                    Log("b", $"<{FileWork.ReadFromConfig("server.cfg", "server ip")}> : <{Convert.ToInt32(FileWork.ReadFromConfig("server.cfg", "server port"))}>\n");
                    mainSocket.Bind(new IPEndPoint(IPAddress.Parse(
                        FileWork.ReadFromConfig("server.cfg", "server ip")),
                        Convert.ToInt32(FileWork.ReadFromConfig("server.cfg", "server port"))));
                    Log("g", "server started succesfully\n");
                    //Thread userCheckThread = new Thread(CloseUnusedUserThreads);
                    //userCheckThread.Start();
                    try
                    {
                        mainSocket.Listen(200);
                        while (true)
                        {
                            var userSocket = mainSocket.Accept();
                            var userThread = new Thread(new ParameterizedThreadStart(UserStart));
                            //users.Add(userSocket, userThread);
                            userThread.Start(userSocket);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("r", $"handler error:\n message:\n  {ex.Message}\nmethod:\n  {ex.TargetSite}\n");
                    }
                }
                catch (ThreadAbortException)
                {

                }
                catch (Exception ex)
                {
                    if (mainSocket.IsBound) mainSocket.CloseSocket();
                    Log("r", $"Main() error:\n message:\n  {ex.Message}\nmethod:\n  {ex.TargetSite}\n");
                }
                Thread.Sleep(1500);
            }
        }

        static void Log(string color, string msg)
        {
            ColoredText.WriteColored(color, "[" + DateTime.Now.ToString() + "]: " + msg);
            FileWork.Log(logfile, msg);
        }

        static void Log(string[] input)
        {
            if (input.Length % 2 == 0)
            {
                input[1] = "[" + DateTime.Now.ToString() + "]: " + input[1];
                ColoredText.WriteColored(input);
                string str = "";
                for (ushort i = 0; i < input.Length; i++)
                {
                    str += input[++i];
                }
                FileWork.Log(logfile, str);
            }
            else
            {
                throw new Exception("error in Log(): every text string must have color string before");
            }
        }

        static void UserStart(dynamic _handlerSocket)
        {
            Socket handlerSocket = (Socket)_handlerSocket;
            Log("g", "user connected\n");
            try
            {
                string recieved = handlerSocket.GetData().ToStr();
                if (recieved == "new user connection try")
                {
                    handlerSocket.Send("new user connection created".ToBytes());
                    while (true)
                    {
                        recieved = handlerSocket.GetData().ToStr();
                        switch (recieved)
                        {
                            // ответ на NetWork.Ping()
                            case "ping":
                                handlerSocket.Send("pong".ToBytes());
                                break;
                            // отправка списка активных серверов
                            case "requesting servers list":

                                break;
                            case "requesting file download":
                                handlerSocket.FSP_Upload();
                                break;
                            default:
                                throw new Exception("unknown request: " + recieved);
                        }
                    }
                }

                else
                {
                    throw new Exception("incorrect connection try");
                }

            }
            catch (Exception ex)
            {
                Log("y", $"UserStart() error:\n message:\n  {ex.Message}\n{ex.StackTrace}\n");
                handlerSocket.CloseSocket();
                Thread.CurrentThread.Abort();
            }
        }

        /*static void CloseUnusedUserThreads()
        {
            while (true)
            {
                foreach (Socket s in users.Keys)
                {
                    if (!NetWork.Ping(s))
                    {
                        Log("y", "closing unused user thread\n");
                        users[s].Abort();
                        users.Remove(s);
                    }
                }
                Thread.Sleep(300000);
            }
        }*/
    }
}
