using CommunityToolkit.Mvvm.ComponentModel;
using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Helpers;

namespace AscendiaApp.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public string AppName { get; } = "AppDisplayName".GetTextLocalized();

    public string AppVersion { get; } = RuntimePackageHelper.IsDebug() ? "Debug" : RuntimePackageHelper.GetPackageVersion();

    public string Architecture { get; } = RuntimePackageHelper.OsArchitecture;
}