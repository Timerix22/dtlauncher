using Avalonia.Controls;

namespace launcher_client_avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}