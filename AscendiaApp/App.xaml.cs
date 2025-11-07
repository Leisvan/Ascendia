using Ascendia.Core.Services;
using AscendiaApp.ViewModels;
using AscendiaApp.ViewModels.Dialogs;
using AscendiaApp.Views;
using LCTWorks.Core.Services;
using LCTWorks.Telemetry;
using LCTWorks.Telemetry.Logging;
using LCTWorks.WinUI;
using LCTWorks.WinUI.Activation;
using LCTWorks.WinUI.Dialogs;
using LCTWorks.WinUI.Helpers;
using LCTWorks.WinUI.Navigation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage;
using System;
using System.Security;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using WinUIEx;

namespace AscendiaApp;

public partial class App : Application, IAppExtended
{
    private readonly ITelemetryService? _telemetryService;

    public App()
    {
        InitializeComponent();

        var configuration = ReadConfigurations();

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                services
                // Default Activation Handler
                .AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>()
                .AddSingleton<ActivationService>()
                .AddSingleton<FrameNavigationService>()
                .AddSingleton(new CacheService(ApplicationData.GetDefault().LocalCachePath))
                .AddSingleton<LadderService>()
                .AddSingleton(sp => new AirtableHttpService(configuration["AirBaseSettings:token"], configuration["AirBaseSettings:baseId"]))
                .AddSingleton<CommunityService>()
                .AddSingleton<DialogService>()
                //ViewModels:
                .AddTransient<MembersViewModel>()
                .AddTransient<EditMemberViewModel>()
                //Discord bot
                .AddLogging(config =>
                {
                    config.AddConsole();
                    config.AddProvider(new ConsoleSimpleLoggerProvider());
                })
                .AddSentry(configuration["Telemetry:key"], RuntimePackageHelper.Environment, RuntimePackageHelper.IsDebug(), RuntimePackageHelper.GetTelemetryContextData())
                ;
            }).Build();

        _telemetryService = GetService<ITelemetryService>();

        UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public IHost Host
    {
        get;
    }

    Window IAppExtended.MainWindow => MainWindow;

    public static T? GetService<T>()
                    where T : class
    {
        try
        {
            if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
            {
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }
            return service;
        }
        catch
        {
            return default;
        }
    }

    public void AppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception && _telemetryService != null)
        {
            exception.Data["AppExType"] = "AppDomainUnhandledException";
            _telemetryService.ReportUnhandledException(exception);
        }
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        var shellPage = new ShellPage();
        var activationService = GetService<ActivationService>();
        if (activationService != null)
        {
            await activationService.ActivateAsync(args, shellPage);
        }
    }

    private static IConfiguration ReadConfigurations()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Package.Current.InstalledLocation.Path)
            .AddJsonFile("assets\\Config\\appsettings.json", false)
            .Build();
    }

    [SecurityCritical]
    private void App_UnhandledException(object _, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        _telemetryService?.ReportUnhandledException(e.Exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs? e)
    {
        if (e?.Exception == null)
        {
            return;
        }
        var flattenedExceptions = e.Exception.Flatten().InnerExceptions;
        foreach (var exception in flattenedExceptions)
        {
            _telemetryService?.TrackError(exception);
        }
        e.SetObserved();
    }
}