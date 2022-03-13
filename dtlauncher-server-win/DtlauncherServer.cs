using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DTLib;
using DTLib.Dtsod;
using DTLib.Filesystem;
using DTLib.Network;
using DTLib.Extensions;

namespace dtlauncher_server
{
    class DtlauncherServer
    {
        static readonly string logfile = $"logs\\dtlauncher-server-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');
        static readonly Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static DtsodV21 config;
        static DTLib.Loggers.DefaultLogger Info = new("logs", "dtlaunchet_server");

        //static readonly Dictionary<Socket, Thread> users = new();

        static void Main()
        {
            try
            {
                Console.Title = "dtlauncher server";
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;
                PublicLog.LogEvent += Info.Log;
                PublicLog.LogNoTimeEvent += Info.Log;
                /*var outBuilder = new StringBuilder();
                string time = DateTime.Now.ToString().Replace(':', '-').Replace(' ', '_');
                foreach (var _file in Directory.GetFiles(@"D:\!dtlauncher-server\share\public\Conan_Exiles"))
                {
                    var file = _file.Remove(0, 35);
                    outBuilder.Append("Download(\"");
                    outBuilder.Append(file);
                    outBuilder.Append("\", \"downloads\\");
                    outBuilder.Append(file);
                    outBuilder.Append("\");\n");
                    outBuilder.Append("Run(\"cmd\", \"/c unarc.exe x -dpinstalled downloads\\");
                    outBuilder.Append(file);
                    outBuilder.Append(" >> logs\\installation-Conan_Exiles-");
                    outBuilder.Append(time);
                    outBuilder.Append(".log\");\n");
                    outBuilder.Append("FileDelete(\"downloads\\");
                    outBuilder.Append(file);
                    outBuilder.Append("\");\n");
                }
                Info.Log("c", "\n\n" + outBuilder.ToString() + "\n\n");*/
                config = config = new(File.ReadAllText("server.dtsod"));
                int f = (int)config["server_port"];
                Info.Log("b", "local address: <", "c", config["server_ip"], "b",
                    ">\npublic address: <", "c", OldNetwork.GetPublicIP(), "b",
                    ">\nport: <", "c", config["server_port"].ToString(), "b", ">\n");
                mainSocket.Bind(new IPEndPoint(IPAddress.Parse(config["server_ip"]), (int)config["server_port"]));
                mainSocket.Listen(1000);
                Info.Log("g", "server started succesfully\n");
                //
                /*DTLib.Timer userCkeckTimer = new(true, 3000, () => 
                {
                    foreach (Socket usr in users.Keys)
                    {
                        if (usr.)
                        {
                            Info.Log("y", $"closing unused user <{usr.RemoteEndPoint.Serialize()[0]}> thread\n");
                            users[usr].Abort();
                            users.Remove(usr);
                        }
                    }
                });*/
                // запуск отдельного потока для каждого юзера
                while (true)
                {
                    Socket userSocket = mainSocket.Accept();
                    var userThread = new Thread(new ParameterizedThreadStart((obj) => UserHandle((Socket)obj)));
                    //users.Add(userSocket, userThread);
                    userThread.Start(userSocket);
                }
            }
            catch (Exception ex)
            {
                Info.Log("r", $"dtlauncher_server.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
                mainSocket.Close();
            }
            Info.Log("press any key to close... ");
            Console.ReadKey();
            Info.Log("gray", "\n");
        }

        // запускается для каждого юзера в отдельном потоке
        static void UserHandle(Socket handlerSocket)
        {
            Info.Log("b", "user connecting...  ");
            //Socket fspSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //fspSocket.Bind()
            FSP fsp = new(handlerSocket);
            try
            {
                handlerSocket.SendPackage("requesting hash".ToBytes());
                byte[] hash = handlerSocket.GetPackage();
                // запрос от апдейтера
                if (hash.HashToString() == "ffffffffffffffff")
                {
                    Info.LogNoTime("c", "client is updater\n");
                    CreateManifest("share\\client\\");
                    Info.Log("g", "client files manifest created\n");
                    handlerSocket.SendPackage("updater".ToBytes());
                    while (true)
                    {
                        if (handlerSocket.Available >= 2)
                        {
                            string request = handlerSocket.GetPackage().BytesToString();
                            string recieved, filepath;
                            switch (request)
                            {
                                case "requesting file download":
                                    filepath = "share\\client\\" + handlerSocket.GetPackage().BytesToString();
                                    fsp.UploadFile(filepath);
                                    break;
                                case "register new user":
                                    Info.Log("b", "new user registration requested\n");
                                    handlerSocket.SendPackage("ok".ToBytes());
                                    //filepath = handlerSocket.GetPackage().BytesToString();
                                    //if (!filePath.EndsWith(".req")) throw new Exception($"wrong registration request file: <{filepath}>");
                                    //Info.Log("b", $"downloading file registration_requests\\{filepath}\n");
                                    //handlerSocket.FSP_Download($"registration_requests\\{filepath}");
                                    recieved = handlerSocket.GetPackage().BytesToString();
                                    filepath = $"registration_requests\\{recieved.Remove(0, recieved.IndexOf(':') + 2)}.req";
                                    File.WriteAllText(filepath, recieved);
                                    Info.Log("b", $"text wrote to file <", "c", "registration_requests\\{filepath}", "b", ">\n");
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
                    Info.LogNoTime("c", "client is launcher\n");
                    string login;
                    lock (new object())
                    {
                        login = OldFilework.ReadFromConfig("users.db", hash.HashToString());
                    }
                    handlerSocket.SendPackage("success".ToBytes());
                    Info.Log("g", "user <", "c", login, "g", "> succesfully logined\n");
                    while (true)
                    {
                        if (handlerSocket.Available >= 64)
                        {
                            string request = handlerSocket.GetPackage().BytesToString();
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
                                    fsp.UploadFile("share\\public\\" + handlerSocket.GetPackage().BytesToString());
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
                Info.Log("y", $"UserStart() error:\n message:\n  {ex.Message}\n{ex.StackTrace}\n");
                handlerSocket.Shutdown(SocketShutdown.Both);
                handlerSocket.Close();
                Thread.CurrentThread.Abort();
            }
        }

        // вычисляет и записывает в manifest.dtsod хеши файлов из files_list.dtsod
        public static void CreateManifest(string dir)
        {
            if (!dir.EndsWith("\\")) dir += "\\";
            List<string> files = Directory.GetAllFiles(dir);
            if (files.Contains(dir + "manifest.dtsod")) files.Remove(dir + "manifest.dtsod");
            StringBuilder manifestBuilder = new();
            Hasher hasher = new();
            for (int i = 0; i < files.Count; i++)
            {
                files[i] = files[i].Remove(0, dir.Length);
                manifestBuilder.Append(files[i]);
                manifestBuilder.Append(": \"");
                byte[] hash = hasher.HashFile(dir + files[i]);
                manifestBuilder.Append(hash.HashToString());
                manifestBuilder.Append("\";\n");
            }
            File.WriteAllText(dir + "manifest.dtsod", manifestBuilder.ToString());
        }
    }
}
