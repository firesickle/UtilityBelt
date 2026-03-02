using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace UtilityBelt.App.Services;

/// <summary>
/// Simple hot-corner watcher. When the cursor enters the top-right corner of the primary work area,
/// we invoke the configured callback. This is implemented via polling to avoid global hooks.
/// </summary>
public sealed class HotCornerService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Action _onTriggered;
    private bool _wasInCorner;

    // Corner size in pixels (square).
    private readonly int _cornerSize;

    public bool Enabled
    {
        get => _timer.IsEnabled;
        set
        {
            if (value) _timer.Start();
            else _timer.Stop();
        }
    }

    public HotCornerService(Action onTriggered, int cornerSize = 4)
    {
        _onTriggered = onTriggered ?? throw new ArgumentNullException(nameof(onTriggered));
        _cornerSize = Math.Clamp(cornerSize, 1, 40);

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += (_, _) => Tick();
    }

    private void Tick()
    {
        if (!TryGetCursorPos(out var p)) return;

        // WorkArea excludes the taskbar.
        var wa = SystemParameters.WorkArea;
        var inCorner = p.X >= wa.Right - _cornerSize && p.X <= wa.Right &&
                       p.Y >= wa.Top && p.Y <= wa.Top + _cornerSize;

        if (inCorner && !_wasInCorner)
            _onTriggered();

        _wasInCorner = inCorner;
    }

    private static bool TryGetCursorPos(out POINT p) => GetCursorPos(out p);

    public void Dispose() => _timer.Stop();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
