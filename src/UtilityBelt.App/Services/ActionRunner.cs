using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UtilityBelt.App.Models;

namespace UtilityBelt.App.Services;

public sealed class ActionRunner
{
    private readonly ToastService _toast;

    public ActionRunner(ToastService toast)
    {
        _toast = toast ?? throw new ArgumentNullException(nameof(toast));
    }

    public void Run(ButtonDefinition def)
    {
        if (def is null) throw new ArgumentNullException(nameof(def));

        try
        {
            var type = (def.Type ?? string.Empty).Trim();

            if (type.Equals("Process", StringComparison.OrdinalIgnoreCase))
            {
                RunProcess(def, runAsAdmin: false);
                return;
            }

            if (type.Equals("ProcessAdmin", StringComparison.OrdinalIgnoreCase))
            {
                RunProcess(def, runAsAdmin: true);
                return;
            }

            if (type.Equals("Url", StringComparison.OrdinalIgnoreCase))
            {
                RunUrl(def);
                return;
            }

            throw new NotSupportedException($"Unknown button type '{def.Type}' (Id={def.Id}).");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // ERROR_CANCELLED: user clicked "No" on the UAC prompt.
            LogService.Log.Information("Elevation prompt cancelled. Id={Id} Type={Type}", def.Id, def.Type);
            _toast.Show("Launch cancelled", "Admin access was not granted.");
        }
        catch (Exception ex)
        {
            LogService.Log.Error(ex, "Action failed. Id={Id} Type={Type}", def.Id, def.Type);
            _toast.Show("Launch failed", ex.Message);
        }
    }

    private static void RunProcess(ButtonDefinition def, bool runAsAdmin)
    {
        if (string.IsNullOrWhiteSpace(def.ExePath))
            throw new InvalidOperationException($"Button '{def.Id}' is missing ExePath.");

        var fullExePath = Path.GetFullPath(def.ExePath, AppContext.BaseDirectory);
        if (!File.Exists(fullExePath))
            throw new FileNotFoundException($"Button '{def.Id}' exe not found: {fullExePath}");

        var psi = new ProcessStartInfo
        {
            FileName = fullExePath,
            Arguments = def.Args ?? string.Empty,
            WorkingDirectory = Path.GetDirectoryName(fullExePath) ?? AppContext.BaseDirectory,
            UseShellExecute = true
        };

        if (!string.IsNullOrWhiteSpace(def.WorkingDirectory))
            psi.WorkingDirectory = Path.GetFullPath(def.WorkingDirectory);

        if (runAsAdmin)
            psi.Verb = "runas";

        Process.Start(psi);
    }

    private static void RunUrl(ButtonDefinition def)
    {
        if (string.IsNullOrWhiteSpace(def.Url))
            throw new InvalidOperationException($"Button '{def.Id}' is missing Url.");

        Process.Start(new ProcessStartInfo(def.Url) { UseShellExecute = true });
    }
}
