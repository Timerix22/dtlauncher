namespace launcher_client_avalonia.GUI;

public partial class ProgramLabel : UserControl
{
    public ProgramLabel(string label, string icon)
    {
        AvaloniaXamlLoader.Load(this);
        NameLabel.Content = label;
        IconImage.Source = new BitmapImage(new Uri( 
            $"{Directory.GetCurrent()}{Путь.Разд}icons{Путь.Разд}{icon}", 
            UriKind.Absolute));
    }
}