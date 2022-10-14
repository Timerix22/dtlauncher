﻿using System;
using System.Diagnostics;
using System.Dynamic;
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

internal static partial class Launcher
{
    private static ConsoleLogger Info = new("launcher-logs", "launcher-info");
    private static ConsoleLogger Error = Info; //new("launcher-logs","launcher-error");
    private static Socket mainSocket;
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
            Console.Title = "Timerix's minecraft launcher";
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;
            Info.Log("g", "launcher is starting");
            PublicLog.LogEvent += Info.Log;
            if (args.Contains("debug")) debug = true;
            if (args.Contains("offline")) offline = true;
            if (args.Contains("updated")) updated = true;
            config = !File.Exists(configFileName)
                ? LauncherConfig.CreateDefault(configFileName)
                : new LauncherConfig(configFileName);

            // обновление лаунчера
            if (!updated && !offline)
            {
                Connect("updater".ToBytes(), "updater");
                mainSocket.SendPackage("requesting launcher update".ToBytes());
                FSP.DownloadFile("minecraft-launcher.exe_new");
                Info.Log("g", "minecraft-launcher.exe_new downloaded");
                if(File.Exists("minecraft-launcher.exe_old"))
                    File.Delete("minecraft-launcher.exe_old");
                System.IO.File.Move("minecraft-launcher.exe", "minecraft-launcher.exe_old");
                Process.Start("cmd","/c move minecraft-launcher.exe_new minecraft-launcher.exe && minecraft-launcher.exe updated");
                return;
            }

            // если уже обновлён
            var launcherAssembly = Assembly.GetExecutingAssembly();

            // читает текст из файлов, добавленных в сборку в виде ресурсов
            string ReadResource(string resource_path)
            {
                using var resStream = launcherAssembly.GetManifestResourceStream(resource_path)
                                      ?? throw new Exception($"can't find resource <{resource_path}>");
                using var resourceStreamReader =
                    new System.IO.StreamReader(resStream, Encoding.UTF8);
                return resourceStreamReader.ReadToEnd();
            }

            tabs.Login = ReadResource("launcher_client.gui.login.gui");
            tabs.Settings = ReadResource("launcher_client.gui.settings.gui");
            tabs.Exit = ReadResource("launcher_client.gui.exit.gui");
            tabs.Log = "";
            tabs.Current = "";
            var hasher = new Hasher();
            var password_hash = Array.Empty<byte>();
            // username
            var username = "";
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
                    case ConsoleKey.P:
                        if (tabs.Current == tabs.Login)
                        {
                            tabs.Login = tabs.Login
                                .Remove(991, 20).Insert(991,   "┏━━━━━━━━━━━━━━━━━━┓")
                                .Remove(1071, 20).Insert(1071, "┃                  ┃")
                                .Remove(1151, 20).Insert(1151, "┗━━━━━━━━━━━━━━━━━━┛");
                            RenderTab(tabs.Login);
                            var password = ReadString(33, 13, 15);
                            tabs.Login = tabs.Login
                                .Remove(991, 20).Insert(991,   "┌──────────────────┐")
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
                                             .BytesToString())["dirs"])
                                    FSP.DownloadByManifest("sync_and_remove\\" + dir,
                                        Directory.GetCurrent() + '\\' + dir, true, true);
                                Info.Log("g", "client updated");
                            }

                            config.Save();
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
        if (mainSocket!=null && mainSocket.Connected)
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
                    ">\nserver port: <", "c", $"{config.ServerPort}", "b", ">");
                var ip = Dns.GetHostAddresses(config.ServerAddress)[0];
                mainSocket.Connect(new IPEndPoint(ip, config.ServerPort));
                Info.Log("g", $"connected to server {ip}");
                break;
            }
            catch (SocketException ex)
            {
                Error.Log("r", $"{ex.Message}\n{ex.StackTrace}");
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
}