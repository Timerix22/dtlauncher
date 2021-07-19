﻿using DTLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace updater
{
    class Updater
    {
        static readonly string logfile = $"logs-updater\\updater-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');
        static Socket mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static Dtsod config;

        static void Main(string[] args)
        {
            try
            {
                Console.Title = "dtlauncher updater";
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;
                PublicLog.LogDel += Log;
                config = config = new(File.ReadAllText("updater.dtsod"));
                // подключение к центральному серверу
                while (true)
                {
                    try
                    {
                        Log("b", "server address: <", "c", config["server_domain"], "b",
                            ">\nserver port: <", "c", config["server_port"].ToString(), "b", ">\n");
                        mainSocket.Connect(new IPEndPoint(Dns.GetHostAddresses(config["server_domain"])[0], (int)config["server_port"]));
                        Log("g", "connected to server\n");
                        break;
                    }
                    catch (SocketException ex)
                    {
                        Log("r", $"updater.Main() error:\n{ex.Message}\n{ex.StackTrace}\n");
                    }
                }
                var recieved = mainSocket.GetPackage().ToStr();
                if (recieved != "requesting hash") throw new Exception("invalid server request");
                mainSocket.SendPackage(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
                recieved = mainSocket.GetPackage().ToStr();
                if (recieved != "updater") throw new Exception($"invalid central server answer <{recieved}>");
                // обновление апдейтера
                if (args.Length == 0 || args[0] != "updated")
                {
                    mainSocket.FSP_Download("dtlauncher.exe", "TEMP\\dtlauncher.exe");
                    Log("g", "dtlauncher.exe downloaded\n");
                    mainSocket.FSP_Download("DTLib.dll", "TEMP\\DTLib.dll");
                    Log("g", "DTLib.dll downloaded\n");
                    Process.Start("cmd", "/c timeout 0 && copy TEMP\\dtlauncher.exe dtlauncher.exe && copy TEMP\\DTLib.dll DTLib.dll && start dtlauncher.exe updated");
                }
                else
                {
                    Log("b", "downloading manifest\n");
                    mainSocket.FSP_Download("manifest.dtsod", "TEMP\\manifest.dtsod");
                    var manifest = new Dtsod(File.ReadAllText("TEMP\\manifest.dtsod"));
                    Log("g", $"found {manifest.Values.Count} files in manifest\n");
                    var hasher = new Hasher();
                    foreach (string file in manifest.Values.Keys)
                    {
                        Log("b", "file <", "c", file, "b", ">...  ");
                        if (!File.Exists(file))
                        {
                            LogNoTime("y", "doesn't exist\n");
                            mainSocket.FSP_Download(file, file);
                        }
                        else if (hasher.HashFile(file).HashToString() != manifest[file])
                        {
                            LogNoTime("y", "outdated\n");
                            mainSocket.FSP_Download(file, file);
                        }
                        else LogNoTime("g", "without changes\n");
                    }
                    // установка шрифтов
                    Process.Start("fonts\\fontinst.exe");
                    FileWork.DirDelete("TEMP");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Log("r", $"updater.Main() error:\n{ex.Message}\n{ex.StackTrace}\n", "gray", "press any key to close...");
                Console.ReadKey();
            }
            Log("gray", " \n");
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
    }
}