using UtilityBelt.App.Models;

namespace UtilityBelt.App.Services;

public sealed class NotificationRouter
{
    private readonly ToastService _toast;
    public NotificationSettings Settings { get; set; } = new();

    private readonly Dictionary<string, StatusLevel> _lastLevelByCheckId = new();
    private readonly Dictionary<string, DateTimeOffset> _lastToastAtByCheckId = new();

    public NotificationRouter(ToastService toast)
    {
        _toast = toast;
    }

    public void OnCheckUpdated(CheckResult result)
    {
        if (!Settings.Enabled) return;

        if (!_lastLevelByCheckId.TryGetValue(result.CheckId, out var lastLevel))
        {
            _lastLevelByCheckId[result.CheckId] = result.Level;
            return;
        }

        if (lastLevel == result.Level) return;

        if (Settings.MinSecondsBetweenToasts > 0)
        {
            if (_lastToastAtByCheckId.TryGetValue(result.CheckId, out var lastToastAt))
            {
                var seconds = (result.TimestampUtc - lastToastAt).TotalSeconds;
                if (seconds < Settings.MinSecondsBetweenToasts) return;
            }
        }

        var title = $"{result.CheckId}: {lastLevel} → {result.Level}";
        var body = result.Message;

        var shouldToast = result.Level switch
        {
            StatusLevel.Warn => Settings.ToastOnWarn,
            StatusLevel.Error => Settings.ToastOnError,
            StatusLevel.Ok => Settings.ToastOnRecovery,
            StatusLevel.Unknown => Settings.ToastOnUnknown,
            _ => false
        };

        if (shouldToast)
        {
            _toast.Show(title, body);
            _lastToastAtByCheckId[result.CheckId] = result.TimestampUtc;
        }

        _lastLevelByCheckId[result.CheckId] = result.Level;
    }

    public void OnCheckFailed(string checkId, Exception ex)
    {
        if (!Settings.Enabled || !Settings.ToastOnExceptions) return;
        _toast.Show($"{checkId}: exception", ex.Message);
    }
}