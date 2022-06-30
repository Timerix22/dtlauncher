using Avalonia.Markup.Xaml;

namespace launcher_client_avalonia.GUI;

public partial class MessageBox : Window
{
    public MessageBox()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
            this.AttachDevTools();
#endif
    }

    public static void Show(string text)
    {
        throw new NotImplementedException();
    }
}