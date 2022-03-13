global using DTLib;
global using DTLib.Dtsod;
global using DTLib.Filesystem;
global using DTLib.Network;
global using DTLib.Extensions;
global using System;
global using System.Net;
global using System.Net.Sockets;
global using System.Text;
global using System.Threading;
global using System.Linq;
using System.Globalization;

namespace launcher_server;

static class Server
{
    static readonly Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    static DtsodV23 config;
    private static readonly DTLib.Loggers.AsyncLogger Logger = new("logs", "launcher-server");

    static readonly object manifestLocker = new();

    static void Main(string[] args)
    {
        try
        {
            Console.Title = "minecraft_launcher_server";
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;
            Logger.Enable();
            PublicLog.LogEvent += Logger.LogAsync;
            PublicLog.LogNoTimeEvent += Logger.LogAsync;
            config = new DtsodV23(File.ReadAllText("launcher-server.dtsod"));
            Logger.Log("b", "local address: <", "c", config["local_ip"], "b",
                ">\npublic address: <", "c", OldNetwork.GetPublicIP(), "b",
                ">\nport: <", "c", config["local_port"].ToString(), "b", ">");
            mainSocket.Bind(new IPEndPoint(IPAddress.Parse(config["local_ip"]), config["local_port"]));
            mainSocket.Listen(1000);
            CreateManifests();
            Logger.Log("g", "server started succesfully");
            // запуск отдельного потока для каждого юзера
            Logger.Log("b", "waiting for users");
            while (true)
            {
                var userSocket = mainSocket.Accept();
                var userThread = new Thread(new ParameterizedThreadStart((obj) => UserHandle((Socket)obj)));
                userThread.Start(userSocket);
            }
        }
        catch (Exception ex)
        {
            Logger.Log("r", ex.ToString());
            if (mainSocket.Connected)
            {
                mainSocket.Shutdown(SocketShutdown.Both);
                mainSocket.Close();
            }
        }
        Logger.Log("gray", "");
    }

    // запускается для каждого юзера в отдельном потоке
    static void UserHandle(Socket handlerSocket)
    {
        Logger.LogAsync("b", "user connecting...  ");
        try
        {
            // запрос хеша пароля и логина
            handlerSocket.SendPackage("requesting hash".ToBytes());
            var hasher = new Hasher();
            var hash = hasher.HashCycled(handlerSocket.GetPackage(), 64);
            FSP fsp = new(handlerSocket);
            // запрос от апдейтера
            if (hash == hasher.HashCycled("updater".ToBytes(),64))
            {
                Logger.LogAsync("b", "user is ", "c", "updater");
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
                                Logger.LogAsync("b", "updater requested client.exe");
                                fsp.UploadFile("share\\launcher.exe");
                                break;
                            case "register new user":
                                Logger.LogAsync("b", "new user registration requested");
                                handlerSocket.SendPackage("ready".ToBytes());
                                string req = StringConverter.MergeToString(
                                    hasher.HashCycled(handlerSocket.GetPackage(), 64).HashToString(),
                                    ":\n{\n\tusername: \"", handlerSocket.GetPackage().ToString(),
                                    "\";\n\tuuid: \"null\";\n};");
                                string filepath = $"registration_requests\\{DateTime.Now.ToString(CultureInfo.InvariantCulture).NormalizeAsPath()}.req";
                                File.WriteAllText(filepath, req);
                                Logger.LogAsync("b", $"text wrote to file <", "c", $"registration_requests\\{filepath}", "b", ">");
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
                Logger.LogAsync("b", $"user is ", "c", user.name);
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
                                Logger.LogAsync("b", $"user ", "c", user.name, "b", " requested file ", "c", file);
                                if (file == "manifest.dtsod")
                                {
                                    lock (manifestLocker) fsp.UploadFile("share\\manifest.dtsod");
                                }
                                else fsp.UploadFile("share\\" + file);
                                break;
                            case "requesting uuid":
                                Logger.LogAsync("b", $"user ", "c", user.name, "b", " requested uuid");
                                handlerSocket.SendPackage(user.uuid.ToBytes());
                                break;
                            case "excess files found":
                                Logger.LogAsync("b", $"user ", "c", user.name, "b", " sent excess files list");
                                fsp.DownloadFile($"excesses\\{user.name}-{DateTime.Now.ToString(CultureInfo.InvariantCulture).NormalizeAsPath()}.txt");
                                break;
                            case "sending launcher error":
                                Logger.LogAsync("y", "user ", "c", user.name, "y", "is sending error:");
                                string error = handlerSocket.GetPackage().ToString();
                                Logger.LogAsync("y", error + '\n');
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
                Logger.LogAsync("y", $"user with hash <{hash.HashToString()}> not found");
                handlerSocket.SendPackage("user not found".ToBytes());
            }
        }
        catch (Exception ex)
        {
            Logger.LogAsync("y", $"UserStart() error:\n message:\n  {ex}");
            if (mainSocket.Connected)
            {
                mainSocket.Shutdown(SocketShutdown.Both);
                mainSocket.Close();
            }
        }
        finally
        {
            if (handlerSocket.Connected) handlerSocket.Shutdown(SocketShutdown.Both);
            handlerSocket.Close();
            Logger.LogAsync("g", "user disconnected");
        }
    }

    static void CreateManifests()
    {
        lock (manifestLocker)
        {
            Directory.Create("share\\download_if_not_exist");
            Directory.Create("share\\sync_always");
            Directory.Create("share\\sync_and_remove");
            
            FSP.CreateManifest("share\\download_if_not_exist");
            FSP.CreateManifest("share\\sync_always");
            foreach (string dir in Directory.GetDirectories("share\\sync_and_remove"))
                FSP.CreateManifest(dir);
            File.WriteAllText("share\\sync_and_remove\\dirlist.dtsod",
                $"dirs: [\""+
                    Directory.GetDirectories("share\\sync_and_remove")
                        .MergeToString("\",\"")
                        .Replace("share\\sync_and_remove\\", "")+
                "\"];");
        };
    }

    static bool FindUser(byte[] hash, out (string name, string uuid) user)
    {
        DtsodV23 usersdb = new(File.ReadAllText("users.dtsod"));
        user = new ValueTuple<string, string>();
        if (!usersdb.ContainsKey(hash.HashToString())) return false;
        user.name = usersdb[hash.HashToString()]["username"];
        user.uuid = usersdb[hash.HashToString()]["uuid"];
        return true;
    }
}