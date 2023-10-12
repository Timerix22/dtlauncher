using Avalonia.Interactivity;

namespace Launcher.Client.Avalonia.GUI;

public partial class MessageBox : Window
{
    public MessageBox()
    {
        InitializeComponent();
    }

    public static void Show(string title, string text)
    {
        var mb = new MessageBox();
        mb.Title = title;
        mb.TextBlock.Text = text;
        mb.Topmost = true;
        mb.Show();
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}