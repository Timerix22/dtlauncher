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

        //static readonly Dictionary<Socket, Thread> users = new();

        static void Main()
        {
            try
            {
                Console.Title = "dtlauncher server";
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;
                PublicLog.LogDel += Log;
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
                Log("c", "\n\n" + outBuilder.ToString() + "\n\n");*/
                config = config = new(File.ReadAllText("server.dtsod"));
                int f = (int)config["server_port"];
                Log("b", "local address: <", "c", config["server_ip"], "b",
                    ">\npublic address: <", "c", GetPublicIP(), "b",
                    ">\nport: <", "c", config["server_port"].ToString(), "b", ">\n");
                mainSocket.Bind(new IPEndPoint(IPAddress.Parse(config["server_ip"]), (int)config["server_port"]));
                mainSocket.Listen(1000);
                Log("g", "server started succesfully\n");
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
                    CreateManifest("share\\client\\");
                    Log("g", "client files manifest created\n");
                    handlerSocket.SendPackage("updater".ToBytes());
                    while (true)
                    {
                        if (handlerSocket.Available >= 2)
                        {
                            var request = handlerSocket.GetPackage().ToStr();
                            string recieved, filepath;
                            switch (request)
                            {
                                case "requesting file download":
                                    filepath = "share\\client\\" + handlerSocket.GetPackage().ToStr();
                                    handlerSocket.FSP_Upload(filepath);
                                    break;
                                case "register new user":
                                    Log("b", "new user registration requested\n");
                                    handlerSocket.SendPackage("ok".ToBytes());
                                    //filepath = handlerSocket.GetPackage().ToStr();
                                    //if (!filePath.EndsWith(".req")) throw new Exception($"wrong registration request file: <{filepath}>");
                                    //Log("b", $"downloading file registration_requests\\{filepath}\n");
                                    //handlerSocket.FSP_Download($"registration_requests\\{filepath}");
                                    recieved = handlerSocket.GetPackage().ToStr();
                                    filepath = $"registration_requests\\{recieved.Remove(0, recieved.IndexOf(':') + 2)}.req";
                                    File.WriteAllText(filepath, recieved);
                                    Log("b", $"text wrote to file <","c","registration_requests\\{filepath}","b",">\n");
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
                    LogNoTime("c", "client is launcher\n");
                    string login;
                    lock (new object())
                    {
                        login = FileWork.ReadFromConfig("users.db", hash.HashToString());
                    }
                    handlerSocket.SendPackage("success".ToBytes());
                    Log("g", "user <", "c", login, "g", "> succesfully logined\n");
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


        // вычисляет и записывает в manifest.dtsod хеши файлов из files_list.dtsod
        public static void CreateManifest(string dir)
        {
            if (!dir.EndsWith("\\")) dir += "\\";
            var files = FileWork.GetAllFiles(dir);
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
