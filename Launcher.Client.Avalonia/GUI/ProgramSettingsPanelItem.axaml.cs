namespace Launcher.Client.Avalonia.GUI;

public partial class ProgramSettingsPanelItem : UserControl
{
    public static readonly StyledProperty<string> SettingKeyProp = 
        AvaloniaProperty.Register<ProgramSettingsPanelItem, string>(
        "SettingKey");
    public string SettingKey
    {
        get => GetValue(SettingKeyProp);
        set => SetValue(SettingKeyProp, value);
        //TODO deal with textblock size
        /*KeyLabel.ToolTip = new ToolTip
            {
                Content = value,
                Foreground = App.MyWhite,
                Background = App.MySoftDark
            };*/
    }

    public static readonly StyledProperty<string> SettingValueProp = 
        AvaloniaProperty.Register<ProgramSettingsPanelItem, string>("SettingValue");
    public string SettingValue
    {
        get => GetValue(SettingValueProp);
        set => SetValue(SettingValueProp, value);
    }

    public event Action<ProgramSettingsPanelItem> UpdatedEvent;

    public ProgramSettingsPanelItem() => InitializeComponent();
    
    public ProgramSettingsPanelItem(string key, string value)
    {
        InitializeComponent();
        SettingKey = key;
        SettingValue = value;
        //TODO invoke UpdatedEvent only when focus changed
        ValueBox.TextInput += (_,_)=> UpdatedEvent?.Invoke(this);
    }
}