using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DTLib;
using DTLib.Dtsod;
using DTLib.Extensions;
using DTLib.Filesystem;
using DTLib.Logging;
using DTLib.Network;

namespace launcher_server;

static class Server
{
    private static ConsoleLogger Info = new("logs","info");
    private static ConsoleLogger Error = new("logs","error");
    static readonly Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    static DtsodV23 config;
    static bool debug;

    static object manifestLocker = new();

    static void Main(string[] args)
    {
        try
        {
            Console.Title = "minecraft_launcher_server";
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;
            PublicLog.LogEvent += Info.Log;
            config = new DtsodV23(File.ReadAllText("launcher-server.dtsod"));
            if (args.Contains("debug")) debug = true;
            Info.Log("b", "local address: <", "c", config["local_ip"], "b",
                ">\npublic address: <", "c", OldNetwork.GetPublicIP(), "b",
                ">\nport: <", "c", config["local_port"].ToString(), "b", ">");
            mainSocket.Bind(new IPEndPoint(IPAddress.Parse(config["local_ip"]), config["local_port"]));
            mainSocket.Listen(1000);
            CreateManifestы();
            Info.Log("g", "server started succesfully");
            // запуск отдельного потока для каждого юзера
            Info.Log("b", "waiting for users");
            while (true)
            {
                var userSocket = mainSocket.Accept();
                var userThread = new Thread(obj => UserHandle((Socket)obj));
                userThread.Start(userSocket);
            }
        }
        catch (Exception ex)
        {
            Error.Log("r", $"{ex.Message}\n{ex.StackTrace}");
            mainSocket.Close();
        }
        Info.Log("gray", "");
    }

    // запускается для каждого юзера в отдельном потоке
    static void UserHandle(Socket handlerSocket)
    {
        Info.Log("b", "user connecting...  ");
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
                Info.Log("b", "user is ", "c", "updater");
                handlerSocket.SendPackage("updater".ToBytes());
                // обработка запросов
                while (true)
                {
                    if (handlerSocket.Available >= 2)
                    {
                        string request = handlerSocket.GetPackage().BytesToString();
                        switch (request)
                        {
                            case "requesting launcher update":
                                Info.Log("c","updater", "b", " requested launcher update");
                                fsp.UploadFile("share\\minecraft-launcher.exe");
                                break;
                            case "requesting file download":
                                var file = handlerSocket.GetPackage().BytesToString();
                                Info.Log("b", "user ", "c","updater", "b", " requested file ", "c", file + "");
                                fsp.UploadFile($"share\\{file}");
                                break;
                            default:
                                throw new Exception("unknown request: " + request);
                        }
                    }
                    else Thread.Sleep(10);
                }
            }
            // неизвестный юзер

            Error.Log("y", $"user with hash <{hash.HashToString()}> not found");
            handlerSocket.SendPackage("user not found".ToBytes());
        }
        catch (Exception ex)
        {
            Error.Log("y", $"{ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            if (handlerSocket.Connected) handlerSocket.Shutdown(SocketShutdown.Both);
            handlerSocket.Close();
            Info.Log("g", "user disconnected");
        }
    }

    static void CreateManifestы()
    {
        lock (manifestLocker)
        {
            FSP.CreateManifest("share\\download_if_not_exist");
            FSP.CreateManifest("share\\sync_always");
            if(!Directory.Exists("share\\sync_and_remove"))
            {
                Directory.Create("share\\sync_and_remove");
                Info.Log("y", "can't create manifest, dir <share\\sync_and_remove> doesn't exist");
            }
            else foreach (string dir in Directory.GetDirectories("share\\sync_and_remove"))
                FSP.CreateManifest(dir);
            if(Directory.GetDirectories("share\\sync_and_remove").Length==0)
                File.WriteAllText("share\\sync_and_remove\\dirlist.dtsod", "dirs: [ ];");
            else File.WriteAllText("share\\sync_and_remove\\dirlist.dtsod",
                "dirs: [\""
                +Directory.GetDirectories("share\\sync_and_remove")
                    .MergeToString("\", \"").Replace("share\\sync_and_remove\\", "")
                +"\"];");
        }
    }
}