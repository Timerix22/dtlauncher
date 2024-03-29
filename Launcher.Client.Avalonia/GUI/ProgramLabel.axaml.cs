﻿namespace Launcher.Client.Avalonia.GUI;

public partial class ProgramLabel : UserControl
{
    public ProgramLabel() => InitializeComponent();
    
    public ProgramLabel(string label, string icon)
    {
        InitializeComponent();
        NameLabel.Content = label;
        IconImage.Source = new Bitmap(
            $"{Directory.GetCurrent()}{Path.Sep}icons{Path.Sep}{icon}");
    }
}