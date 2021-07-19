using DTLib;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace dtlauncher_client
{
    class Program
    {
        static ConsoleGUI gui = new ConsoleGUI(90, 30);
        static Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static string logfile = $"logs\\client-{DateTime.UtcNow}.log".Replace(':', '-').Replace(' ', '_');
        static bool enter = false;

        static void Main()
        {
            try
            {
                Console.Title = "dtlauncher";
                gui.ReadFromFile("gui\\start_tab.gui");
                gui.ShowAll();
                NetWork.Log += Log;
                // подключение к серверу
                /*mainSocket.Connect(new IPEndPoint(Dns.GetHostAddresses(
                    FileWork.ReadFromConfig("client.cfg", "central server ip"))[0],
                    Convert.ToInt32(FileWork.ReadFromConfig("client.cfg", "central server port"))));
                mainSocket.ReceiveTimeout = 5000;
                gui.ChangeLine(3, 4, 'y', "trying to connect to the main server...");
                string recieved = mainSocket.Request("new user connection try").ToStr();
                if (recieved != "new user connection created")
                    throw new Exception("can't connect to the main server");
                gui.ChangeLine(3, 5, 'g', "connected to the main server");
                //NetWork.RequestServersList(mainSocket);
                gui.ResetCursor();*/
                Input();
            }
            catch (Exception e)
            {
                Log("r", "\nerror:\n" + e.Message + "\n" + e.StackTrace + '\n');
                Console.ReadLine();
            }
        }

        static string inputText = "";
        static void Input()
        {
            while (true)
            {
                ConsoleKeyInfo readKeyResult = Console.ReadKey(true); // Считывание ввода
                switch (readKeyResult.Key)
                {
                    case ConsoleKey.F1:
                        gui.ReadFromFile("gui\\files_tab.gui");
                        for (sbyte i = 3; i < 89; i++)
                            gui.ChangeColor(i, 1, 'w');
                        for (sbyte i = 3; i < 13; i++)
                            gui.ChangeColor(i, 1, 'c');
                        gui.UpdateAll();
                        break;
                    case ConsoleKey.F2:
                        gui.ReadFromFile("gui\\servers_tab.gui");
                        for (sbyte i = 3; i < 89; i++)
                            gui.ChangeColor(i, 1, 'w');
                        for (sbyte i = 19; i < 31; i++)
                            gui.ChangeColor(i, 1, 'c');
                        gui.UpdateAll();
                        break;
                    case ConsoleKey.F3:
                        gui.ReadFromFile("gui\\settings_tab.gui");
                        for (sbyte i = 3; i < 89; i++)
                            gui.ChangeColor(i, 1, 'w');
                        for (sbyte i = 37; i < 50; i++)
                            gui.ChangeColor(i, 1, 'c');
                        gui.UpdateAll();
                        break;
                    case ConsoleKey.F4:
                        return;
                    case ConsoleKey.F5:
                        Console.Clear();
                        gui.ShowAll();
                        break;
                    case ConsoleKey.F6:
                        break;
                    /*case ConsoleKey.F7:
                        break;
                    case ConsoleKey.F8:
                        break;
                    case ConsoleKey.F9:
                        break;
                    case ConsoleKey.F10:
                        break;
                    case ConsoleKey.F11:
                        break;
                    case ConsoleKey.F12:
                        break;
                    case ConsoleKey.UpArrow:
                        break;
                    case ConsoleKey.DownArrow:
                        break;
                    case ConsoleKey.LeftArrow:
                        break;
                    case ConsoleKey.RightArrow:
                        break;
                    case ConsoleKey.PageUp:
                        break;
                    case ConsoleKey.PageDown:
                        break;
                    case ConsoleKey.Home:
                        break;
                    case ConsoleKey.End:
                        break;
                    case ConsoleKey.Escape:
                        break;
                    case ConsoleKey.Enter:
                        enter = true;
                        break;
                    case ConsoleKey.Backspace:
                        if (inputText.Length > 0)
                            inputText = inputText.Remove(inputText.Length - 1);
                        break;*/
                    default:
                        inputText += readKeyResult.KeyChar;
                        break;
                }
            }
        }
        static void Log(string color, string msg)
        {
            ColoredText.WriteColored(color, msg);
            FileWork.Log(logfile, msg);
        }

        static void Log(string[] input)
        {
            if (input.Length % 2 == 0)
            {
                ColoredText.WriteColored(input);
                string str = "";
                for (ushort i = 0; i < input.Length; i++)
                {
                    str += input[++i];
                }
                FileWork.Log(logfile, str);
            }
            else
            {
                throw new Exception("error in Log(): every text string must have color string before");
            }
        }
    }
}
