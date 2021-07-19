using DTLib;
using System;
using System.IO;
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
            if (input.Length % 2 == 0)
            {
                string str = "";
                for (ushort i = 0; i < input.Length; i++)
                    str += input[++i];
                Log(str);
            }
            else throw new Exception("error in Log(): every text string must have color string before");
        }

        void Register(object sender, EventArgs e)
        {
            try
            {
                string login = LoginBox.Text;
                string password = PasswBox.Password;
                string filename = $"register-{login}.req";
                var hasher = new Hasher();
                if (File.Exists(filename)) File.Delete(filename);
                var writer = File.CreateText(filename);
                writer.Write(login + "  $" +
                    hasher.HashCycled(password.ToBytes(), login.ToBytes(), 4096)
                    .HashToString());
                writer.Close();
                Log($"request file created:{Directory.GetCurrentDirectory()}\\{filename}");
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
                ConnectingLabel.Visibility = Visibility.Visible;
                /*// пересоздание сокета
                if (mainSocket.Connected)
                {
                    mainSocket.Shutdown(SocketShutdown.Both);
                    mainSocket.Close();
                    mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
                // подключение к серверу
                Log("connecting to the main server...");
                mainSocket.Connect(new IPEndPoint(Dns.GetHostAddresses(
                FileWork.ReadFromConfig("client.cfg", "central server ip"))[0],
                    Convert.ToInt32(FileWork.ReadFromConfig("client.cfg", "central server port"))));
                mainSocket.ReceiveTimeout = 2000;
                string recieved = mainSocket.Request("new user connection try").ToStr();
                if (recieved != "new user connection created")
                throw new Exception("can't connect to the main server");
                Log("connected to the main server");
                //NetWork.RequestServersList(mainSocket);
                var hasher = new SecureHasher(256);
                // подключение к серверу
                mainSocket.Connect(new IPEndPoint(Dns.GetHostAddresses("m1net.keenetic.pro")[0], 20008));
                mainSocket.ReceiveTimeout = 5000;
                // авторизация
                string login = LoginBox.Text;
                string password = PasswBox.Password;
                string recievedString = NetWork.Request(mainSocket, login).ToStr();
                if (recievedString != "2e9b7473ce473cdbb9b9e68aa444f5146b1b415a05917aceecd3861804cc2fd8")
                    throw new Exception("incorrect login\n");recievedString = NetWork.Request(mainSocket,hasher.HashSaltCycled(password.ToBytes(), login.ToBytes(), 4096)).ToStr();
                if (recievedString != "82c8e541601e0883ea189a285e514adfa6c2da05c83285359bbcf73bd3a8518b")
                    throw new Exception("incorrect password");*/
                ConnectingLabel.Visibility = Visibility.Hidden;
                Log("succesfully login\n");
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
