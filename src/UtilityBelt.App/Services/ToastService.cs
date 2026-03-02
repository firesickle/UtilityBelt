using System.Diagnostics;
using Hardcodet.Wpf.TaskbarNotification;

namespace UtilityBelt.App.Services;

/// <summary>
/// Notifications.
/// "Real" Windows toasts for unpackaged desktop apps require AUMID/registration.
/// For now we use the taskbar icon's balloon tips which are reliable and registration-free.
/// </summary>
public sealed class ToastService
{
    private readonly TaskbarIcon _trayIcon;
    private readonly string _appName;

    public ToastService(TaskbarIcon trayIcon, string appName = "UtilityBelt")
    {
        _trayIcon = trayIcon ?? throw new ArgumentNullException(nameof(trayIcon));
        _appName = appName;
    }

    public void Show(string title, string message)
    {
        try
        {
            _trayIcon.ShowBalloonTip(
                title: $"{_appName}: {title}",
                message: message,
                symbol: BalloonIcon.None);
        }
        catch (Exception ex)
        {
            LogService.Log.Warning(ex, "Notification delivery failed: {Title}", title);
            LogService.Log.Information("NOTIFY (fallback-log): {Title} - {Message}", title, message);

            try
            {
                var dir = LogService.LogsDirectory();
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
            catch
            {
                // ignore
            }
        }
    }
}
