using LCTWorks.WinUI.Helpers;

namespace AscendiaApp.Helpers;

public static class AppHelper
{
    public static bool IsDebug => RuntimePackageHelper.IsDebug();
}