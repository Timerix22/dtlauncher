using Launcher.Client.Avalonia.GUI;

namespace Launcher.Client.Avalonia;

public class Program
{
    public readonly string Name;
    public readonly string Directory;
    public readonly string Description;
    public readonly string IconFile;
    public readonly string BackgroundFile;
    public readonly string LaunchFile;
    public readonly string LaunchArgs;
    
    public readonly ProgramLabel ProgramLabel;

    public readonly string SettingsFile;
    public readonly DtsodV23 Settings;

    public readonly StackPanel SettingsPanel;
    
    private Process ProgramProcess;
    
    public event Action<Program> ProgramSelectedEvent;

    public Program(IOPath descriptorFile)
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
        ProgramLabel.PointerPressed += (_, _) => ProgramSelectedEvent?.Invoke(this);
        
        SettingsFile = $"settings{Path.Sep}{Directory}.settings";
        Settings = File.Exists(SettingsFile)
            ? DtsodConverter.UpdateByDefault(
                new DtsodV23(File.ReadAllText(SettingsFile)),
                descriptor["default_settings"])
            : descriptor["default_settings"];
        File.WriteAllText(SettingsFile, Settings.ToString());
        SettingsPanel = new StackPanel();
        foreach (var setting in Settings)
        {
            ProgramSettingsPanelItem settingUi = new(setting.Key, setting.Value);
            settingUi.UpdatedEvent += UpdateSetting;
            SettingsPanel.Children.Add(settingUi);
        }
    }

    void UpdateSetting(ProgramSettingsPanelItem uiElem)
    {
        Settings[uiElem.SettingKey] = uiElem.SettingValue;
        File.WriteAllText(SettingsFile, Settings.ToString());
    }

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