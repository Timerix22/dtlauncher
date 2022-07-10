namespace Launcher.Client.Avalonia.GUI;

public class TabButton : Button
{
    public static readonly StyledProperty<Grid> TabGridProp = AvaloniaProperty.Register<TabButton, Grid>("TabGrid");
    public Grid TabGrid
    {
        get => GetValue(TabGridProp);
        set => SetValue(TabGridProp, value);
    }
}