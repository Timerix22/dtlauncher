using DTLib;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using static DTLib.NetWork;

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
            InitializeComponent();
            try
            {
                FileWork.DirExistenceCheck("logs");
                FileWork.DirExistenceCheck("downloads");
                NetWork.Log += Log;
                LoginButton.Click += Login;
                RegisterButton.Click += Register;
                Closed += CloseWindow;
                Log("launcher is starting\n");
                // подключение к серверу
                mainSocket.Connect(new IPEndPoint(Dns.GetHostAddresses(
                    FileWork.ReadFromConfig("client.cfg", "central server ip"))[0],
                    Convert.ToInt32(FileWork.ReadFromConfig("client.cfg", "central server port"))));
                mainSocket.ReceiveTimeout = 2000;
                Log("connecting to the main server...");
                string recieved = mainSocket.Request("new user connection try").ToStr();
                if (recieved != "new user connection created")
                    throw new Exception("can't connect to the main server");
                Log("connected to the main server");
                //NetWork.RequestServersList(mainSocket);
            }
            catch (Exception e)
            {
                Log("\nerror:\n" + e.Message + "\n" + e.StackTrace + '\n');
            }
        }

        void Log(string msg)
        {
            msg = "[" + DateTime.Now.ToString() + "]: " + msg;
            FileWork.Log(logfile, msg);
            LogBox.Text += msg;
        }

        void Log(string[] input)
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

                var hasher = new SecureHasher(256);
                if (File.Exists(filename)) File.Delete(filename);
                var writer = File.CreateText(filename);
                writer.Write(login + "  $" +
                    hasher.HashSaltCycled(password.ToBytes(), login.ToBytes(), 4096)
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
                /*// пересоздание сокета
                if (mainSocket.IsBound)
                {
                    mainSocket.Shutdown(SocketShutdown.Both);
                    mainSocket.Close();
                    mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }

                var hasher = new SecureHasher(256);
                // подключение к серверу
                mainSocket.Connect(new IPEndPoint(Dns.GetHostAddresses("m1net.keenetic.pro")[0], 20008));
                mainSocket.ReceiveTimeout = 5000;

                // авторизация
                string login = LoginBox.Text;
                string password = PasswBox.Password;

                string recievedString = NetWork.Request(mainSocket, login).ToStr();
                if (recievedString != "2e9b7473ce473cdbb9b9e68aa444f5146b1b415a05917aceecd3861804cc2fd8")
                {
                    throw new Exception("incorrect login\n");
                }

                recievedString = NetWork.Request(mainSocket,hasher.HashSaltCycled(password.ToBytes(), login.ToBytes(), 4096)).ToStr();
                if (recievedString != "82c8e541601e0883ea189a285e514adfa6c2da05c83285359bbcf73bd3a8518b")
                {
                    throw new Exception("incorrect password");
                }*/

                Log("succesfully login\n");
                var lauWin = new LauncherWindow(mainSocket, logfile);
                //CloseSocket(null, null);
                lauWin.Show();
                Hide();
                lauWin.Closed += CloseWindow;
            }
            catch (Exception ex)
            {
                Log("login error: " + ex.Message + '\n' + ex.StackTrace);
            }
        }

        void CloseWindow(object sender, EventArgs e)
        {
            Log("DTchat closed");
            Close();
            App.Current.Shutdown();
        }
    }
}
