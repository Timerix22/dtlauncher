namespace launcher_client_avalonia.GUI;

public partial class ProgramSettingsPanelItem : UserControl
{
    public static readonly StyledProperty<string> SettingKeyProp = 
        AvaloniaProperty.Register<ProgramSettingsPanelItem, string>(
        "SettingKey");
    public string SettingKey
    {
        get => (string)GetValue(SettingKeyProp);
        set
        {
            SetValue(SettingKeyProp, value);
            
            KeyLabel.ToolTip = new ToolTip
            {
                Content = value,
                Foreground = App.MyWhite,
                Background = App.MySoftDark
            };
        }
    }

    public static readonly StyledProperty<string> SettingValueProp = 
        AvaloniaProperty.Register<ProgramSettingsPanelItem, string>("SettingValue");
    public string SettingValue
    {
        get => (string)GetValue(SettingValueProp);
        set => SetValue(SettingValueProp, value);
    }

    public event Action<ProgramSettingsPanelItem> UpdatedEvent;
    
    public ProgramSettingsPanelItem(string key, string value)
    {
        AvaloniaXamlLoader.Load(this);
        SettingKey = key;
        SettingValue = value;
        ValueBox.TextChanged += (_,_)=> UpdatedEvent?.Invoke(this);
    }
}