using System.Drawing;
using System.IO;
using System.Reflection;
using Hardcodet.Wpf.TaskbarNotification;
using UtilityBelt.App.Models;

namespace UtilityBelt.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;

    private Icon? _iconUnknown;
    private Icon? _iconOk;
    private Icon? _iconWarn;
    private Icon? _iconError;

    public TrayIconService(TaskbarIcon taskbarIcon)
    {
        _taskbarIcon = taskbarIcon;
        LoadIcons();
        Apply(StatusLevel.Unknown);
    }

    public void Apply(StatusLevel status)
    {
        _taskbarIcon.Icon = status switch
        {
            StatusLevel.Ok => _iconOk,
            StatusLevel.Warn => _iconWarn,
            StatusLevel.Error => _iconError,
            _ => _iconUnknown
        };

        _taskbarIcon.ToolTipText = $"UtilityBelt ({status})";
    }

    private void LoadIcons()
    {
        // For single-file publish: prefer embedded .ico resources.
        // Fallback: load loose .ico files next to the exe (useful during dev).

        _iconUnknown = TryLoadEmbedded("tray_unknown.ico") ?? TryLoadFromDisk("tray_unknown.ico");
        _iconOk = TryLoadEmbedded("tray_ok.ico") ?? TryLoadFromDisk("tray_ok.ico");
        _iconWarn = TryLoadEmbedded("tray_warn.ico") ?? TryLoadFromDisk("tray_warn.ico");
        _iconError = TryLoadEmbedded("tray_error.ico") ?? TryLoadFromDisk("tray_error.ico");

        // If nothing was found, generate simple fallback icons.
        if (_iconUnknown is null)
            _iconUnknown = MakeSolidIcon(Color.Gray);
        if (_iconOk is null)
            _iconOk = MakeSolidIcon(Color.FromArgb(0x2E, 0xCC, 0x71));
        if (_iconWarn is null)
            _iconWarn = MakeSolidIcon(Color.FromArgb(0xF1, 0xC4, 0x0F));
        if (_iconError is null)
            _iconError = MakeSolidIcon(Color.FromArgb(0xE7, 0x4C, 0x3C));
    }

    private static Icon? TryLoadFromDisk(string fileName)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, fileName);
            return File.Exists(path) ? new Icon(path) : null;
        }
        catch (Exception ex)
        {
            LogService.Log.Warning(ex, "Failed loading tray icon from disk {FileName}", fileName);
            return null;
        }
    }

    private static Icon? TryLoadEmbedded(string fileName)
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();

            // Resource names are typically like: "UtilityBelt.App.tray_unknown.ico"
            var resourceName = asm
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
                return null;

            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null)
                return null;

            // Icon ctor will read the stream into memory; disposing stream is fine.
            return new Icon(stream);
        }
        catch (Exception ex)
        {
            LogService.Log.Warning(ex, "Failed loading tray icon embedded resource {FileName}", fileName);
            return null;
        }
    }

    private static Icon MakeSolidIcon(Color color)
    {
        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, 1, 1, 14, 14);
            using var pen = new Pen(Color.FromArgb(220, 0, 0, 0));
            g.DrawEllipse(pen, 1, 1, 14, 14);
        }

        // Note: Icon.FromHandle doesn't take ownership of the HICON.
        // We only use this as a fallback if shipped icons are missing.
        var hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public void Dispose()
    {
        _iconUnknown?.Dispose();
        _iconOk?.Dispose();
        _iconWarn?.Dispose();
        _iconError?.Dispose();
    }
}
