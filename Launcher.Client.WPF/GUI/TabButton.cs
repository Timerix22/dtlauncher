using System.Windows.Controls;

namespace Launcher.Client.WPF.GUI;

public class TabButton : Button
{
    public static readonly DependencyProperty TabGridProp = DependencyProperty.Register(
        "TabGrid",
        typeof(Grid), 
        typeof(TabButton));
    public Grid TabGrid
    {
        get => (Grid)GetValue(TabGridProp);
        set => SetValue(TabGridProp, value);
    }
}