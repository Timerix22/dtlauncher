using DTLib;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace dtlauncher_client_win
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public Socket mainSocket { private set; get; } = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public string logfile { private set; get; } = $"logs\\client-{DateTime.Now}.log".Replace(':', '-').Replace(' ', '_');

        public LoginWindow()
        {
            try
            {
                InitializeComponent();
                FileWork.DirCreate("logs");
                FileWork.DirCreate("downloads");
                PublicLog.LogDel += Log;
                LoginButton.Click += Login;
                RegisterButton.Click += Register;
                Closed += CloseWindow;
                Log("[" + DateTime.Now.ToString() + "]: launcher is starting\n");
            }
            catch (Exception e)
            {
                Log("error:\n" + e.Message + "\n" + e.StackTrace + '\n');
            }
        }

        void Log(string msg)
        {
            lock (new object())
            {
                if (LogBox.Text.Length - 1 >= 0 && LogBox.Text[LogBox.Text.Length - 1] == '\n') msg = "[" + DateTime.Now.ToString() + "]: " + msg;
                FileWork.Log(logfile, msg);
                LogBox.Text += msg;
            }
        }

        void Log(params string[] input)
        {
            var str = new System.Text.StringBuilder();
            if (input.Length == 1) str.Append(input[0]);
            else if (input.Length % 2 == 0) for (ushort i = 0; i < input.Length; i++) str.Append(input[++i]);
            else throw new Exception("error in Log(): every text string must have color string before");
            lock (new object())
            {
                FileWork.Log(logfile, str.ToString());
                LogBox.Text += str.ToString();
            }
        }

        void Register(object sender, EventArgs e)
        {
            try
            {
                var hasher = new Hasher();
                File.WriteAllText($"register-{LoginBox.Text}.req",
                    hasher.HashCycled(hasher.Hash(LoginBox.Text.ToBytes(), PasswBox.Password.ToBytes()), 512).HashToString() 
                    + ": " + LoginBox.Text);
                Log($"request file created:{Directory.GetCurrentDirectory()}\\register-{LoginBox.Text}.req {hasher.Hash(LoginBox.Text.ToBytes(), PasswBox.Password.ToBytes()).Length}");
            }
            catch (Exception ex)
            {
                Log("registration error: " + ex.Message);
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
                mainSocket.Connect(new IPEndPoint(
                    Dns.GetHostAddresses(FileWork.ReadFromConfig("client.cfg", "central server ip"))[0],
                    FileWork.ReadFromConfig("client.cfg", "central server port").ToInt()));
                mainSocket.ReceiveTimeout = 2000;
                string request = mainSocket.GetPackageClear(16).ToStr();
                if (request != "requesting hash") throw new Exception($"Login() error: invalid request <{request}> <{request.Length}>");
                var hasher = new Hasher();
                mainSocket.SendPackage(32, hasher.HashCycled(hasher.Hash(LoginBox.Text.ToBytes(), PasswBox.Password.ToBytes()), 512));
                request = mainSocket.GetPackageClear(16).ToStr();
                if (request != "success") throw new Exception($"Login() error: invalid success message <{request}>");
                Log("succesfully logined\n");
                // вызов нового окна
                var lauWin = new LauncherWindow(mainSocket, logfile, LogBox.Text);
                lauWin.Show();
                Hide();
                PublicLog.LogDel -= Log;
                lauWin.Closed += CloseWindow;
            }
            catch (Exception ex)
            {
                Log("login error: " + ex.Message + '\n' + ex.StackTrace);
            }
        }

        void CloseWindow(object sender, EventArgs e)
        {
            if (mainSocket.Connected)
            {
                mainSocket.Shutdown(SocketShutdown.Both);
                mainSocket.Close();
            }
            Log("DTchat closed");
            Close();
            App.Current.Shutdown();
        }
    }
}
