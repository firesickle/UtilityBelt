namespace UtilityBelt.App.Models;

public sealed class AppSettings
{
    public UiSettings UI { get; set; } = new();
    public List<ButtonDefinition> Buttons { get; set; } = new();
    public SchedulerSettings Scheduler { get; set; } = new();
    public List<StatusCheckDefinition> StatusChecks { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
}

public sealed class UiSettings
{
    public bool AlwaysOnTop { get; set; } = true;
    public double Width { get; set; } = 520;
    public int MaxRows { get; set; } = 3;
    public int Columns { get; set; } = 4;
    public double MarginFromEdge { get; set; } = 12;

    // Button sizing
    // These control the size/feel of each toolbar button.
    public double ButtonMinHeight { get; set; } = 44;
    public double ButtonMinWidth { get; set; } = 0; // 0 = let layout decide
    public double ButtonPaddingX { get; set; } = 10;
    public double ButtonPaddingY { get; set; } = 8;

    // Icon sizing
    public double ButtonIconSize { get; set; } = 26;
    public double ButtonIconOnlySize { get; set; } = 34;

    // Font sizing
    public double ButtonTextFontSize { get; set; } = 13;
    public double ButtonTextWithIconFontSize { get; set; } = 11;

    /// <summary>
    /// If true, the toolbar will use the saved Left/Top coordinates.
    /// If false, it will snap to the top-right.
    /// </summary>
    public bool RememberPosition { get; set; } = true;

    public double? Left { get; set; }
    public double? Top { get; set; }

    /// <summary>
    /// Enables the hot-corner trigger.
    /// </summary>
    public bool HotCornerEnabled { get; set; } = true;

    /// <summary>
    /// Hot corner size in pixels.
    /// </summary>
    public int HotCornerSize { get; set; } = 4;

    /// <summary>
    /// If true, shows a small always-on-top indicator at the trigger corner.
    /// </summary>
    public bool HotCornerIndicatorEnabled { get; set; } = true;

    /// <summary>
    /// Indicator square size in pixels.
    /// </summary>
    public int HotCornerIndicatorSize { get; set; } = 10;

    /// <summary>
    /// Indicator opacity (0-1).
    /// </summary>
    public double HotCornerIndicatorOpacity { get; set; } = 0.35;

    // Auto-hide behavior
    public bool AutoHideEnabled { get; set; } = true;
    public int AutoHideDelayMs { get; set; } = 350;

    // Animation
    public bool SlideAnimationEnabled { get; set; } = true;
    public int SlideAnimationDurationMs { get; set; } = 160;
}

public sealed class SchedulerSettings
{
    public bool Enabled { get; set; } = true;
}

public sealed class NotificationSettings
{
    public bool Enabled { get; set; } = true;

    public bool ToastOnWarn { get; set; } = true;
    public bool ToastOnError { get; set; } = true;
    public bool ToastOnRecovery { get; set; } = true;
    public bool ToastOnUnknown { get; set; } = false;
    public bool ToastOnExceptions { get; set; } = true;

    // Helps reduce spam for flapping checks.
    public int MinSecondsBetweenToasts { get; set; } = 10;
}

public sealed class StatusCheckDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Ping | DiskSpace | HttpHealth
    public int IntervalSeconds { get; set; } = 30;

    // Ping
    public string? Host { get; set; }
    public int TimeoutMs { get; set; } = 1000;

    // DiskSpace
    public string? Drive { get; set; } = "C";
    public int WarnBelowPercent { get; set; } = 15;
    public int ErrorBelowPercent { get; set; } = 5;

    // HttpHealth
    public string? Url { get; set; }
}

public sealed class ButtonDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Process | Url | ProcessAdmin

    // Look & feel
    public string? PathData { get; set; } // SVG Path "d" attribute content
    public string? BackColor { get; set; }
    public string? ForegroundColor { get; set; }

    // Process / ProcessAdmin
    public string? ExePath { get; set; }
    public string? Args { get; set; }
    public string? WorkingDirectory { get; set; }

    // Url
    public string? Url { get; set; }

    public string? Tooltip { get; set; }
}
