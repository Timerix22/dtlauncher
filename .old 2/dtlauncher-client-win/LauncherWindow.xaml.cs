using DTLib;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using static DTLib.NetWork;

namespace dtlauncher_client_win
{
    /// <summary>
    /// Логика взаимодействия для LauncherWindow.xaml
    /// </summary>
    public partial class LauncherWindow : Window
    {
        Socket socket;
        string logfile;

        public LauncherWindow(Socket _socket, string _logfile)
        {
            InitializeComponent();
            socket = _socket;
            logfile = _logfile;
            NetWork.Log += Log;
            //mainSocket.FSP_Download(new FSP_FileObject("share\\file.arc", "downloads\\file.arc"));
        }

        void Log(string msg)
        {
            msg = "[" + DateTime.Now.ToString() + "]: " + msg;
            FileWork.Log(logfile, msg);
            //LogBox.Text += msg;
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
    }
}
