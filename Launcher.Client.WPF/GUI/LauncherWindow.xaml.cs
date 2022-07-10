using System.Windows.Media.Imaging;

namespace Launcher.Client.WPF.GUI;

public partial class LauncherWindow : Window
{
    public LauncherWindow()
    {
        InitializeComponent();
        LogBox.Text = Logger.Buffer;
        Logger.MessageSent += LogHandler;
        LogfileLabel.Content = Logger.LogfileName.Remove(0,Logger.LogfileName.LastIndexOf(Путь.Разд)+1);
        LogfileLabel.MouseLeftButtonDown += (_,_)=>
            Process.Start("explorer.exe", LauncherLogger.LogfileDir);
        LogfileLabel.MouseEnter += (_,_)=>LogfileLabel.Foreground=App.MySelectionColor;
        LogfileLabel.MouseLeave += (_,_)=>LogfileLabel.Foreground=App.MyWhite;
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

    void LogHandler(string m) => Dispatcher.Invoke(()=>LogBox.Text += m);


    private TabButton CurrentTab;
    void SelectTab(object sender, RoutedEventArgs _)
    {
        if(CurrentTab!=null)
        {
            CurrentTab.Background = App.MyDark;
            CurrentTab.TabGrid.Visibility = Visibility.Collapsed;
        }
        var selected = (TabButton)sender;
        selected.Background = App.MySelectionColor;
        selected.TabGrid.Visibility = Visibility.Visible;
        CurrentTab = selected;
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
    public void SelectProgram(Program selectedProg)
    {
        try
        {
            if (DisplayingProgram != null)
            {
                DisplayingProgram.ProgramLabel.Foreground = App.MyWhite;
                DisplayingProgram.ProgramLabel.FontWeight = FontWeights.Normal;
            }
            else ProgramGrid.Visibility = Visibility.Visible;

            selectedProg.ProgramLabel.Foreground = App.MySelectionColor;
            selectedProg.ProgramLabel.FontWeight = FontWeights.Bold;

            NameLabel.Content = selectedProg.Name;
            DescriptionBox.Text = selectedProg.Description;
            BackgroundImage.Source =
                new BitmapImage(new Uri(
                    $"{Directory.GetCurrent()}{Путь.Разд}backgrounds{Путь.Разд}{selectedProg.BackgroundFile}", 
                    UriKind.Absolute));
            ProgramSettingsViever.Content = selectedProg.SettingsPanel;
            DisplayingProgram = selectedProg;
        }
        catch(Exception ex)
        { LogError("SelectProgram()",ex); }
    }
}