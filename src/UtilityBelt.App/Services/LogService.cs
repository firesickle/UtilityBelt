using System.IO;
using Serilog;

namespace UtilityBelt.App.Services;

public static class LogService
{
    private static ILogger? _logger;

    public static ILogger Log => _logger ??= CreateLogger();

    public static string LogsDirectory(string appName = "UtilityBelt")
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, appName, "logs");
    }

    private static ILogger CreateLogger()
    {
        var logDir = LogsDirectory();
        Directory.CreateDirectory(logDir);

        var path = Path.Combine(logDir, "utilitybelt-.log");

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();
    }
}
