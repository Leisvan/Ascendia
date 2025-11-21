using LCTWorks.Telemetry;

namespace Ascendia.Core.Interactivity;

public static class CoreTelemetry
{
    private static readonly Lock _lock = new();
    private static ConsoleColor _foregroundColor = ConsoleColor.Gray;

    private static ITelemetryService? _telemetryService;

    public static event EventHandler? ClearConsoleRequested;

    public static ConsoleColor ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            lock (_lock)
            {
                _foregroundColor = value;
                Console.ForegroundColor = value;
            }
        }
    }

    public static void ClearConsole()
    {
        lock (_lock)
        {
            ClearConsoleRequested?.Invoke(null, EventArgs.Empty);
            ResetForegroundColor();
        }
    }

    public static void ResetForegroundColor()
            => ForegroundColor = ConsoleColor.Gray;

    public static void SetTelemetryService(ITelemetryService telemetryService)
        => _telemetryService = telemetryService;

    public static void WriteErrorLine(string message)
    {
        WriteLine(message, ConsoleColor.Red);
        _telemetryService?.Log(message, Microsoft.Extensions.Logging.LogLevel.Error);
    }

    public static void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray, bool includeTimeSpan = true)
    {
        var time = DateTime.Now.ToLocalTime();
        if (includeTimeSpan)
        {
            message = $"[{time:HH:mm:ss}] {message}";
        }
        ForegroundColor = color;
        Console.WriteLine(message);
        ResetForegroundColor();
        _telemetryService?.Log(message);
    }

    public static void WriteSuccessLine(string message)
    {
        WriteLine(message, ConsoleColor.Green);
        _telemetryService?.Log(message, Microsoft.Extensions.Logging.LogLevel.Information);
    }

    public static void WriteWarningLine(string message)
    {
        WriteLine(message, ConsoleColor.Yellow);
        _telemetryService?.Log(message, Microsoft.Extensions.Logging.LogLevel.Warning);
    }
}