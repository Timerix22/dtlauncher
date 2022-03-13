using DTLib;
using DTLib.Dtsod;
using DTLib.Filesystem;
using DTLib.Network;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

namespace launcher_server
{
    class Server
    {
        static readonly string logfile = $"logs\\launcher-server_{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');
        static readonly Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static DtsodV22 config;
        static bool debug = false;

        static object manifestLocker = new();

        static void Main(string[] args)
        {
            try
            {
                Console.Title = "minecraft_launcher_server";
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;
                PublicLog.LogEvent += Log;
                PublicLog.LogNoTimeEvent += LogNoTime;
                config = new DtsodV22(File.ReadAllText("launcher-server.dtsod"));
                if (args.Contains("debug")) debug = true;
                Log("b", "local address: <", "c", config["local_ip"], "b",
                    ">\npublic address: <", "c", OldNetwork.GetPublicIP(), "b",
                    ">\nport: <", "c", config["local_port"].ToString(), "b", ">\n");
                mainSocket.Bind(new IPEndPoint(IPAddress.Parse(config["local_ip"]), config["local_port"]));
                mainSocket.Listen(1000);
                CreateManifestы();
                Log("g", "server started succesfully\n");
                // запуск отдельного потока для каждого юзера
                Log("b", "waiting for users\n");
                while (true)
                {
                    var userSocket = mainSocket.Accept();
                    var userThread = new Thread(new ParameterizedThreadStart((obj) => UserHandle((Socket)obj)));
                    userThread.Start(userSocket);
                }
            }
            catch (Exception ex)
            {
                Log("r", $"Server.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
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
                ColoredConsole.Write(msg);
                if (msg.Length == 1) File.AppendAllText(logfile, msg[0]);
                else
                {
                    StringBuilder strB = new();
                    for (ushort i = 0; i < msg.Length; i++)
                        strB.Append(msg[++i]);
                    File.AppendAllText(logfile, strB.ToString());
                }
            }
        }

        // запускается для каждого юзера в отдельном потоке
        static void UserHandle(Socket handlerSocket)
        {
            Log("b", "user connecting...  ");
            try
            {
                // тут запрос пароля заменён запросом заглушки
                handlerSocket.SendPackage("requesting hash".ToBytes());
                var hasher = new Hasher();
                var hash = hasher.HashCycled(handlerSocket.GetPackage(), 64);
                FSP fsp = new(handlerSocket);
                FSP.debug = debug;
                // запрос от апдейтера
                if (hash.HashToString() == "39368b9c9ca9a74007acd2358fb7945cf172fc86c93969d0933e40aee6c10ca8")
                {
                    LogNoTime("b", "user is ", "c", "updater\n");
                    handlerSocket.SendPackage("updater".ToBytes());
                    // обработка запросов
                    while (true)
                    {
                        if (handlerSocket.Available >= 2)
                        {
                            var request = handlerSocket.GetPackage().ToString();
                            switch (request)
                            {
                                case "requesting launcher update":
                                    Log("b", "updater requested client.exe\n");
                                    fsp.UploadFile("share\\launcher.exe");
                                    break;
                                case "register new user":
                                    Log("b", "new user registration requested\n");
                                    handlerSocket.SendPackage("ready".ToBytes());
                                    var req = FrameworkFix.MergeToString(
                                            hasher.HashCycled(handlerSocket.GetPackage(), 64).HashToString(),
                                            ":\n{\n\tusername: \"", handlerSocket.GetPackage().ToString(),
                                            "\";\n\tuuid: \"null\";\n};\n");
                                    var filepath = $"registration_requests\\{DateTime.Now.ToString().Replace(':', '-').Replace(' ', '_')}.req";
                                    File.WriteAllText(filepath, req);
                                    Log("b", $"text wrote to file <", "c", $"registration_requests\\{filepath}", "b", ">\n");
                                    break;
                                default:
                                    throw new Exception("unknown request: " + request);
                            }
                        }
                        else Thread.Sleep(10);
                    }
                }
                // запрос от юзера
                else if (FindUser(hash, out var user))
                {
                    LogNoTime("b", $"user is ", "c", user.name + "\n");
                    handlerSocket.SendPackage("launcher".ToBytes());
                    // обработка запросов
                    while (true)
                    {
                        if (handlerSocket.Available >= 2)
                        {
                            var request = handlerSocket.GetPackage().ToString();
                            switch (request)
                            {
                                case "requesting file download":
                                    var file = handlerSocket.GetPackage().ToString();
                                    Log("b", $"user ", "c", user.name, "b", " requested file ", "c", file + "\n");
                                    if (file == "manifest.dtsod")
                                    {
                                        lock (manifestLocker) fsp.UploadFile("share\\manifest.dtsod");
                                    }
                                    else fsp.UploadFile("share\\" + file);
                                    break;
                                case "requesting uuid":
                                    Log("b", $"user ", "c", user.name, "b", " requested uuid\n");
                                    handlerSocket.SendPackage(user.uuid.ToBytes());
                                    break;
                                case "excess files found":
                                    Log("b", $"user ", "c", user.name, "b", " sent excess files list\n");
                                    fsp.DownloadFile($"excesses\\{user.name}-{DateTime.Now.ToString().Replace(':', '-').Replace(' ', '_')}.txt");
                                    break;
                                case "sending launcher error":
                                    Log("y", "user ", "c", user.name, "y", "is sending error:\n");
                                    string error = handlerSocket.GetPackage().ToString();
                                    Log("y", error + '\n');
                                    break;
                                default:
                                    throw new Exception("unknown request: " + request);
                            }
                        }
                        else Thread.Sleep(10);
                    }
                }
                // неизвестный юзер
                else
                {
                    LogNoTime("y", $"user with hash <{hash.HashToString()}> not found\n");
                    handlerSocket.SendPackage("user not found".ToBytes());
                }
            }
            catch (Exception ex)
            {
                Log("y", $"UserStart() error:\n message:\n  {ex.Message}\n{ex.StackTrace}\n");
            }
            finally
            {
                if (handlerSocket.Connected) handlerSocket.Shutdown(SocketShutdown.Both);
                handlerSocket.Close();
                Log("g", "user disconnected\n");
            }
        }

        static void CreateManifestы()
        {
            lock (manifestLocker)
            {
                FSP.CreateManifest("share\\download_if_not_exist");
                FSP.CreateManifest("share\\sync_always");
                foreach (string dir in Directory.GetDirectories("share\\sync_and_remove"))
                    FSP.CreateManifest(dir);
                File.WriteAllText("share\\sync_and_remove\\dirlist.dtsod",
                    $"dirs: [\"{Directory.GetDirectories("share\\sync_and_remove").MergeToString("\",\"").Replace("share\\sync_and_remove\\", "")}\"];\n");
            };
        }

        static bool FindUser(byte[] hash, out (string name, string uuid) user)
        {
            DtsodV22 usersdb = new(File.ReadAllText("users.dtsod"));
            user = new();
            if (usersdb.ContainsKey(hash.HashToString()))
            {
                user.name = usersdb[hash.HashToString()]["username"];
                user.uuid = usersdb[hash.HashToString()]["uuid"];
                return true;
            }
            else return false;
        }
    }
}
