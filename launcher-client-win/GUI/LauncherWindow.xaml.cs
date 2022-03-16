using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace launcher_client_win.GUI;

public partial class LauncherWindow : Window
{
    public LauncherWindow()
    {
        try
        {
            InitializeComponent();
            LogBox.Text = Logger.Buffer;
            Logger.MessageSent += LogHandler;
            LogfileLabel.Content = Logger.Logfile.Remove(0,Logger.Logfile.LastIndexOf(Path.Sep)+1);
            LogfileLabel.MouseLeftButtonDown += (s,e)=>
                Process.Start("explorer.exe", Logger.Logfile.Remove(Logger.Logfile.LastIndexOf(Path.Sep)));
            LogfileLabel.MouseEnter += (s,e)=>LogfileLabel.Foreground=App.MySelectionColor;
            LogfileLabel.MouseLeave += (s,e)=>LogfileLabel.Foreground=App.MyWhite;
            LibraryButton.TabGrid = LibraryGrid;
            DownloadsButton.TabGrid = DownloadsGrid;
            LogButton.TabGrid = LogGrid;
            SettingsButton.TabGrid = SettingsGrid;
            LibraryButton.Click += SelectTab;
            DownloadsButton.Click += SelectTab;
            LogButton.Click += SelectTab;
            SettingsButton.Click += SelectTab;
            ProgramGrid.Visibility = Visibility.Hidden;
            SelectTab(LibraryButton, null);
            FillProgramsPanel();
            Logger.Log("launcher started");
        }
        catch(Exception ex)
        { LogError("LAUNCHER WINDOW INIT",ex); }
    }

    void LogHandler(string m) => Dispatcher.Invoke(()=>LogBox.Text += m);


    private TabButton CurrentTab;
    void SelectTab(object sender, RoutedEventArgs _)
    {
        if(CurrentTab!=null)
        {
            CurrentTab.Foreground = App.MyWhite;
            CurrentTab.TabGrid.Visibility = Visibility.Collapsed;
        }
        var selected = (TabButton)sender;
        selected.Foreground = App.MyGreen;
        selected.TabGrid.Visibility = Visibility.Visible;
        CurrentTab = selected;
    }
    void LibraryTab_activate(object sender, RoutedEventArgs eventArgs)
    {
        LibraryButton.Foreground = App.MyGreen;
        LogButton.Foreground = App.MyWhite;
        SettingsButton.Foreground = App.MyWhite;
        LibraryGrid.Visibility = Visibility.Visible;
        LogGrid.Visibility = Visibility.Hidden;
        SettingsGrid.Visibility = Visibility.Hidden;
    }
    void LogTab_activate(object sender, RoutedEventArgs eventArgs)
    {
        LibraryButton.Foreground = App.MyWhite;
        LogButton.Foreground = App.MyGreen;
        SettingsButton.Foreground = App.MyWhite;
        LibraryGrid.Visibility = Visibility.Hidden;
        LogGrid.Visibility = Visibility.Visible;
        SettingsGrid.Visibility = Visibility.Hidden;
    }
    void SettingsTab_activate(object sender, RoutedEventArgs eventArgs)
    {
        LibraryButton.Foreground = App.MyWhite;
        LogButton.Foreground = App.MyWhite;
        SettingsButton.Foreground = App.MyGreen;
        LibraryGrid.Visibility = Visibility.Hidden;
        LogGrid.Visibility = Visibility.Hidden;
        SettingsGrid.Visibility = Visibility.Visible;
    }

    public Program[] Programs;
    
    private void FillProgramsPanel()
    {
        Logger.Log("reading descriptors...");
        string[] descriptors = Directory.GetFiles("descriptors");
        Programs = new Program[descriptors.Length];
        for (ushort i = 0; i < descriptors.Length; i++)
        {
            string descriptor = descriptors[i];
            if(descriptor.EndsWith(".descriptor"))
            {
                Logger.Log('\t'+descriptor);
                Programs[i] = new Program(descriptors[i]);
                ProgramsPanel.Children.Add(Programs[i].ProgramLabel);
                Programs[i].ProgramSelectedEvent += SelectProgram;
            }
        }
    }
    
    public Program DisplayingProgram;
    public void SelectProgram(Program selectedP)
    {
        try
        {
            if (DisplayingProgram != null)
            {
                DisplayingProgram.ProgramLabel.Foreground = App.MyWhite;
                DisplayingProgram.ProgramLabel.FontWeight = FontWeights.Normal;
            }
            else ProgramGrid.Visibility = Visibility.Visible;

            selectedP.ProgramLabel.Foreground = App.MyGreen;
            selectedP.ProgramLabel.FontWeight = FontWeights.Bold;

            NameLabel.Content = selectedP.Name;
            DescriptionBox.Text = selectedP.Description;
            BackgroundImage.Source =
                new BitmapImage(new Uri(
                    $"{Directory.GetCurrent()}{Path.Sep}backgrounds{Path.Sep}{selectedP.BackgroundFile}", 
                    UriKind.Absolute));
            DisplayingProgram = selectedP;
        }
        catch(Exception ex)
        { LogError("SelectProgram()",ex); }
    }
}