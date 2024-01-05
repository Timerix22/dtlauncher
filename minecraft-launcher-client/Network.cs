using System.Net;
using System.Net.Sockets;
using System.Threading;
using DTLib;
using DTLib.Dtsod;
using DTLib.Extensions;
using DTLib.Filesystem;
using DTLib.Logging;
using DTLib.Network;
using static launcher_client.Launcher;

namespace launcher_client;

public class Network
{
    public static Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static FSP Fsp = new(mainSocket);

    // подключение серверу
    public static void ConnectToLauncherServer()
    {
        if (mainSocket.Connected)
        {
            Logger.LogInfo(nameof(ConnectToLauncherServer), "socket is connected already. disconnecting...");
            mainSocket.Shutdown(SocketShutdown.Both);
            mainSocket.Close();
            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Fsp = new(mainSocket);
        }

        while (true)
            try
            {
                Logger.LogInfo(nameof(ConnectToLauncherServer), $"connecting to server {Config.ServerAddress}:{Config.ServerPort}");
                var ip = Dns.GetHostAddresses(Config.ServerAddress)[0];
                mainSocket.Connect(new IPEndPoint(ip, Config.ServerPort));
                Logger.LogInfo(nameof(ConnectToLauncherServer), $"connected to server {ip}");
                break;
            }
            catch (SocketException ex)
            {
                Logger.LogError(nameof(ConnectToLauncherServer), ex);
                Thread.Sleep(2000);
            }

        mainSocket.ReceiveTimeout = 2500;
        mainSocket.SendTimeout = 2500;
        mainSocket.GetAnswer("requesting user name");
        mainSocket.SendPackage("minecraft-launcher");
        mainSocket.GetAnswer("minecraft-launcher OK");
    }
    
    public static void DownloadByManifest(IOPath dirOnServer, IOPath dirOnClient, bool overwrite = false, bool delete_excess = false)
    {
        var manifestPath = Path.Concat(dirOnServer, "manifest.dtsod");
        Logger.LogDebug(nameof(DownloadByManifest), manifestPath);
        string manifestContent = Fsp.DownloadFileToMemory(manifestPath).BytesToString();
        var manifest = new DtsodV23(manifestContent);
        var hasher = new Hasher();
        foreach (var fileOnServerData in manifest)
        {
            IOPath fileOnClient = Path.Concat(dirOnClient, fileOnServerData.Key);
            if (!File.Exists(fileOnClient) || (overwrite && hasher.HashFile(fileOnClient).HashToString() != fileOnServerData.Value))
                Fsp.DownloadFile(Path.Concat(dirOnServer, fileOnServerData.Key), fileOnClient);
        }
        // удаление лишних файлов
        if (delete_excess)
        {
            foreach (var file in Directory.GetAllFiles(dirOnClient))
            {
                if (!manifest.ContainsKey(file.RemoveBase(dirOnClient).Str.Replace('\\','/'))) 
                    File.Delete(file);
            }
        }
    }
}