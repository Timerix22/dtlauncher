using DTLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace dtlauncher_client_win
{
    /// <summary>
    /// Логика взаимодействия для ProgramLabel.xaml
    /// </summary>
    public partial class ProgramLabel : UserControl
    {
        public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ProgramLabel));
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(System.Windows.Media.Imaging.BitmapImage), typeof(ProgramLabel));
        public System.Windows.Media.Imaging.BitmapImage Icon
        {
            get { return (System.Windows.Media.Imaging.BitmapImage)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(ProgramLabel));
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public string BackgroundImage;
        public string Script;
        public string InstallDir;
        public string LaunchFile;
        public string Arguments;
        public string Description;
        public int Number;
        LauncherWindow Window;

        public ProgramLabel()
        {
            InitializeComponent();
        }

        public ProgramLabel(string descriptorPath, int number, LauncherWindow window)
        {
            try
            {
                InitializeComponent();
                Window = window;
                Number = number;
                FontSize = 14;
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                Text = FileWork.ReadFromConfig(descriptorPath, "name");
                NameLabel.Content = FileWork.ReadFromConfig(descriptorPath, "name");
                IconImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\descroptors\\" + FileWork.ReadFromConfig(descriptorPath, "icon"), UriKind.Absolute));
                BackgroundImage = Directory.GetCurrentDirectory() + "\\descroptors\\" + FileWork.ReadFromConfig(descriptorPath, "background");
                Script = FileWork.ReadFromConfig(descriptorPath, "script");
                InstallDir = FileWork.ReadFromConfig(descriptorPath, "installdir");
                LaunchFile = FileWork.ReadFromConfig(descriptorPath, "launchfile");
                Arguments = FileWork.ReadFromConfig(descriptorPath, "arguments");
                Description = FileWork.ReadFromConfig(descriptorPath, "description").Replace("\\n", "\n");
                //Window.Log(Text + "    " + Icon + "    " + Number);
            }
            catch (Exception e)
            {
                Window.Log("error:\n" + e.Message + "\n" + e.StackTrace + '\n');
            }
        }

        void ProgramShow(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                Window.programsArray[Window.PreviousProgramNum].Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                Window.programsArray[Window.PreviousProgramNum].FontWeight = FontWeights.Normal;
                Window.programsArray[Window.PreviousProgramNum].FontSize = 14;
                Window.PreviousProgramNum = Number;
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 240));
                FontWeight = FontWeights.Bold;
                FontSize = 13;
                Window.NameLabel.Content = Text;
                Window.DescriptionBox.Text = Description;
                Window.BackgroundImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(BackgroundImage, UriKind.Absolute));
                Window.Launch = () =>
                {
                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = LaunchFile,
                        Arguments = Arguments,
                    };
                    Process.Start(startInfo);
                };
                Window.Install = () =>
                {
                    var scriptrunner = new DTScript.ScriptRunner
                    {
                        debug = false,
                        mainSocket = Window.mainSocket
                    };
                    scriptrunner.RunScriptFile(Directory.GetCurrentDirectory() + "\\scripts\\" + Script);
                };
            }
            catch (Exception ex)
            {
                Window.Log("error:\n" + ex.Message + "\n" + ex.StackTrace + '\n');
            }
        }
    }
}
