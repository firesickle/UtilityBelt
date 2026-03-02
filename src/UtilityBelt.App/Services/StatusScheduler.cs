using UtilityBelt.App.Models;

namespace UtilityBelt.App.Services;

public sealed class StatusScheduler : IDisposable
{
    private readonly object _gate = new();
    private readonly List<ScheduledCheck> _checks = new();
    private CancellationTokenSource? _cts;

    public event EventHandler<CheckResult>? CheckUpdated;
    public event EventHandler<(string CheckId, Exception Exception)>? CheckFailed;
    public event EventHandler<StatusLevel>? AggregateStatusChanged;

    public bool IsRunning { get; private set; }
    public StatusLevel AggregateStatus { get; private set; } = StatusLevel.Unknown;

    public void Configure(IEnumerable<IStatusCheck> checks)
    {
        lock (_gate)
        {
            _checks.Clear();
            foreach (var c in checks)
                _checks.Add(new ScheduledCheck(c));
        }

        RecomputeAggregate();
    }

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;

        _cts = new CancellationTokenSource();

        List<ScheduledCheck> snapshot;
        lock (_gate) snapshot = _checks.ToList();

        foreach (var s in snapshot)
            _ = RunLoopAsync(s, _cts.Token);
    }

    public void Stop(bool resetToUnknown = true)
    {
        if (!IsRunning) return;
        IsRunning = false;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (resetToUnknown)
        {
            lock (_gate)
            {
                foreach (var c in _checks)
                    c.Last = null;
            }

            UpdateAggregate(StatusLevel.Unknown);
        }
    }

    public void ToggleRunning()
    {
        if (IsRunning) Stop(true);
        else Start();
    }

    private async Task RunLoopAsync(ScheduledCheck scheduled, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await scheduled.Check.RunAsync(ct);

                lock (_gate)
                    scheduled.Last = result;

                CheckUpdated?.Invoke(this, result);
                RecomputeAggregate();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                CheckFailed?.Invoke(this, (scheduled.Check.Id, ex));

                var result = new CheckResult(
                    scheduled.Check.Id,
                    StatusLevel.Unknown,
                    $"{scheduled.Check.Id} exception: {ex.Message}",
                    DateTimeOffset.UtcNow);

                lock (_gate)
                    scheduled.Last = result;

                CheckUpdated?.Invoke(this, result);
                RecomputeAggregate();
            }

            try
            {
                await Task.Delay(scheduled.Check.Interval, ct);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private void RecomputeAggregate()
    {
        StatusLevel next;
        lock (_gate)
        {
            next = _checks.Select(c => c.Last?.Level ?? StatusLevel.Unknown).DefaultIfEmpty(StatusLevel.Unknown).Max();
        }

        UpdateAggregate(next);
    }

    private void UpdateAggregate(StatusLevel next)
    {
        if (AggregateStatus == next) return;
        AggregateStatus = next;
        AggregateStatusChanged?.Invoke(this, next);
    }

    public void Dispose()
    {
        Stop();
    }

    private sealed class ScheduledCheck
    {
        public ScheduledCheck(IStatusCheck check) => Check = check;
        public IStatusCheck Check { get; }
        public CheckResult? Last { get; set; }
    }
}