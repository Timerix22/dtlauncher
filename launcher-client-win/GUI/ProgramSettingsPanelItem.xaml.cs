using System.Windows.Controls;

namespace launcher_client_win.GUI;

public partial class ProgramSettingsPanelItem : UserControl
{
    public static readonly DependencyProperty SettingKeyProp = DependencyProperty.Register(
        "SettingKey",
        typeof(string), 
        typeof(TabButton));
    public string SettingKey
    {
        get => (string)GetValue(SettingKeyProp);
        set => SetValue(SettingKeyProp, value);
    }
    
    public static readonly DependencyProperty SettingValueProp = DependencyProperty.Register(
        "SettingValue",
        typeof(string), 
        typeof(TabButton));
    public string SettingValue
    {
        get => (string)GetValue(SettingValueProp);
        set => SetValue(SettingValueProp, value);
    }
    
    public ProgramSettingsPanelItem(string key, string value)
    {
        InitializeComponent();
        SettingKey = key;
        SettingValue = value;
        ValueBox.TextChanged += (_,_)=> UpdatedEvent?.Invoke(this);
    }

    public event Action<ProgramSettingsPanelItem> UpdatedEvent;
}