using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace AscendiaApp.Views;

public sealed partial class ShellPage : Page
{
    private const string BetaBuildCaption = "BETA";

    private const string DebugBuildCaption = "DEBUG";

    public ShellPage()
    {
        InitializeComponent();
        TitleBarHelper.Extend(AppTitleBar, AppTitleBarText, "AppDisplayName".GetTextLocalized());
        SetVersion();
    }

    public string? BuildCaption { get; set; }

    public bool HasCaption { get; set; }

    public string? VersionString { get; set; }

    private void SetVersion()
    {
        if (RuntimePackageHelper.IsDebug())
        {
            HasCaption = true;
            BuildCaption = DebugBuildCaption;
        }
        else
        {
            VersionString = RuntimePackageHelper.GetPackageVersion();
            if (VersionString.StartsWith('0'))
            {
                HasCaption = true;
                BuildCaption = BetaBuildCaption;
            }
        }
    }
}