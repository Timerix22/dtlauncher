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
using DTLib.Logging;

namespace Launcher.Server;

static class Server
{
    static readonly Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    static DtsodV23 config;
    private static readonly CompositeLogger Logger = new(
        new ConsoleLogger(),
        new FileLogger("logs", "launcher-server"));

    static readonly object manifestLocker = new();

    static void Main()
    {
        try
        {
            Console.Title = "Launcher.Server";
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;
            DTLibInternalLogging.SetLogger(Logger);
            config = new DtsodV23(File.ReadAllText("launcher-server.dtsod"));
            Logger.LogInfo("Main", $"""
                local address: {config["local_ip"]}
                public address: {OldNetwork.GetPublicIP()}
                port: {config["local_port"]}
                """);
            mainSocket.Bind(new IPEndPoint(IPAddress.Parse(config["local_ip"]), config["local_port"]));
            mainSocket.Listen(1000);
            CreateManifests();
            Logger.LogInfo("Main", "server started succesfully");
            // запуск отдельного потока для каждого юзера
            Logger.LogInfo("Main", "waiting for users");
            while (true)
            {
                var userSocket = mainSocket.Accept();
                var userThread = new Thread((obj) => HandleUser((Socket)obj));
                userThread.Start(userSocket);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Main", ex);
            if (mainSocket.Connected)
            {
                mainSocket.Shutdown(SocketShutdown.Both);
                mainSocket.Close();
            }
        }
        Console.ResetColor();
    }

    // запускается для каждого юзера в отдельном потоке
    static void HandleUser(Socket handlerSocket)
    {
        Logger.LogInfo("HandleUser", "user connecting...");
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
                Logger.LogInfo("HandleUser", "user is updater");
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
                                Logger.LogInfo("HandleUser", "updater requested client.exe");
                                fsp.UploadFile("share\\launcher.exe");
                                break;
                            case "register new user":
                                Logger.LogInfo("HandleUser", "new user registration requested");
                                handlerSocket.SendPackage("ready".ToBytes());
                                string req = StringConverter.MergeToString(
                                    hasher.HashCycled(handlerSocket.GetPackage(), 64).HashToString(),
                                    ":\n{\n\tusername: \"", handlerSocket.GetPackage().ToString(),
                                    "\";\n\tuuid: \"null\";\n};");
                                var filepath = Path.Concat("registration_requests", DateTime.Now.ToString(MyTimeFormat.ForFileNames), ".req");
                                File.WriteAllText(filepath, req);
                                Logger.LogInfo("HandleUser", $"text wrote to file <{filepath}>");
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
                Logger.LogInfo("HandleUser", "user is " + user.name);
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
                                var requestedFile = Path.Concat("share",handlerSocket.GetPackage().ToString());
                                Logger.LogInfo("HandleUser", $"user {user.name} requested file {requestedFile}");
                                if (requestedFile == "share/manifest.dtsod")
                                    lock (manifestLocker)
                                        fsp.UploadFile(requestedFile.ToString());
                                else fsp.UploadFile(requestedFile.ToString());
                                break;
                            case "requesting uuid":
                                Logger.LogInfo("HandleUser", "user " + user.name + " requested uuid");
                                handlerSocket.SendPackage(user.uuid.ToBytes());
                                break;
                            case "excess files found":
                                Logger.LogInfo("HandleUser", "user " + user.name + " sent excess files list");
                                fsp.DownloadFile(Path.Concat(
                                    "excesses",user.name, DateTime.Now.ToString(MyTimeFormat.ForFileNames),".txt")
                                    .ToString());
                                break;
                            case "sending launcher error":
                                string error = handlerSocket.GetPackage().ToString();
                                Logger.LogWarn("HandleUser",  "user "+ user.name + "is sending error:\n"+error);
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
                Logger.LogWarn("HandleUser", $"user with hash {hash.HashToString()} not found");
                handlerSocket.SendPackage("user not found".ToBytes());
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarn("HandleUser", ex);
            if (mainSocket.Connected)
            {
                mainSocket.Shutdown(SocketShutdown.Both);
                mainSocket.Close();
            }
        }
        finally
        {
            if (handlerSocket.Connected) 
                handlerSocket.Shutdown(SocketShutdown.Both);
            handlerSocket.Close();
            Logger.LogInfo("HandleUser", "user disconnected");
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
                "dirs: [\""+
                    Directory.GetDirectories("share\\sync_and_remove")
                        .MergeToString("\",\"")
                        .Replace("share\\sync_and_remove\\", "")+
                "\"];");
        }
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