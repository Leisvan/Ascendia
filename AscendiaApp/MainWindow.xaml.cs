using LCTWorks.WinUI.Extensions;
using System;
using System.IO;
using WinUIEx;

namespace AscendiaApp;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetTextLocalized();
    }
}