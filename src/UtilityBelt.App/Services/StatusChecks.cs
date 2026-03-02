using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UtilityBelt.App.Models;



namespace UtilityBelt.App.Services;

public interface IStatusCheck
{
    string Id { get; }
    TimeSpan Interval { get; }
    Task<CheckResult> RunAsync(CancellationToken ct);
}

public sealed class PingStatusCheck : IStatusCheck
{
    private readonly string _host;
    private readonly int _timeoutMs;

    public PingStatusCheck(string id, string host, TimeSpan interval, int timeoutMs)
    {
        Id = id;
        _host = host;
        Interval = interval;
        _timeoutMs = timeoutMs;
    }

    public string Id { get; }
    public TimeSpan Interval { get; }

    public async Task<CheckResult> RunAsync(CancellationToken ct)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(_host, _timeoutMs);

            if (reply.Status == IPStatus.Success)
            {
                return new CheckResult(
                    Id,
                    StatusLevel.Ok,
                    $"Ping {_host}: {reply.RoundtripTime}ms",
                    DateTimeOffset.UtcNow);
            }

            return new CheckResult(
                Id,
                StatusLevel.Error,
                $"Ping {_host}: {reply.Status}",
                DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            return new CheckResult(
                Id,
                StatusLevel.Unknown,
                $"Ping {_host} failed: {ex.Message}",
                DateTimeOffset.UtcNow);
        }
    }
}

/// <summary>
/// Checks a web endpoint with 3-state mapping:
/// - Unknown: host/port not found (connection refused / DNS fail)
/// - Warn: reachable but unhealthy (non-success HTTP, timeout, etc)
/// - Ok: reachable and healthy
/// </summary>
public sealed class HttpHealthStatusCheck : IStatusCheck
{
    private readonly Uri _url;
    private readonly TimeSpan _timeout;

    public HttpHealthStatusCheck(string id, Uri url, TimeSpan interval, TimeSpan timeout)
    {
        Id = id;
        _url = url;
        Interval = interval;
        _timeout = timeout;
    }

    public string Id { get; }
    public TimeSpan Interval { get; }

    public async Task<CheckResult> RunAsync(CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            using var http = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };

            using var req = new HttpRequestMessage(HttpMethod.Get, _url);
            req.Headers.TryAddWithoutValidation("User-Agent", "UtilityBelt/1.0");

            var started = DateTimeOffset.UtcNow;
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            var elapsedMs = (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds);

            if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode <= 299)
            {
                return new CheckResult(Id, StatusLevel.Ok, $"HTTP {(int)resp.StatusCode} in {elapsedMs}ms", DateTimeOffset.UtcNow);
            }

            // Reachable, but unhealthy
            return new CheckResult(Id, StatusLevel.Warn, $"HTTP {(int)resp.StatusCode} ({resp.ReasonPhrase})", DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // timeout
            return new CheckResult(Id, StatusLevel.Warn, $"HTTP timeout after {(int)_timeout.TotalMilliseconds}ms", DateTimeOffset.UtcNow);
        }
        catch (HttpRequestException ex) when (IsNotFound(ex))
        {
            return new CheckResult(Id, StatusLevel.Unknown, $"Endpoint not found: {_url}", DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            // Anything else (TLS errors, malformed responses, etc) => reachable/unhealthy.
            return new CheckResult(Id, StatusLevel.Warn, $"HTTP failed: {ex.Message}", DateTimeOffset.UtcNow);
        }
    }

    private static bool IsNotFound(HttpRequestException ex)
    {
        // Connection refused / DNS failure / no route etc.
        if (ex.InnerException is SocketException) return true;

        // HttpClient on some frameworks sets HResult for name resolution failures.
        // We keep this broad since we just want "not reachable" -> Unknown.
        return false;
    }
}