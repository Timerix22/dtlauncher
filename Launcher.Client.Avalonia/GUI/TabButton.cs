using Avalonia.Styling;

namespace Launcher.Client.Avalonia.GUI;

public partial class TabButton : Button, IStyleable
{
    Type IStyleable.StyleKey => typeof(Button);
    
    public static readonly StyledProperty<Grid> TabGridProp = AvaloniaProperty.Register<TabButton, Grid>("TabGrid");
    public Grid TabGrid
    {
        get => GetValue(TabGridProp);
        set => SetValue(TabGridProp, value);
    }
    
    public TabButton() :base() {}
}