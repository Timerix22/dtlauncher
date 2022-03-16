using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using launcher_client_win.GUI;

namespace launcher_client_win;

public class Program
{
    public readonly string Name;
    public readonly string Directory;
    public readonly string Description;
    public readonly string IconFile;
    public readonly string BackgroundFile;
    public readonly string LaunchFile;
    public readonly string LaunchArgs;
    
    public ProgramLabel ProgramLabel;
    private Process ProgramProcess;

    public Program(string descriptorFile)
    {
        DtsodV23 descriptor= new(File.ReadAllText(descriptorFile));
        Name = descriptor["name"];
        Directory = descriptor["directory"];
        Description = descriptor["description"];
        IconFile = descriptor["icon"];
        BackgroundFile = descriptor["background"];
        string startcommand = descriptor["launchcommand"];
        LaunchFile = startcommand.Remove(startcommand.IndexOf(' '));
        LaunchArgs = startcommand.Remove(0,startcommand.IndexOf(' '));
        ProgramLabel = new ProgramLabel(Name, IconFile);

        ProgramLabel.MouseLeftButtonDown += ProgramLabel_ClickHandler;
    }

    public event Action<Program> ProgramSelectedEvent;
    void ProgramLabel_ClickHandler(object s, MouseButtonEventArgs e) => ProgramSelectedEvent?.Invoke(this);

    public void Launch()
    {
        if(ProgramProcess.HasExited)
            throw new Exception($"can't start program <{Name}>, because it hadn't stopped yet");
        ProgramProcess = Process.Start(LaunchFile, LaunchArgs);
        if (ProgramProcess is null) 
            throw new Exception($"program <{Name}> started, but ProgramProcess is null");
        CurrentLauncherWindow.LaunchButton.Content = "Stop";
        ProgramProcess.Exited += ProgramExitedHandler;
    }

    public void Stop()
    {
        if (!ProgramProcess.HasExited)
            throw new Exception($"can't stop program <{Name}>, because it had stopped already");
        ProgramProcess.Kill(true); 
    }

    void ProgramExitedHandler(object sender, EventArgs eargs)
    {
        CurrentLauncherWindow.LaunchButton.Content = "Start";
    }
}