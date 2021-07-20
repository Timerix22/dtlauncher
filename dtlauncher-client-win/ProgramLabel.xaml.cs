using DTLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(BitmapImage), typeof(ProgramLabel));
        public BitmapImage Icon
        {
            get => (BitmapImage)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(ProgramLabel));
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public int Number;
        LauncherWindow Window;
        //public string background;
        //public string description;
        //public string installScript;
        //public string installDir;
        Dtsod descriptor;
        Dtsod launchinfo;

        public ProgramLabel()
        {
            InitializeComponent();
        }

        public ProgramLabel(string descriptorFile, int number, LauncherWindow window)
        {
            try
            {
                InitializeComponent();
                Window = window;
                Number = number;
                descriptor = new(File.ReadAllText(descriptorFile));
                launchinfo = new(File.ReadAllText("launchinfo\\" + descriptor["id"]));
                FontSize = 14;
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                Text = descriptor["name"];
                NameLabel.Content = descriptor["name"];
                IconImage.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\icons\\" + descriptor["id"], UriKind.Absolute));
                //background = Directory.GetCurrentDirectory() + "\\descriptors\\" + descriptor["background"];
                //installScript = descriptor["script"];
                //installDir = descriptor["installdir"];
                //launchInfo = descriptor["LaunchInfo"];
                //description = descriptor["description"].Replace("\\n", "\n");
                //Window.Log(Text + "    " + Icon + "    " + Number);
            }
            catch (Exception ex)
            {
                string mes = $"ProgramLabel() error:\n{ex.Message}\n{ex.StackTrace}\n";
                MessageBox.Show(mes);
                Window.Log(mes);
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
                Window.DescriptionBox.Text = descriptor["description"];
                Window.BackgroundImage.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\backgrounds\\" + descriptor["id"], UriKind.Absolute));
                Window.Launch = () =>
                {
                    switch (descriptor["id"])
                    {
                        case "anarx_1.12":
                            
                            break;
                        default:
                            Window.Log($"launching file <{launchinfo["launchfile"]}>\n");
                            Process.Start(launchinfo["launchfile"]);
                            break;
                    }
                };
                Window.Install = () =>
                {
                    Window.Log($"launching installation script <{descriptor["id"]}>\n");
                    var scriptrunner = new DTScript.ScriptRunner
                    {
                        debug = false,
                        mainSocket = Window.mainSocket
                    };
                    scriptrunner.RunScriptFile("installscripts\\" + descriptor["id"]);
                };
            }
            catch (Exception ex)
            {
                string mes = $"ProgramLabel.ProgramShow() error:\n{ex.Message}\n{ex.StackTrace}\n";
                MessageBox.Show(mes);
                Window.Log(mes);
            }
        }
    }
}
