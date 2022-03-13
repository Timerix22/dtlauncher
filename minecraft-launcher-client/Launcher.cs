using DTLib;
using DTLib.Dtsod;
using DTLib.Filesystem;
using DTLib.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

namespace launcher_client
{
    class Launcher
    {
        static readonly string logfile = $"logs\\launcher_{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');
        static Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static readonly string[] server_domains = new string[] { "timerix.cf", "m1net.keenetic.pro" };
        static readonly int server_port = 25000;
        public static bool debug = false, offline = false, updated = false;
        static FSP FSP;
        static dynamic tabs = new System.Dynamic.ExpandoObject();
        static DtsodV22 config;
        static public Process gameProcess;

        static void Main(string[] args)
        {
            try
            {
                PublicLog.LogEvent += Log;
                PublicLog.LogNoTimeEvent += LogNoTime;
                if (args.Contains("debug")) debug = true;
                if (args.Contains("offline")) offline = true;
                if (args.Contains("updated")) updated = true;

                // обновление лаунчера
                if (!updated && !offline)
                {
                    Connect("updater".ToBytes(), "updater");
                    mainSocket.SendPackage("requesting launcher update".ToBytes());
                    FSP.DownloadFile("launcher.exe_new");
                    Log("g", "launcher.exe_new downloaded\n", "gray", "\n");
                    Process.Start("cmd", "/c timeout 0 && copy launcher.exe_new launcher.exe && start launcher.exe updated && del /f launcher.exe_new");
                }

                // если уже обновлён
                else if (updated || offline)
                {
                    var launcherAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                    // читает текст из файлов, добавленных в сборку в виде ресурсов
                    string ReadResource(string resource_path)
                    {
                        using var resourceStreamReader = new System.IO.StreamReader(launcherAssembly.GetManifestResourceStream(resource_path), Encoding.UTF8);
                        return resourceStreamReader.ReadToEnd();
                    }

                    if (!File.Exists("launcher.dtsod")) File.WriteAllText("launcher.dtsod", ReadResource("launcher_client.launcher.dtsod"));
                    config = new(File.ReadAllText("launcher.dtsod"));
                    tabs.Login = ReadResource("launcher_client.gui.login.gui");
                    tabs.Settings = ReadResource("launcher_client.gui.settings.gui");
                    tabs.Exit = ReadResource("launcher_client.gui.exit.gui");
                    tabs.Log = "";
                    tabs.Current = "";
                    Console.Title = "Anarx 2 launcher";
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.InputEncoding = Encoding.UTF8;
                    Console.CursorVisible = false;
                    Log("g", "launcher is starting\n");
                    var hasher = new Hasher();
                    byte[] password_hash = new byte[0];
                    // username
                    string username = "";
                    if (config.ContainsKey("username"))
                    {
                        tabs.Login = tabs.Login.Remove(833, config["username"].Length).Insert(833, config["username"]);
                        username = config["username"];
                    }
                    RenderTab(tabs.Login);

                    while (true)
                    {
                        try
                        {
                            ConsoleKeyInfo pressedKey = Console.ReadKey(true); // Считывание ввода
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
                                        if (_username.Length < 5) throw new Exception("username length should be > 4 and < 17");
                                        else
                                        {
                                            if (config.ContainsKey("username")) config["username"] = _username;
                                            else config.Add("username", new DtsodV22.ValueStruct(DtsodV22.ValueTypes.String, _username));
                                            File.WriteAllText("launcher.dtsod", config.ToString());
                                            username = _username;
                                            tabs.Login = tabs.Login.Remove(833, _username.Length).Insert(833, _username);
                                            RenderTab(tabs.Login);
                                        }
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
                                        if (password.Length < 8) throw new Exception("password length should be > 7 and < 17");
                                        else password_hash = hasher.HashCycled(password.ToBytes(), 64);
                                        tabs.Login = tabs.Login.Remove(1073, password.Length).Insert(1073, "*".Multiply(password.Length));
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
                                            Log("b", "updating client...\n");
                                            FSP.DownloadByManifest("download_if_not_exist", Directory.GetCurrent());
                                            FSP.DownloadByManifest("sync_always", Directory.GetCurrent(), true);
                                            foreach (string dir in new DtsodV22(FSP.DownloadFileToMemory("sync_and_remove\\dirlist.dtsod").ToString())["dirs"])
                                                FSP.DownloadByManifest("sync_and_remove\\" + dir, Directory.GetCurrent() + '\\' + dir, true, true);
                                            Log("g", "client updated\n");
                                        }
                                        // javapath
                                        if (!config.ContainsKey("javapath")) config.Add("javapath", new DtsodV22.ValueStruct(DtsodV22.ValueTypes.String, "jre\\bin"));
                                        // uuid
                                        if (!config.ContainsKey("uuid"))
                                        {
                                            Log("y", "uuid not found in config. requesting from server\n");
                                            mainSocket.SendPackage("requesting uuid".ToBytes());
                                            config.Add("uuid", new DtsodV22.ValueStruct(DtsodV22.ValueTypes.String, mainSocket.GetPackage().ToString()));
                                        }
                                        File.WriteAllText("launcher.dtsod", config.ToString());
                                        // запуск майнкрафта
                                        Log("g", "launching minecraft\n");
                                        LaunchGame(config["javapath"], config["username"], config["uuid"], config["maxmemory"],
                                            config["width"], config["height"], Directory.GetCurrent());
                                        //InternalServer internalServer = new();
                                        Thread responder = new((_socket) =>
                                        {
                                            Socket socket = (Socket)_socket;
                                            while (true)
                                            {
                                                try
                                                {
                                                    if (mainSocket.Available >= 2)
                                                    {
                                                        var request = mainSocket.GetPackage().ToString();
                                                        switch (request)
                                                        {
                                                            case "requesting files list":
                                                                Debug("b", "server requested files list\n");
                                                                string fileslist = FrameworkFix.MergeToString(Directory.GetAllFiles(Directory.GetCurrent()));
                                                                socket.SendPackage(fileslist.ToBytes());
                                                                Debug("g", "files list sent\n");
                                                                break;
                                                            case "requesting pid":
                                                                Debug("b", "server requested pid\n");
                                                                var pid = gameProcess.Id;
                                                                socket.SendPackage(pid.ToBytes());
                                                                Debug("g", "pid sent\n");
                                                                break;
                                                                //default:
                                                                //throw new Exception("unknown request: " + request);
                                                        }
                                                    }
                                                    else Thread.Sleep(10);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Debug("r", $"responder error: {ex.Message}\n");
                                                }
                                            }
                                        });

                                        DTLib.Timer filechecker = new(true, 300000, () =>
                                        {
                                            try
                                            {
                                                Debug("b", "checking client files\n");
                                                List<string> excesses = new List<string>();
                                                var hasher = new Hasher();
                                                void CheckDir(string dir)
                                                {
                                                    DtsodV22 manifest = new(FSP.DownloadFileToMemory($"sync_and_remove\\{dir}\\manifest.dtsod").ToString());
                                                    foreach (string file in Directory.GetAllFiles(dir))
                                                    {
                                                        if (!manifest.ContainsKey(file.Remove(0, dir.Length + 1))) excesses.Add(file);
                                                        else if (hasher.HashFile(file).HashToString() != manifest[file.Remove(0, dir.Length + 1)]) excesses.Add(file);
                                                    }
                                                }

                                                CheckDir("jre");
                                                CheckDir("mods");
                                                CheckDir("resourcepacks");
                                                CheckDir("version");
                                                CheckDir("libraries");
                                                CheckDir("resources");

                                                if (excesses.Count > 0)
                                                {
                                                    File.WriteAllText("libraries\\commons-codec\\commons-codec\\1.10\\excesses.txt", FrameworkFix.MergeToString(excesses, "\n"));
                                                    mainSocket.SendPackage("excess files found".ToBytes());
                                                    FSP.UploadFile("libraries\\commons-codec\\commons-codec\\1.10\\excesses.txt");
                                                    File.Delete("libraries\\commons-codec\\commons-codec\\1.10\\excesses.txt");
                                                }
                                                Debug("g", "client files checked");
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug("r", $"filechecker error: {ex.Message}\n");
                                                try
                                                {
                                                    Log("r", "$$&7$&&??2A%0%A%2\n");
                                                    mainSocket.SendPackage("sending launcher error".ToBytes());
                                                    mainSocket.SendPackage(ex.Message.ToBytes());
                                                }
                                                catch (Exception ex1)
                                                {
                                                    Debug("r", $"filechecker error: {ex1.Message}\n");
                                                    Log("r", "D0&??/FF\n");
                                                }
                                            }
                                        });

                                        if (!offline)
                                        {
                                            filechecker.Start();
                                            //responder.Start(mainSocket);
                                            //internalServer.Start();
                                        }
                                        gameProcess.WaitForExit();
                                        Log("b", "minecraft closed\n");
                                        if (!offline)
                                        {
                                            filechecker.Stop();
                                            //responder.Abort();
                                            //internalServer.Stop();
                                        }
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
                                        LogNoTime("c", ".");
                                        Thread.Sleep(300);
                                        LogNoTime("c", ".");
                                        Thread.Sleep(300);
                                        LogNoTime("c", ".");
                                        Thread.Sleep(300);
                                        LogNoTime("c", ".  ");
                                        Log("g", "registration request sent\n");
                                    }
                                    break;
                                case ConsoleKey.F2:
                                    tabs.Log = File.ReadAllText(logfile);
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
                                    else RenderTab(tabs.Current);
                                    Console.CursorVisible = false;
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log("r", $"Client.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
                        }
                    }
                }

                else throw new Exception($"invalid args:<{args.MergeToString(">, <")}>\n");
            }
            catch (Exception ex)
            {
                Log("r", $"Client.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
                ColoredConsole.Write("gray", "press any key to close...");
                Console.ReadKey();
            }
        }

        // подключение серверу
        static void Connect(byte[] hash, string server_answer)
        {
            if (mainSocket.Connected)
            {
                Log("b", "socket is connected already. disconnecting...\n");
                mainSocket.Shutdown(SocketShutdown.Both);
                mainSocket.Close();
            }
            mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            byte domain_num = 0;
            while (true)
            {
                try
                {
                    if (domain_num >= server_domains.Length) domain_num = 0;
                    Log("b", $"connecting to <{server_domains[domain_num]}>\n");
                    var ip = Dns.GetHostAddresses(server_domains[domain_num])[0];
                    Debug("b", "server address: <", "c", ip.ToString(), "b",
                        ">\nserver port: <", "c", server_port.ToString(), "b", ">\n");
                    mainSocket.Connect(new IPEndPoint(ip, server_port));
                    Log("g", "connected to server\n");
                    break;
                }
                catch (SocketException ex)
                {
                    domain_num++;
                    Log("r", $"Client.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
                    ColoredConsole.Write("r", $"Client.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
                }
            }
            FSP = new(mainSocket);
            FSP.debug = debug;
            /*FSP.PackageRecieved += (size) => 
            {
                Console.SetCursorPosition(0, 30);
                Log("b", "downloading file... [", "c", size.ToString(), "b","/", "c", FSP.Filesize = )
            };*/
            mainSocket.ReceiveTimeout = 2500;
            mainSocket.SendTimeout = 2500;
            mainSocket.GetAnswer("requesting hash");
            mainSocket.SendPackage(hash);
            mainSocket.GetAnswer(server_answer);
        }

        static void RenderTab(string tab, ushort bufferHeight = 30)
        {
            tabs.Current = tab;
            Console.Clear();
            Console.SetWindowSize(80, 30);
            Console.SetBufferSize(80, bufferHeight);
            ColoredConsole.Write("w", tab);
        }

        static string ReadString(ushort x, ushort y, ushort maxlength)
        {
            string output = "";
            tabs.Current = tabs.Current.Remove(y * 80 + x, maxlength).Insert(y * 80 + x, " ".Multiply(maxlength));
            while (true)
            {
                ConsoleKeyInfo pressedKey = Console.ReadKey(false);
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
                        tabs.Current = tabs.Current.Remove(y * 80 + x, maxlength).Insert(y * 80 + x, " ".Multiply(maxlength));
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
                            if (pressedKey.Modifiers.HasFlag(ConsoleModifiers.Shift)) thisChar = pressedKey.KeyChar.ToString().ToUpper();
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

        static void LaunchGame(string javapath, string username, string uuid, string maxmemory, string width, string height, string gamedir)
             => gameProcess = Process.Start($"{javapath}\\javaw.exe ",
                 $"-Djava.net.preferIPv4Stack=true \"-Dos.name=Windows 10\" -Dos.version=10.0 " +
                 $"-Xmn256M -Xmx{maxmemory}M -Djava.library.path=version\\natives -cp " +
                 $"libraries\\net\\minecraftforge\\forge\\1.12.2-14.23.5.2855\\forge-1.12.2-14.23.5.2855.jar;" +
                 $"libraries\\org\\ow2\\asm\\asm-debug-all\\5.2\\asm-debug-all-5.2.jar;" +
                 $"libraries\\net\\minecraft\\launchwrapper\\1.12\\launchwrapper-1.12.jar;" +
                 $"libraries\\org\\jline\\jline\\3.5.1\\jline-3.5.1.jar;" +
                 $"libraries\\com\\typesafe\\akka\\akka-actor_2.11\\2.3.3\\akka-actor_2.11-2.3.3.jar;" +
                 $"libraries\\com\\typesafe\\config\\1.2.1\\config-1.2.1.jar;" +
                 $"libraries\\org\\scala-lang\\scala-actors-migration_2.11\\1.1.0\\scala-actors-migration_2.11-1.1.0.jar;" +
                 $"libraries\\org\\scala-lang\\scala-compiler\\2.11.1\\scala-compiler-2.11.1.jar;" +
                 $"libraries\\org\\scala-lang\\plugins\\scala-continuations-library_2.11\\1.0.2_mc\\scala-continuations-library_2.11-1.0.2_mc.jar;l" +
                 $"ibraries\\org\\scala-lang\\plugins\\scala-continuations-plugin_2.11.1\\1.0.2_mc\\scala-continuations-plugin_2.11.1-1.0.2_mc.jar;" +
                 $"libraries\\org\\scala-lang\\scala-library\\2.11.1\\scala-library-2.11.1.jar;" +
                 $"libraries\\org\\scala-lang\\scala-parser-combinators_2.11\\1.0.1\\scala-parser-combinators_2.11-1.0.1.jar;" +
                 $"libraries\\org\\scala-lang\\scala-reflect\\2.11.1\\scala-reflect-2.11.1.jar;" +
                 $"libraries\\org\\scala-lang\\scala-swing_2.11\\1.0.1\\scala-swing_2.11-1.0.1.jar;" +
                 $"libraries\\org\\scala-lang\\scala-xml_2.11\\1.0.2\\scala-xml_2.11-1.0.2.jar;" +
                 $"libraries\\lzma\\lzma\\0.0.1\\lzma-0.0.1.jar;" +
                 $"libraries\\java3d\\vecmath\\1.5.2\\vecmath-1.5.2.jar;" +
                 $"libraries\\net\\sf\\trove4j\\trove4j\\3.0.3\\trove4j-3.0.3.jar;" +
                 $"libraries\\org\\apache\\maven\\maven-artifact\\3.5.3\\maven-artifact-3.5.3.jar;" +
                 $"libraries\\net\\sf\\jopt-simple\\jopt-simple\\5.0.3\\jopt-simple-5.0.3.jar;" +
                 $"libraries\\oshi-project\\oshi-core\\1.1\\oshi-core-1.1.jar;" +
                 $"libraries\\net\\java\\dev\\jna\\jna\\4.4.0\\jna-4.4.0.jar;" +
                 $"libraries\\net\\java\\dev\\jna\\platform\\3.4.0\\platform-3.4.0.jar;" +
                 $"libraries\\com\\ibm\\icu\\icu4j-core-mojang\\51.2\\icu4j-core-mojang-51.2.jar;" +
                 $"libraries\\net\\sf\\jopt-simple\\jopt-simple\\5.0.3\\jopt-simple-5.0.3.jar;" +
                 $"libraries\\com\\paulscode\\codecjorbis\\20101023\\codecjorbis-20101023.jar;" +
                 $"libraries\\com\\paulscode\\codecwav\\20101023\\codecwav-20101023.jar;" +
                 $"libraries\\com\\paulscode\\libraryjavasound\\20101123\\libraryjavasound-20101123.jar;" +
                 $"libraries\\com\\paulscode\\librarylwjglopenal\\20100824\\librarylwjglopenal-20100824.jar;" +
                 $"libraries\\com\\paulscode\\soundsystem\\20120107\\soundsystem-20120107.jar;" +
                 $"libraries\\io\\netty\\netty-all\\4.1.9.Final\\netty-all-4.1.9.Final.jar;" +
                 $"libraries\\com\\google\\guava\\guava\\21.0\\guava-21.0.jar;" +
                 $"libraries\\org\\apache\\commons\\commons-lang3\\3.5\\commons-lang3-3.5.jar;" +
                 $"libraries\\commons-io\\commons-io\\2.5\\commons-io-2.5.jar;" +
                 $"libraries\\commons-codec\\commons-codec\\1.10\\commons-codec-1.10.jar;" +
                 $"libraries\\net\\java\\jinput\\jinput\\2.0.5\\jinput-2.0.5.jar;" +
                 $"libraries\\net\\java\\jutils\\jutils\\1.0.0\\jutils-1.0.0.jar;" +
                 $"libraries\\com\\google\\code\\gson\\gson\\2.8.0\\gson-2.8.0.jar;" +
                 $"libraries\\com\\mojang\\authlib\\1.5.25\\authlib-1.5.25.jar;" +
                 $"libraries\\com\\mojang\\realms\\1.10.22\\realms-1.10.22.jar;" +
                 $"libraries\\org\\apache\\commons\\commons-compress\\1.8.1\\commons-compress-1.8.1.jar;" +
                 $"libraries\\org\\apache\\httpcomponents\\httpclient\\4.3.3\\httpclient-4.3.3.jar;" +
                 $"libraries\\commons-logging\\commons-logging\\1.1.3\\commons-logging-1.1.3.jar;" +
                 $"libraries\\org\\apache\\httpcomponents\\httpcore\\4.3.2\\httpcore-4.3.2.jar;" +
                 $"libraries\\it\\unimi\\dsi\\fastutil\\7.1.0\\fastutil-7.1.0.jar;" +
                 $"libraries\\org\\apache\\logging\\log4j\\log4j-api\\2.8.1\\log4j-api-2.8.1.jar;" +
                 $"libraries\\org\\apache\\logging\\log4j\\log4j-core\\2.8.1\\log4j-core-2.8.1.jar;" +
                 $"libraries\\org\\lwjgl\\lwjgl\\lwjgl\\2.9.4-nightly-20150209\\lwjgl-2.9.4-nightly-20150209.jar;" +
                 $"libraries\\org\\lwjgl\\lwjgl\\lwjgl_util\\2.9.4-nightly-20150209\\lwjgl_util-2.9.4-nightly-20150209.jar;" +
                 $"libraries\\com\\mojang\\text2speech\\1.10.3\\text2speech-1.10.3.jar;" +
                 $"version\\1.12.2-forge-14.23.5.2855.jar " +
                 $"-Dminecraft.applet.TargetDirectory={gamedir} " +
                 $"-Dfml.ignoreInvalidMinecraftCertificates=true -Dfml.ignorePatchDiscrepancies=true " +
                 $"net.minecraft.launchwrapper.Launch --username {username} --version 1.12.2-forge-14.23.5.2855 " +
                 $"--gameDir {gamedir} --assetsDir assets --assetIndex 1.12 " +
                 $"--uuid {uuid} --accessToken null --userType mojang --tweakClass net.minecraftforge.fml.common.launcher.FMLTweaker " +
                 $"--versionType Forge --width {width} --height {height}");



        // вывод лога в консоль и файл
        public static void Log(params string[] msg)
        {
            if (msg.Length == 1) msg[0] = "[" + DateTime.Now.ToString() + "]: " + msg[0];
            else msg[1] = "[" + DateTime.Now.ToString() + "]: " + msg[1];
            LogNoTime(msg);
        }

        public static void LogNoTime(params string[] msg)
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


        public static void Debug(params string[] msg)
        {
            if (debug) Log(msg);
        }
        public static void DebugNoTime(params string[] msg)
        {
            if (debug) LogNoTime(msg);
        }
    }
}
