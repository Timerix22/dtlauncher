namespace Launcher.Client.Avalonia.GUI;

public partial class MessageBox : Window
{
    public MessageBox()
    {
        InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
    }

    public static void Show(string title, string text)
    {
        throw new NotImplementedException();
    }
}