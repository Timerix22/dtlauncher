using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using DTLib;
using DTLib.Dtsod;
using DTLib.Filesystem;
using DTLib.Network;
using DTLib.Extensions;

namespace dtlauncher_client_win
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public string logfile = $"logs\\client-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');
        DtsodV21 config;

        public LoginWindow()
        {
            try
            {
                InitializeComponent();
                LogBox.Text = "   \n"; // костыль для работы Log()
                Directory.Create("logs");
                Directory.Create("downloads");
                Directory.Create("installed");
                Directory.Create("installscripts");
                Directory.Create("launchinfo");
                PublicLog.LogEvent += Log;
                PublicLog.LogNoTimeEvent += Log;
                LoginButton.Click += Login;
                RegisterButton.Click += Register;
                Log("[" + DateTime.Now.ToString() + "]: launcher is starting\n");
                config = new(File.ReadAllText("client.dtsod"));
                Closed += AppClose;
            }
            catch (Exception e)
            {
                Log("error:\n" + e.Message + "\n" + e.StackTrace + '\n');
            }
        }

        void Register(object sender, EventArgs e)
        {
            try
            {
                var hasher = new Hasher();
                string filename = $"register-{LoginBox.Text}.req";
                string content = hasher.HashCycled(hasher.Hash(LoginBox.Text.ToBytes(), PasswBox.Password.ToBytes()), 512).HashToString() + ": " + LoginBox.Text;
                //File.WriteAllText(filename, hasher.HashCycled(hasher.Hash(LoginBox.Text.ToBytes(), PasswBox.Password.ToBytes()), 512).HashToString() + ": " + LoginBox.Text);
                //Log($"request file created:{Directory.GetCurrentDirectory()}\\register-{LoginBox.Text}.req {hasher.Hash(LoginBox.Text.ToBytes(), PasswBox.Password.ToBytes()).Length}");
                if (mainSocket.Connected)
                {
                    mainSocket.Shutdown(SocketShutdown.Both);
                    mainSocket.Close();
                    mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
                Log($"server address: <{config["server_domain"]}>\nserver port: <{config["server_port"]}>\n");
                mainSocket.Connect(new IPEndPoint(Dns.GetHostAddresses(config["server_domain"])[0], (int)config["server_port"]));
                Log("g", "connecting to server...\n");
                mainSocket.ReceiveTimeout = 2000;
                string recieved = mainSocket.GetPackage().BytesToString();
                if (recieved != "requesting hash") throw new Exception($"Login() error: invalid request <{recieved}> <{recieved.Length}>");
                mainSocket.SendPackage(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
                recieved = mainSocket.GetPackage().BytesToString();
                if (recieved != "updater") throw new Exception($"invalid central server answer <{recieved}>");
                mainSocket.SendPackage("register new user".ToBytes());
                recieved = mainSocket.GetPackage().BytesToString();
                if (recieved != "ok") throw new Exception($"invalid central server answer <{recieved}>");
                mainSocket.SendPackage(content.ToBytes());
                Log("g", "registration request sent\n");
                //mainSocket.SendPackage(filename.ToBytes());
                //mainSocket.FSP_Upload(filename);
                //mainSocket.Shutdown(SocketShutdown.Both);
                //mainSocket.Close();
            }
            catch (Exception ex)
            {
                string mes = $"LoginWindow.Register() error:\n{ex.Message}\n{ex.StackTrace}\n";
                MessageBox.Show(mes);
                Log(mes);
            }
        }

        void Login(object sender, EventArgs e)
        {
            try
            {
                if (mainSocket.Connected)
                {
                    mainSocket.Shutdown(SocketShutdown.Both);
                    mainSocket.Close();
                    mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
                Log($"server address: <{config["server_domain"]}>\nserver port: <{config["server_port"]}>\n");
                mainSocket.Connect(new IPEndPoint(Dns.GetHostAddresses(config["server_domain"])[0], (int)config["server_port"]));
                Log("g", "connecting to server...\n");
                mainSocket.ReceiveTimeout = 2000;
                string recieved = mainSocket.GetPackage().BytesToString();
                if (recieved != "requesting hash") throw new Exception($"Login() error: invalid request <{recieved}> <{recieved.Length}>");
                var hasher = new Hasher();
                mainSocket.SendPackage(hasher.HashCycled(hasher.Hash(LoginBox.Text.ToBytes(), PasswBox.Password.ToBytes()), 512));
                recieved = mainSocket.GetPackage().BytesToString();
                if (recieved != "success") throw new Exception($"Login() error: invalid server answer <{recieved}>");
                Log("succesfully connected\n");
                // вызов нового окна
                PublicLog.LogEvent -= Log;
                PublicLog.LogNoTimeEvent -= Log;
                var lauWin = new LauncherWindow(mainSocket, logfile, LogBox.Text);
                lauWin.Show();
                Closed -= AppClose;
                Close();
            }
            catch (Exception ex)
            {
                string mes = $"LoginWindow.Login() error:\n{ex.Message}\n{ex.StackTrace}\n";
                MessageBox.Show(mes);
                Log(mes);
            }
        }

        public void Log(string msg)
        {
            if (LogBox.Text[LogBox.Text.Length - 1] == '\n') msg = "[" + DateTime.Now.ToString() + "]: " + msg;
            File.AppendAllText(logfile, msg);
            LogBox.Text += msg;
        }

        public void Log(params string[] input)
        {
            if (input.Length == 1) Log(input[0]);
            if (input.Length % 2 == 0)
            {
                StringBuilder strB = new();
                for (ushort i = 0; i < input.Length; i++)
                    strB.Append(input[++i]);
                Log(strB.ToString());
            }
            else throw new Exception("error in Log(): every text string must have color string before");
        }

        void AppClose(object sender, EventArgs e)
        {
            if (mainSocket.Connected)
            {
                mainSocket.Shutdown(SocketShutdown.Both);
                mainSocket.Close();
            }
            Log("dtlauncher closing\n");
            App.Current.Shutdown();
        }
    }
}
