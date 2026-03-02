namespace UtilityBelt.App.Models;

public enum StatusLevel
{
    Unknown = 0,
    Ok = 1,
    Warn = 2,
    Error = 3
}

public sealed record CheckResult(
    string CheckId,
    StatusLevel Level,
    string Message,
    DateTimeOffset TimestampUtc);
