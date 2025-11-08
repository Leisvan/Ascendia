using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace AscendiaApp.Views;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();
        TitleBarHelper.Extend(AppTitleBar, AppTitleBarText, "AppDisplayName".GetTextLocalized());
    }

    public string Version { get; } = RuntimePackageHelper.IsDebug() ? "Debug" : RuntimePackageHelper.GetPackageVersion();
}