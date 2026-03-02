using System.IO;
using UtilityBelt.App.Models;

namespace UtilityBelt.App.Services;

public sealed class DiskSpaceStatusCheck : IStatusCheck
{
    private readonly string _driveLetter;
    private readonly int _warnBelowPercent;
    private readonly int _errorBelowPercent;

    public DiskSpaceStatusCheck(
        string id,
        string driveLetter,
        TimeSpan interval,
        int warnBelowPercent,
        int errorBelowPercent)
    {
        if (warnBelowPercent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(warnBelowPercent));
        if (errorBelowPercent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(errorBelowPercent));

        Id = id;
        _driveLetter = NormalizeDriveLetter(driveLetter);
        Interval = interval;
        _warnBelowPercent = warnBelowPercent;
        _errorBelowPercent = errorBelowPercent;
    }

    public string Id { get; }
    public TimeSpan Interval { get; }

    public Task<CheckResult> RunAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            var di = new DriveInfo(_driveLetter);
            if (!di.IsReady)
            {
                return Task.FromResult(new CheckResult(
                    Id,
                    StatusLevel.Unknown,
                    $"Disk {_driveLetter}: drive not ready",
                    DateTimeOffset.UtcNow));
            }

            var total = di.TotalSize;
            var free = di.AvailableFreeSpace;
            var freePct = total <= 0 ? 0 : (int)Math.Floor((free / (double)total) * 100.0);

            var level = freePct <= _errorBelowPercent ? StatusLevel.Error
                : freePct <= _warnBelowPercent ? StatusLevel.Warn
                : StatusLevel.Ok;

            var msg = $"Disk {_driveLetter}: {freePct}% free ({FormatBytes(free)} of {FormatBytes(total)})";

            return Task.FromResult(new CheckResult(Id, level, msg, DateTimeOffset.UtcNow));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CheckResult(
                Id,
                StatusLevel.Unknown,
                $"Disk {_driveLetter} failed: {ex.Message}",
                DateTimeOffset.UtcNow));
        }
    }

    private static string NormalizeDriveLetter(string drive)
    {
        var d = (drive ?? string.Empty).Trim();
        if (d.Length == 1) return d.ToUpperInvariant() + ":";
        if (d.Length == 2 && d[1] == ':') return d.ToUpperInvariant();
        if (d.Length >= 3 && d[1] == ':' && (d[2] == '\\' || d[2] == '/')) return d.Substring(0, 2).ToUpperInvariant();
        return d;
    }

    private static string FormatBytes(long bytes)
    {
        string[] suf = ["B", "KB", "MB", "GB", "TB"];
        double d = bytes;
        var i = 0;
        while (d >= 1024 && i < suf.Length - 1)
        {
            d /= 1024;
            i++;
        }
        return $"{d:0.##}{suf[i]}";
    }
}
