using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using DTLib;
using DTLib.Dtsod;
using DTLib.Extensions;
using DTLib.Logging;
using DTLib.Network;
using Directory = DTLib.Filesystem.Directory;
using File = DTLib.Filesystem.File;

namespace launcher_client;

internal static class Launcher
{
    private static ConsoleLogger Info = new("launcher-logs", "launcher-info");
    private static ConsoleLogger Error = Info; //new("launcher-logs","launcher-error");
    private static Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static bool debug, offline, updated;
    private static FSP FSP;
    private static dynamic tabs = new ExpandoObject();
    private static LauncherConfig config;
    private static string configFileName = "launcher.dtsod";
    public static Process gameProcess;

    private static void Main(string[] args)
    {
        try
        {
            PublicLog.LogEvent += Info.Log;
            if (args.Contains("debug")) debug = true;
            if (args.Contains("offline")) offline = true;
            if (args.Contains("updated")) updated = true;

            // обновление лаунчера
            if (!updated && !offline)
            {
                Connect("updater".ToBytes(), "updater");
                mainSocket.SendPackage("requesting launcher update".ToBytes());
                FSP.DownloadFile("launcher.exe_new");
                Info.Log("g", "launcher.exe_new downloaded");
                Process.Start("cmd",
                    "/c timeout 0 && copy launcher.exe_new launcher.exe && start launcher.exe updated && del /f launcher.exe_new");
            }

            // если уже обновлён
            else if (updated || offline)
            {
                var launcherAssembly = Assembly.GetExecutingAssembly();

                // читает текст из файлов, добавленных в сборку в виде ресурсов
                string ReadResource(string resource_path)
                {
                    using var resStream = launcherAssembly.GetManifestResourceStream(resource_path) 
                        ?? throw new Exception($"can't find resource <{resource_path}>");
                    using var resourceStreamReader =
                        new StreamReader(resStream, Encoding.UTF8);
                    return resourceStreamReader.ReadToEnd();
                }

                config = !File.Exists(configFileName)
                    ? LauncherConfig.CreateDefault(configFileName)
                    : new LauncherConfig(configFileName);
                tabs.Login = ReadResource("launcher_client.gui.login.gui");
                tabs.Settings = ReadResource("launcher_client.gui.settings.gui");
                tabs.Exit = ReadResource("launcher_client.gui.exit.gui");
                tabs.Log = "";
                tabs.Current = "";
                Console.Title = "Timerix's minecraft launcher";
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
                Console.CursorVisible = false;
                Info.Log("g", "launcher is starting");
                var hasher = new Hasher();
                var password_hash = new byte[0];
                // username
                var username = "";
                if (!config.Username.IsNullOrEmpty())
                {
                    tabs.Login = tabs.Login.Remove(833, config.Username.Length).Insert(833, config.Username);
                    username = config.Username;
                }

                RenderTab(tabs.Login);

                while (true)
                    try
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
                                    tabs.Login = tabs.Login.Remove(751, 20).Insert(751, "┏━━━━━━━━━━━━━━━━━━┓")
                                        .Remove(831, 20).Insert(831, "┃                  ┃")
                                        .Remove(911, 20).Insert(911, "┗━━━━━━━━━━━━━━━━━━┛");
                                    RenderTab(tabs.Login);
                                    var _username = ReadString(33, 10, 15);
                                    tabs.Login = tabs.Login.Remove(751, 20).Insert(751, "┌──────────────────┐")
                                        .Remove(831, 20).Insert(831, "│                  │")
                                        .Remove(911, 20).Insert(911, "└──────────────────┘");
                                    RenderTab(tabs.Login);
                                    if (_username.Length < 5)
                                        throw new Exception("username length should be > 4 and < 17");
                                    config.Username = _username;
                                    File.WriteAllText(configFileName, config.ToString());
                                    username = _username;
                                    tabs.Login = tabs.Login.Remove(833, _username.Length).Insert(833, _username);
                                    RenderTab(tabs.Login);
                                }

                                break;
                            case ConsoleKey.P:
                                if (tabs.Current == tabs.Login)
                                {
                                    tabs.Login = tabs.Login.Remove(991, 20).Insert(991, "┏━━━━━━━━━━━━━━━━━━┓")
                                        .Remove(1071, 20).Insert(1071, "┃                  ┃")
                                        .Remove(1151, 20).Insert(1151, "┗━━━━━━━━━━━━━━━━━━┛");
                                    RenderTab(tabs.Login);
                                    var password = ReadString(33, 13, 15);
                                    tabs.Login = tabs.Login.Remove(991, 20).Insert(991, "┌──────────────────┐")
                                        .Remove(1071, 20).Insert(1071, "│                  │")
                                        .Remove(1151, 20).Insert(1151, "└──────────────────┘");
                                    RenderTab(tabs.Login);
                                    if (password.Length < 8)
                                        throw new Exception("password length should be > 7 and < 17");
                                    password_hash = hasher.HashCycled(password.ToBytes(), 64);
                                    tabs.Login = tabs.Login.Remove(1073, password.Length)
                                        .Insert(1073, "*".Multiply(password.Length));
                                    RenderTab(tabs.Login);
                                }

                                break;
                            case ConsoleKey.L:
                                if (tabs.Current == tabs.Login)
                                {
                                    RenderTab(tabs.Current);
                                    if (username.Length < 5) throw new Exception("username is too short");
                                    if (password_hash.Length == 0) throw new Exception("pasword is null");
                                    // обновление клиента
                                    if (!offline)
                                    {
                                        Connect(hasher.HashCycled(username.ToBytes(), password_hash, 64), "launcher");
                                        //обновление файлов клиента
                                        Info.Log("b", "updating client...");
                                        FSP.DownloadByManifest("download_if_not_exist", Directory.GetCurrent());
                                        FSP.DownloadByManifest("sync_always", Directory.GetCurrent(), true);
                                        foreach (string dir in new DtsodV23(FSP
                                                     .DownloadFileToMemory("sync_and_remove\\dirlist.dtsod")
                                                     .ToString())["dirs"])
                                            FSP.DownloadByManifest("sync_and_remove\\" + dir,
                                                Directory.GetCurrent() + '\\' + dir, true, true);
                                        Info.Log("g", "client updated");
                                    }
                                    
                                    if (!config.UUID.IsNullOrEmpty())
                                    {
                                        Info.Log("y", "uuid not found in config. requesting from server");
                                        mainSocket.SendPackage("requesting uuid".ToBytes());
                                        var uuid = mainSocket.GetPackage().ToString();
                                        config.UUID = uuid;
                                    }

                                    File.WriteAllText(configFileName, config.ToString());
                                    // запуск майнкрафта
                                    Info.Log("g", "launching minecraft");
                                    LaunchGame(config.JavaPath, config.Username, config.UUID,
                                        config.GameMemory, config.GameWindowWidth, config.GameWindowHeight);
                                    gameProcess.WaitForExit();
                                    Info.Log("b", "minecraft closed");
                                }

                                break;
                            case ConsoleKey.R:
                                if (tabs.Current == tabs.Login && !offline)
                                {
                                    RenderTab(tabs.Current);
                                    if (username.Length < 5) throw new Exception("username is too short");
                                    if (password_hash.Length == 0) throw new Exception("pasword is null");
                                    Connect("updater".ToBytes(), "updater");
                                    mainSocket.SendPackage("register new user".ToBytes());
                                    mainSocket.GetAnswer("ready");
                                    mainSocket.SendPackage(hasher.HashCycled(username.ToBytes(), password_hash, 64));
                                    mainSocket.SendPackage(username.ToBytes());
                                    Thread.Sleep(300);
                                    Console.Write(".");
                                    Thread.Sleep(300);
                                    Console.Write(".");
                                    Thread.Sleep(300);
                                    Console.Write(".");
                                    Thread.Sleep(300);
                                    Console.Write(".");
                                    Info.Log("g", "registration request sent");
                                }

                                break;
                            case ConsoleKey.F2:
                                tabs.Log = File.ReadAllText(Info.LogfileName);
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
                        Error.Log("r", $"{ex.Message}\n{ex.StackTrace}");
                    }
            }

            else
            {
                throw new Exception($"invalid args:<{args.MergeToString(">, <")}>");
            }
        }
        catch (Exception ex)
        {
            Error.Log("r", $"{ex.Message}\n{ex.StackTrace}");
            ColoredConsole.Write("gray", "press any key to close...");
            Console.ReadKey();
        }
    }

    // подключение серверу
    private static void Connect(byte[] hash, string server_answer)
    {
        if (mainSocket.Connected)
        {
            Info.Log("y", "socket is connected already. disconnecting...");
            mainSocket.Shutdown(SocketShutdown.Both);
            mainSocket.Close();
        }

        mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        while (true)
            try
            {
                Info.Log("b", "connecting to server address: <", "c", config.ServerAddress, "b",
                    ">\nserver port: <", "c", config.ServerPort.ToString(), "b", ">");
                var ip = Dns.GetHostAddresses(config.ServerAddress)[0];
                mainSocket.Connect(new IPEndPoint(ip, config.ServerPort));
                Info.Log("g", $"connected to server {ip}");
                break;
            }
            catch (SocketException ex)
            {
                Error.Log("r",  $"{ex.Message}\n{ex.StackTrace}");
                Thread.Sleep(2000);
            }

        FSP = new FSP(mainSocket);
        FSP.debug = debug;
        /*FSP.PackageRecieved += (size) => 
        {
            Console.SetCursorPosition(0, 30);
            Info.Log("b", "downloading file... [", "c", size.ToString(), "b","/", "c", FSP.Filesize = )
        };*/
        mainSocket.ReceiveTimeout = 2500;
        mainSocket.SendTimeout = 2500;
        mainSocket.GetAnswer("requesting hash");
        mainSocket.SendPackage(hash);
        mainSocket.GetAnswer(server_answer);
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
                        string thisChar;
                        if (pressedKey.Modifiers.HasFlag(ConsoleModifiers.Shift))
                            thisChar = pressedKey.KeyChar.ToString().ToUpper();
                        else thisChar = pressedKey.KeyChar.ToString();
                        output += thisChar;
                    }

                    RenderTab(tabs.Current);
                    Console.SetCursorPosition(x, y);
                    ColoredConsole.Write("c", output);
                    break;
            }
        }
    }

    private static void LaunchGame(string javapath, string username, string uuid, int maxmemory, int width,
        int height)
    {
        gameProcess = Process.Start($"{javapath}\\javaw.exe ",
            "-Djava.net.preferIPv4Stack=true \"-Dos.name=Windows 10\" -Dos.version=10.0 " +
            $"-Xmn256M -Xmx{maxmemory}M -Djava.library.path=version\\natives -cp " +
            "libraries\\net\\minecraftforge\\forge\\1.12.2-14.23.5.2855\\forge-1.12.2-14.23.5.2855.jar;" +
            "libraries\\org\\ow2\\asm\\asm-debug-all\\5.2\\asm-debug-all-5.2.jar;" +
            "libraries\\net\\minecraft\\launchwrapper\\1.12\\launchwrapper-1.12.jar;" +
            "libraries\\org\\jline\\jline\\3.5.1\\jline-3.5.1.jar;" +
            "libraries\\com\\typesafe\\akka\\akka-actor_2.11\\2.3.3\\akka-actor_2.11-2.3.3.jar;" +
            "libraries\\com\\typesafe\\config\\1.2.1\\config-1.2.1.jar;" +
            "libraries\\org\\scala-lang\\scala-actors-migration_2.11\\1.1.0\\scala-actors-migration_2.11-1.1.0.jar;" +
            "libraries\\org\\scala-lang\\scala-compiler\\2.11.1\\scala-compiler-2.11.1.jar;" +
            "libraries\\org\\scala-lang\\plugins\\scala-continuations-library_2.11\\1.0.2_mc\\scala-continuations-library_2.11-1.0.2_mc.jar;l" +
            "ibraries\\org\\scala-lang\\plugins\\scala-continuations-plugin_2.11.1\\1.0.2_mc\\scala-continuations-plugin_2.11.1-1.0.2_mc.jar;" +
            "libraries\\org\\scala-lang\\scala-library\\2.11.1\\scala-library-2.11.1.jar;" +
            "libraries\\org\\scala-lang\\scala-parser-combinators_2.11\\1.0.1\\scala-parser-combinators_2.11-1.0.1.jar;" +
            "libraries\\org\\scala-lang\\scala-reflect\\2.11.1\\scala-reflect-2.11.1.jar;" +
            "libraries\\org\\scala-lang\\scala-swing_2.11\\1.0.1\\scala-swing_2.11-1.0.1.jar;" +
            "libraries\\org\\scala-lang\\scala-xml_2.11\\1.0.2\\scala-xml_2.11-1.0.2.jar;" +
            "libraries\\lzma\\lzma\\0.0.1\\lzma-0.0.1.jar;" +
            "libraries\\java3d\\vecmath\\1.5.2\\vecmath-1.5.2.jar;" +
            "libraries\\net\\sf\\trove4j\\trove4j\\3.0.3\\trove4j-3.0.3.jar;" +
            "libraries\\org\\apache\\maven\\maven-artifact\\3.5.3\\maven-artifact-3.5.3.jar;" +
            "libraries\\net\\sf\\jopt-simple\\jopt-simple\\5.0.3\\jopt-simple-5.0.3.jar;" +
            "libraries\\oshi-project\\oshi-core\\1.1\\oshi-core-1.1.jar;" +
            "libraries\\net\\java\\dev\\jna\\jna\\4.4.0\\jna-4.4.0.jar;" +
            "libraries\\net\\java\\dev\\jna\\platform\\3.4.0\\platform-3.4.0.jar;" +
            "libraries\\com\\ibm\\icu\\icu4j-core-mojang\\51.2\\icu4j-core-mojang-51.2.jar;" +
            "libraries\\net\\sf\\jopt-simple\\jopt-simple\\5.0.3\\jopt-simple-5.0.3.jar;" +
            "libraries\\com\\paulscode\\codecjorbis\\20101023\\codecjorbis-20101023.jar;" +
            "libraries\\com\\paulscode\\codecwav\\20101023\\codecwav-20101023.jar;" +
            "libraries\\com\\paulscode\\libraryjavasound\\20101123\\libraryjavasound-20101123.jar;" +
            "libraries\\com\\paulscode\\librarylwjglopenal\\20100824\\librarylwjglopenal-20100824.jar;" +
            "libraries\\com\\paulscode\\soundsystem\\20120107\\soundsystem-20120107.jar;" +
            "libraries\\io\\netty\\netty-all\\4.1.9.Final\\netty-all-4.1.9.Final.jar;" +
            "libraries\\com\\google\\guava\\guava\\21.0\\guava-21.0.jar;" +
            "libraries\\org\\apache\\commons\\commons-lang3\\3.5\\commons-lang3-3.5.jar;" +
            "libraries\\commons-io\\commons-io\\2.5\\commons-io-2.5.jar;" +
            "libraries\\commons-codec\\commons-codec\\1.10\\commons-codec-1.10.jar;" +
            "libraries\\net\\java\\jinput\\jinput\\2.0.5\\jinput-2.0.5.jar;" +
            "libraries\\net\\java\\jutils\\jutils\\1.0.0\\jutils-1.0.0.jar;" +
            "libraries\\com\\google\\code\\gson\\gson\\2.8.0\\gson-2.8.0.jar;" +
            "libraries\\com\\mojang\\authlib\\1.5.25\\authlib-1.5.25.jar;" +
            "libraries\\com\\mojang\\realms\\1.10.22\\realms-1.10.22.jar;" +
            "libraries\\org\\apache\\commons\\commons-compress\\1.8.1\\commons-compress-1.8.1.jar;" +
            "libraries\\org\\apache\\httpcomponents\\httpclient\\4.3.3\\httpclient-4.3.3.jar;" +
            "libraries\\commons-logging\\commons-logging\\1.1.3\\commons-logging-1.1.3.jar;" +
            "libraries\\org\\apache\\httpcomponents\\httpcore\\4.3.2\\httpcore-4.3.2.jar;" +
            "libraries\\it\\unimi\\dsi\\fastutil\\7.1.0\\fastutil-7.1.0.jar;" +
            "libraries\\org\\apache\\logging\\log4j\\log4j-api\\2.8.1\\log4j-api-2.8.1.jar;" +
            "libraries\\org\\apache\\logging\\log4j\\log4j-core\\2.8.1\\log4j-core-2.8.1.jar;" +
            "libraries\\org\\lwjgl\\lwjgl\\lwjgl\\2.9.4-nightly-20150209\\lwjgl-2.9.4-nightly-20150209.jar;" +
            "libraries\\org\\lwjgl\\lwjgl\\lwjgl_util\\2.9.4-nightly-20150209\\lwjgl_util-2.9.4-nightly-20150209.jar;" +
            "libraries\\com\\mojang\\text2speech\\1.10.3\\text2speech-1.10.3.jar;" +
            "version\\1.12.2-forge-14.23.5.2855.jar " +
            $"-Dminecraft.applet.TargetDirectory=.\\ " +
            "-Dfml.ignoreInvalidMinecraftCertificates=true -Dfml.ignorePatchDiscrepancies=true " +
            $"net.minecraft.launchwrapper.Launch --username {username} --version 1.12.2-forge-14.23.5.2855 " +
            $"--gameDir .\\ --assetsDir assets --assetIndex 1.12 " +
            $"--uuid {uuid} --accessToken null --userType mojang --tweakClass net.minecraftforge.fml.common.launcher.FMLTweaker " +
            $"--versionType Forge --width {width} --height {height}");
    }
}