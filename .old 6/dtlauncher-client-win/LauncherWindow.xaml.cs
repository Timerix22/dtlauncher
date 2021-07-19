﻿using DTLib;
using System;
using System.IO;
using System.Net.Sockets;
using System.Windows;

namespace dtlauncher_client_win
{
    /// <summary>
    /// Логика взаимодействия для LauncherWindow.xaml
    /// </summary>
    public partial class LauncherWindow : Window
    {
        public Socket mainSocket;
        string logfile;
        public delegate void LaunchDel();
        public LaunchDel Launch = () => { };
        public delegate void InstallDel();
        public LaunchDel Install = () => { };
        public ProgramLabel[] programsArray;
        public int PreviousProgramNum = 0;

        public LauncherWindow(Socket _socket, string _logfile, string _log)
        {
            InitializeComponent();
            try
            {
                mainSocket = _socket;
                logfile = _logfile;
                LogBox.Text += _log;
                PublicLog.LogDel += Log;
                // переключение вкладок кнопками
                var green = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 220, 17));
                var white = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                HomeButton.Click += (s, e) =>
                {
                    LogGrid.Visibility = Visibility.Hidden;
                    SettingsGrid.Visibility = Visibility.Hidden;
                    HomeGrid.Visibility = Visibility.Visible;
                    LogButton.Foreground = white;
                    SettingsButton.Foreground = white;
                    HomeButton.Foreground = green;
                };
                LogButton.Click += (s, e) =>
                {
                    HomeGrid.Visibility = Visibility.Hidden;
                    SettingsGrid.Visibility = Visibility.Hidden;
                    LogGrid.Visibility = Visibility.Visible;
                    HomeButton.Foreground = white;
                    SettingsButton.Foreground = white;
                    LogButton.Foreground = green;
                };
                SettingsButton.Click += (s, e) =>
                {
                    HomeGrid.Visibility = Visibility.Hidden;
                    LogGrid.Visibility = Visibility.Hidden;
                    SettingsGrid.Visibility = Visibility.Visible;
                    HomeButton.Foreground = white;
                    LogButton.Foreground = white;
                    SettingsButton.Foreground = green;
                };
                // считывание дескрипторов программ
                var descriptors = Directory.GetFiles("descroptors", "*.desc");
                programsArray = new ProgramLabel[descriptors.Length];
                Log(descriptors.Length + " descriptors found\n");
                for (int i = 0; i < descriptors.Length; i++)
                {
                    programsArray[i] = new ProgramLabel(descriptors[i], i, this);
                    ProgramsPanel.Children.Add(programsArray[i]);
                    Log(programsArray[i].Text + " added to ProgramsPanel\n");
                }
                LaunchButton.Click += (s, e) => Launch();
                InstallButton.Click += (s, e) => Install();
                //mainSocket.FSP_Download(new FSP_FileObject("share\\file.arc", "downloads\\file.arc"));
            }
            catch (Exception e)
            {
                Log("error:\n" + e.Message + "\n" + e.StackTrace + '\n');
            }
        }

        public void Log(string msg)
        {

            if (LogBox.Text[LogBox.Text.Length - 1] == '\n') msg = "[" + DateTime.Now.ToString() + "]: " + msg;
            FileWork.Log(logfile, msg);
            LogBox.Text += msg;
        }

        public void Log(params string[] input)
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