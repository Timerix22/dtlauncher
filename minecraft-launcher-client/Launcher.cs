using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using DTLib.Console;
using DTLib.Dtsod;
using DTLib.Extensions;
using DTLib.Logging;
using DTLib.Network;
using DTLib.Filesystem;
using Directory = DTLib.Filesystem.Directory;
using File = DTLib.Filesystem.File;

namespace launcher_client;

internal static partial class Launcher
{
    private static FileLogger _fileLogger = new("launcher-logs", "launcher_client");
    private static ILogger logger = new CompositeLogger(
        _fileLogger,
        new ConsoleLogger());
    private static Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static bool debug, offline, updated;
    private static FSP FSP = new(mainSocket);
    private static dynamic tabs = new ExpandoObject();
    private static LauncherConfig config = null!;

    private static void Main(string[] args)
    {
        try
        {
            Console.Title = "anarx_2";
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;

#if  DEBUG
            debug = true;
#else
            if (args.Contains("debug")) debug = true;
#endif
            if (args.Contains("offline")) offline = true;
            if (args.Contains("updated")) updated = true;
            config = !File.Exists(LauncherConfig.ConfigFilePath)
                ? LauncherConfig.CreateDefault()
                : LauncherConfig.LoadFromFile();
            
            DTLibInternalLogging.SetLogger(logger);
            logger.DebugLogEnabled = debug;
            logger.LogInfo("Main", "launcher is starting");
            
            if(File.Exists("minecraft-launcher.exe_old"))
                File.Delete("minecraft-launcher.exe_old");
            
            // обновление лаунчера
            if (!updated && !offline)
            {
                ConnectToLauncherServer();
                mainSocket.SendPackage("requesting launcher update");
                FSP.DownloadFile("minecraft-launcher.exe_new");
                logger.LogInfo("Main", "minecraft-launcher.exe_new downloaded");
                System.IO.File.Move("minecraft-launcher.exe", "minecraft-launcher.exe_old");
                Process.Start("cmd","/c " +
                            "move minecraft-launcher.exe_new minecraft-launcher.exe && " +
                            "minecraft-launcher.exe updated");
                return;
            }

            // если уже обновлён
            tabs.Login = EmbeddedResources.ReadText("launcher_client.gui.login.gui");
            tabs.Settings = EmbeddedResources.ReadText("launcher_client.gui.settings.gui");
            tabs.Exit = EmbeddedResources.ReadText("launcher_client.gui.exit.gui");
            tabs.Log = "";
            tabs.Current = "";
            string username = "";
            if (!config.Username.IsNullOrEmpty())
            {
                tabs.Login = tabs.Login.Remove(833, config.Username.Length).Insert(833, config.Username);
                username = config.Username;
            }

            RenderTab(tabs.Login);

            while (true) try
                    // ReSharper disable once BadChildStatementIndent
            {
                var pressedKey = Console.ReadKey(true); // Считывание ввода
                switch (pressedKey.Key)
                {
                    case ConsoleKey.F1:
                        RenderTab(tabs.Login);
                        break;
                    case ConsoleKey.N:
                        if (tabs.Current == tabs.Login)
                        {
                            tabs.Login = tabs.Login
                                .Remove(751, 20).Insert(751, "┏━━━━━━━━━━━━━━━━━━┓")
                                .Remove(831, 20).Insert(831, "┃                  ┃")
                                .Remove(911, 20).Insert(911, "┗━━━━━━━━━━━━━━━━━━┛");
                            RenderTab(tabs.Login);
                            var _username = ReadString(33, 10, 15);
                            tabs.Login = tabs.Login
                                .Remove(751, 20).Insert(751, "┌──────────────────┐")
                                .Remove(831, 20).Insert(831, "│                  │")
                                .Remove(911, 20).Insert(911, "└──────────────────┘");
                            RenderTab(tabs.Login);
                            if (_username.Length < 5)
                                throw new Exception("username length should be > 4 and < 17");
                            config.Username = _username;
                            config.Save();
                            username = _username;
                            tabs.Login = tabs.Login.Remove(833, _username.Length).Insert(833, _username);
                            RenderTab(tabs.Login);
                        }
                        break;
                    case ConsoleKey.L:
                        if (tabs.Current == tabs.Login)
                        {
                            RenderTab(tabs.Current);
                            if (username.Length < 2) throw new Exception("username is too short");
                            
                            // обновление клиента
                            if (!offline)
                            {
                                ConnectToLauncherServer();
                                //обновление файлов клиента
                                logger.LogInfo("Main", "updating client...");
                                FSP.DownloadByManifest("download_if_not_exist", Directory.GetCurrent());
                                FSP.DownloadByManifest("sync_always", Directory.GetCurrent(), true);
                                foreach (string dir in new DtsodV23(FSP
                                             .DownloadFileToMemory("sync_and_remove\\dirlist.dtsod")
                                             .BytesToString())["dirs"])
                                    FSP.DownloadByManifest("sync_and_remove\\" + dir,
                                        Directory.GetCurrent() + '\\' + dir, true, true);
                                logger.LogInfo("Main", "client updated");
                            }

                            // запуск майнкрафта
                            logger.LogInfo("Main", "launching minecraft");
                            string gameOptions = ConstructGameLaunchArgs(config.Username, 
                                NameUUIDFromString("OfflinePlayer:" + config.Username),
                                config.GameMemory, 
                                config.GameWindowWidth, 
                                config.GameWindowHeight,
                                Directory.GetCurrent());
                            logger.LogDebug("LaunchGame", gameOptions);
                            var gameProcess = Process.Start($"{config.JavaPath}\\java.exe", gameOptions);
                            gameProcess.WaitForExit();
                            logger.LogInfo("Main", "minecraft closed");
                        }
                        break;
                    case ConsoleKey.F2:
                        tabs.Log = File.ReadAllText(_fileLogger.LogfileName);
                        RenderTab(tabs.Log, 9999);
                        break;
                    case ConsoleKey.F3:
                        RenderTab(tabs.Settings);
                        break;
                    case ConsoleKey.F4:
                        RenderTab(tabs.Exit);
                        break;
                    case ConsoleKey.Enter:
                        if (tabs.Current == tabs.Exit)
                        {
                            Console.Clear();
                            Console.BufferHeight = 9999;
                            return;
                        }
                        break;
                    case ConsoleKey.F5:
                        if (tabs.Current == tabs.Log) goto case ConsoleKey.F2;
                        RenderTab(tabs.Current);
                        Console.CursorVisible = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Main", ex);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Main", ex);
            ColoredConsole.Write("gray", "press any key to close...");
            Console.ReadKey();
        }
    }

    // подключение серверу
    private static void ConnectToLauncherServer()
    {
        if (mainSocket.Connected)
        {
            logger.LogInfo(nameof(ConnectToLauncherServer), "socket is connected already. disconnecting...");
            mainSocket.Shutdown(SocketShutdown.Both);
            mainSocket.Close();
            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            FSP = new FSP(mainSocket);
            FSP.debug = debug;
        }

        while (true)
            try
            {
                logger.LogInfo(nameof(ConnectToLauncherServer), $"connecting to server {config.ServerAddress}:{config.ServerPort}");
                var ip = Dns.GetHostAddresses(config.ServerAddress)[0];
                mainSocket.Connect(new IPEndPoint(ip, config.ServerPort));
                logger.LogInfo(nameof(ConnectToLauncherServer), $"connected to server {ip}");
                break;
            }
            catch (SocketException ex)
            {
                logger.LogError(nameof(ConnectToLauncherServer), ex);
                Thread.Sleep(2000);
            }

        mainSocket.ReceiveTimeout = 2500;
        mainSocket.SendTimeout = 2500;
        mainSocket.GetAnswer("requesting user name");
        mainSocket.SendPackage("minecraft-launcher");
        mainSocket.GetAnswer("minecraft-launcher OK");
    }

    private static void RenderTab(string tab, ushort bufferHeight = 30)
    {
        tabs.Current = tab;
        Console.Clear();
        Console.SetWindowSize(80, 30);
        Console.SetBufferSize(80, bufferHeight);
        ColoredConsole.Write("w", tab);
    }

    private static string ReadString(ushort x, ushort y, ushort maxlength)
    {
        var output = "";
        tabs.Current = tabs.Current.Remove(y * 80 + x, maxlength).Insert(y * 80 + x, " ".Multiply(maxlength));
        while (true)
        {
            var pressedKey = Console.ReadKey(false);
            switch (pressedKey.Key)
            {
                case ConsoleKey.Enter:
                    return output;
                case ConsoleKey.Backspace:
                    if (output.Length > 0)
                    {
                        output = output.Remove(output.Length - 1);
                        RenderTab(tabs.Current);
                        Console.SetCursorPosition(x, y);
                        ColoredConsole.Write("c", output);
                    }

                    break;
                case ConsoleKey.Escape:
                    tabs.Current = tabs.Current.Remove(y * 80 + x, maxlength)
                        .Insert(y * 80 + x, " ".Multiply(maxlength));
                    RenderTab(tabs.Current);
                    return "";
                //case ConsoleKey.Spacebar:
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                    break;
                default:
                    if (output.Length <= maxlength)
                    {
                        string keyC = pressedKey.KeyChar.ToString();
                        string thisChar = pressedKey.Modifiers.HasFlag(ConsoleModifiers.Shift) ? keyC.ToUpper() : keyC;
                        output += thisChar;
                    }

                    RenderTab(tabs.Current);
                    Console.SetCursorPosition(x, y);
                    ColoredConsole.Write("c", output);
                    break;
            }
        }
    }

    //minecraft player uuid explanation
    //https://gist.github.com/CatDany/0e71ca7cd9b42a254e49/
    //java uuid generation in c#
    //https://stackoverflow.com/questions/18021808/uuid-interop-with-c-sharp-code
    public static string NameUUIDFromString(string input)
        => NameUUIDFromBytes(Encoding.UTF8.GetBytes(input));
    
    public static string NameUUIDFromBytes(byte[] input)
    {
        byte[] hash = MD5.HashData(input);
        hash[6] &= 0x0f;
        hash[6] |= 0x30;
        hash[8] &= 0x3f;
        hash[8] |= 0x80;
        string hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
        return hex.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
    }
}