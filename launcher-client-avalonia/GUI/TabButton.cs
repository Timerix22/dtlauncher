namespace launcher_client_avalonia.GUI;

public class TabButton : Button
{
    public static readonly StyledProperty<Grid> TabGridProp = AvaloniaProperty.Register<TabButton, Grid>("TabGrid");
    public Grid TabGrid
    {
        get => (Grid)GetValue(TabGridProp);
        set => SetValue(TabGridProp, value);
    }
}