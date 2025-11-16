using Ascendia.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Helpers;

namespace AscendiaApp.ViewModels;

public partial class AboutViewModel(AirtableHttpService airtableHttpService) : ObservableObject
{
    private readonly AirtableHttpService _airtableHttpService = airtableHttpService;

    public string AirtableUrl => _airtableHttpService.AirtableUrl;

    public string AppName { get; } = "AppDisplayName".GetTextLocalized();

    public string AppVersion { get; } = RuntimePackageHelper.IsDebug() ? "Debug" : RuntimePackageHelper.GetPackageVersion();

    public string Architecture { get; } = RuntimePackageHelper.OsArchitecture;

    public string GitHubUrl { get; } = "https://github.com/Leisvan/ascendia";
}