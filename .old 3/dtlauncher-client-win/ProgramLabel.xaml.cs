using System.Windows.Controls;

namespace dtlauncher_client_win
{
    /// <summary>
    /// Логика взаимодействия для ProgramLabel.xaml
    /// </summary>
    public partial class ProgramLabel : UserControl
    {
        /*static public DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ProgramLabel));
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        static public DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(System.Windows.Media.Imaging.BitmapImage), typeof(ProgramLabel));
        public System.Windows.Media.Imaging.BitmapImage Icon
        {
            get { return (System.Windows.Media.Imaging.BitmapImage)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        static public DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(ProgramLabel));
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }*/

        public string BackgroundImage;
        public string ScriptServerPath;
        public string InstallDir;
        public string LaunchFile;
        public string Arguments;
        public string Description;
        public int Number;
        LauncherWindow Window;

        public ProgramLabel() { }

        /*public ProgramLabel(string descriptorPath, int number, LauncherWindow window)
        {
            Window = window;
            Number = number;
            //grid.MouseLeftButtonDown += (s, e) => ProgramShow();
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
            var text = FileWork.ReadFromConfig(descriptorPath, "name");
            Window.Log(text + '\n');
            Text = text;
            //Text = (string)NameLabel.Content;
            //IconImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\programs\\" + FileWork.ReadFromConfig(descriptorPath, "icon"), UriKind.Absolute));
            //BackgroundImage = Directory.GetCurrentDirectory() + "\\programs\\" + FileWork.ReadFromConfig(descriptorPath, "background");
            //ScriptServerPath = FileWork.ReadFromConfig(descriptorPath, "scriptserverpath");
            //InstallDir = FileWork.ReadFromConfig(descriptorPath, "installdir");
            //LaunchFile = FileWork.ReadFromConfig(descriptorPath, "launchfile");
            //if (LaunchFile.Contains("%installdir%")) LaunchFile = LaunchFile.Replace("%installdir%", InstallDir);
            //Arguments = FileWork.ReadFromConfig(descriptorPath, "arguments");
            //Description = FileWork.ReadFromConfig(descriptorPath, "description").Replace("\\n", "\r\n");
            //Window.Log(Text + "    " + Icon + "    " + Number);
        }

        void ProgramShow()
        {
            Window.programsArray[Window.PreviousProgramNum].Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
            Window.PreviousProgramNum = Number;
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 240));
            Window.Log(Directory.GetCurrentDirectory() + '\\' + BackgroundImage + '\n');
            Window.BackgroundImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(BackgroundImage, UriKind.Absolute));
            Window.Launch = () => {
                Process.Start(LaunchFile, Arguments);
            };
        }*/
    }
}
