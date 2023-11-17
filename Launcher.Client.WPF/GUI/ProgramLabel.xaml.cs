using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Launcher.Client.WPF.GUI;

public partial class ProgramLabel : UserControl
{
    public ProgramLabel(string label, string icon)
    {
        InitializeComponent();
        NameLabel.Content = label;
        IconImage.Source = new BitmapImage(new Uri( 
            $"{Directory.GetCurrent()}{Path.Sep}icons{Path.Sep}{icon}", 
            UriKind.Absolute));
    }
}