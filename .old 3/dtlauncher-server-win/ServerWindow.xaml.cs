using DTLib;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using static DTLib.NetWork;

namespace dtlauncher_server_win
{
    /// <summary>
    /// Логика взаимодействия для ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window
    {
        static string logfile = $"logs\\server-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');
        static Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public ServerWindow()
        {
            InitializeComponent();
            /*while (true)
            {
                try
                {
                    FileWork.DirExistenceCheck("logs");
                    FileWork.DirExistenceCheck("share");
                    NetWork.Log += Log;
                    Log($"<{FileWork.ReadFromConfig("server.cfg", "server ip")}> : <{Convert.ToInt32(FileWork.ReadFromConfig("server.cfg", "server port"))}>\n");
                    mainSocket.Bind(new IPEndPoint(IPAddress.Parse(FileWork.ReadFromConfig("server.cfg", "server ip")), Convert.ToInt32(FileWork.ReadFromConfig("server.cfg", "server port"))));
                    Log("server started succesfully\n");
                    try
                    {
                        mainSocket.Listen(200);
                        while (true)
                        {
                            var userSocket = mainSocket.Accept();
                            var userThread = new Thread(new ParameterizedThreadStart(UserStart));
                            userThread.Start(userSocket);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"handler error:\n message:\n  {ex.Message}\nmethod:\n  {ex.TargetSite}\n");
                    }
                }
                catch (Exception ex)
                {
                    if (mainSocket.IsBound) mainSocket.CloseSocket();
                    Log($"Main() error:\n message:\n  {ex.Message}\nmethod:\n  {ex.TargetSite}\n");
                }
                Thread.Sleep(1500);
            }*/
        }

        void Log(string msg)
        {
            msg = "[" + DateTime.Now.ToString() + "]: " + msg;
            FileWork.Log(logfile, msg);
            //LogBox.Text += msg;
        }

        void Log(string[] input)
        {
            if (input.Length % 2 == 0)
            {
                string str = "";
                for (ushort i = 0; i < input.Length; i++)
                    str += input[++i];
                Log(str);
            }
            else throw new Exception("error in Log(): every text string must have color string before");
        }

        void UserStart(dynamic _handlerSocket)
        {
            Socket handlerSocket = (Socket)_handlerSocket;
            Log("user connected\n");
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
                            // отправка файла
                            case "requesting file download":
                                handlerSocket.FSP_Upload();
                                break;
                            default:
                                throw new Exception("unknown request: " + recieved);
                        }
                    }
                }
                else throw new Exception("incorrect connection try");
            }
            catch (Exception ex)
            {
                Log($"UserStart() error:\n message:\n  {ex.Message}\n{ex.StackTrace}\n");
                handlerSocket.CloseSocket();
                Thread.CurrentThread.Abort();
            }
        }
    }
}
