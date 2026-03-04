using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using UtilityBelt.App.Models;
using UtilityBelt.App.Services;

namespace UtilityBelt.App;

public partial class App : Application
{
    private static ServiceProvider? _provider;
    private TaskbarIcon? _trayIcon;
    private ToolbarWindow? _toolbar;
    private HotCornerService? _hotCorner;
    private SettingsService? _settingsService;

    private readonly Mutex _mutex = new(true, "UtilityBelt.SingleInstance");

    // Tray menu actions (code-behind click handlers call these)
    private Action? _trayToggleToolbar;
    private Action? _trayReloadConfig;
    private Action? _trayToggleChecks;
    private Action? _trayOpenLogs;
    private Action? _trayExitApp;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!_mutex.WaitOne(0, false))
        {
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (_, ex) =>
        {
            LogService.Log.Fatal(ex.Exception, "DispatcherUnhandledException");
            _provider?.GetRequiredService<ToastService>()?.Show("Unhandled UI exception", ex.Exception.Message);
            ex.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            LogService.Log.Fatal(ex.ExceptionObject as Exception, "UnhandledException");
            _provider?.GetRequiredService<ToastService>()?.Show(
                "Unhandled exception",
                ex.ExceptionObject?.ToString() ?? "(unknown)");
        };

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _provider = BuildProvider(_trayIcon);

        using var scope = _provider.CreateScope();
        var provider = scope.ServiceProvider;

        var scheduler = provider.GetRequiredService<StatusScheduler>();
        var trayIconService = provider.GetRequiredService<TrayIconService>();
        _settingsService = provider.GetRequiredService<SettingsService>();
        var toast = provider.GetRequiredService<ToastService>();
        var notificationRouter = provider.GetRequiredService<NotificationRouter>();

        scheduler.AggregateStatusChanged += (_, status) =>
            Dispatcher.Invoke(() => trayIconService.Apply(status));

        scheduler.CheckUpdated += (_, result) =>
            Dispatcher.Invoke(() => notificationRouter.OnCheckUpdated(result));

        scheduler.CheckFailed += (_, evt) =>
            Dispatcher.Invoke(() => notificationRouter.OnCheckFailed(evt.CheckId, evt.Exception));

        _trayToggleToolbar = () =>
        {
            LogService.Log.Information("Tray menu: Toggle toolbar");
            if (_toolbar is null) return;

            Dispatcher.Invoke(() =>
            {
                if (_toolbar.IsVisible)
                    _toolbar.Hide();
                else
                {
                    _toolbar.Show();
                    _toolbar.Activate();
                }
            });
        };

        _trayReloadConfig = () =>
        {
            LogService.Log.Information("Tray menu: Reload config");
            LoadConfig(provider, _settingsService, notificationRouter, toast, scheduler);
        };

        _trayToggleChecks = () =>
        {
            LogService.Log.Information("Tray menu: Toggle checks");
            scheduler.ToggleRunning();
        };

        _trayOpenLogs = () =>
        {
            LogService.Log.Information("Tray menu: Open logs folder");
            try
            {
                var dir = LogService.LogsDirectory();
                Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogService.Log.Warning(ex, "Failed to open logs folder");
            }
        };

        _trayExitApp = () =>
        {
            LogService.Log.Information("Tray menu: Exit");

            // Ensure shutdown runs on the WPF UI thread.
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    _hotCorner?.Dispose();
                    scheduler.Dispose();
                    trayIconService.Dispose();
                    _trayIcon?.Dispose();
                    _toolbar?.Close();
                }
                catch (Exception ex)
                {
                    LogService.Log.Warning(ex, "Error during exit cleanup");
                }
                finally
                {
                    Shutdown();
                }
            });
        };

        _toolbar = new ToolbarWindow();
        _toolbar.LocationChanged += (_, _) => PersistToolbarLocation();

        LoadConfig(provider, _settingsService, notificationRouter, toast, scheduler);
        _toolbar.Hide();
    }

    // Tray menu click handlers (wired in App.xaml)
    private void TrayToggleToolbar_Click(object sender, RoutedEventArgs e) => _trayToggleToolbar?.Invoke();
    private void TrayReloadConfig_Click(object sender, RoutedEventArgs e) => _trayReloadConfig?.Invoke();
    private void TrayToggleChecks_Click(object sender, RoutedEventArgs e) => _trayToggleChecks?.Invoke();
    private void TrayOpenLogs_Click(object sender, RoutedEventArgs e) => _trayOpenLogs?.Invoke();
    private void TrayExit_Click(object sender, RoutedEventArgs e) => _trayExitApp?.Invoke();

    private static ServiceProvider BuildProvider(TaskbarIcon trayIcon)
    {
        var services = new ServiceCollection();
        services.AddSingleton(trayIcon);
        services.AddSingleton<SettingsService>();
        services.AddSingleton<StatusScheduler>();
        services.AddSingleton(sp => new ToastService(sp.GetRequiredService<TaskbarIcon>()));
        services.AddSingleton<NotificationRouter>();
        services.AddSingleton<TrayIconService>(sp => new TrayIconService(sp.GetRequiredService<TaskbarIcon>()));
        services.AddTransient<ActionRunner>();
        return services.BuildServiceProvider();
    }

    private void LoadConfig(
        IServiceProvider provider,
        SettingsService settingsService,
        NotificationRouter notificationRouter,
        ToastService toast,
        StatusScheduler scheduler)
    {
        var defaultPath = Path.Combine(AppContext.BaseDirectory, "appsettings.default.json");
        var settings = settingsService.LoadOrCreateDefault(defaultPath);
        settingsService.Save(settings);

        notificationRouter.Settings = settings.Notifications;

        var actionRunner = provider.GetRequiredService<ActionRunner>();
        var toolbarVm = new ToolbarViewModel(actionRunner, settings.Buttons, settings.UI.Columns);
        _toolbar!.ApplySettingsAndViewModel(settings.UI, toolbarVm);

        ConfigureHotCorner(settings.UI);
        ConfigureScheduler(scheduler, settings);
    }

    private void ConfigureHotCorner(UiSettings ui)
    {
        _hotCorner?.Dispose();
        _hotCorner = new HotCornerService(() =>
        {
            if (_toolbar is null) return;
            if (_toolbar.IsVisible) return;

            Dispatcher.Invoke(() =>
            {
                _toolbar.ShowAnimated();
            });
        }, ui.HotCornerSize);

        _hotCorner.Enabled = true;
    }


    private void PersistToolbarLocation()
    {
        try
        {
            if (_toolbar is null) return;
            if (_settingsService is null) return;

            var ui = _toolbar.CurrentUiSettings;
            if (ui is null || !ui.RememberPosition) return;
            if (!_toolbar.IsVisible) return;

            ui.Left = _toolbar.Left;
            ui.Top = _toolbar.Top;

            var settings = _settingsService.Load();
            settings.UI.Left = ui.Left;
            settings.UI.Top = ui.Top;
            settings.UI.RememberPosition = ui.RememberPosition;
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            LogService.Log.Warning(ex, "Failed to persist toolbar location");
        }
    }

    private static void ConfigureScheduler(StatusScheduler scheduler, AppSettings settings)
    {
        var checks = new List<IStatusCheck>();
        foreach (var def in settings.StatusChecks)
        {
            var type = (def.Type ?? string.Empty).Trim();

            if (type.Equals("Ping", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(def.Host) || string.IsNullOrWhiteSpace(def.Id)) continue;
                checks.Add(new PingStatusCheck(def.Id, def.Host, TimeSpan.FromSeconds(Math.Max(5, def.IntervalSeconds)), def.TimeoutMs));
            }
            else if (type.Equals("DiskSpace", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(def.Drive) || string.IsNullOrWhiteSpace(def.Id)) continue;
                checks.Add(new DiskSpaceStatusCheck(def.Id, def.Drive, TimeSpan.FromSeconds(Math.Max(10, def.IntervalSeconds)), def.WarnBelowPercent, def.ErrorBelowPercent));
            }
            else if (type.Equals("HttpHealth", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(def.Url) || string.IsNullOrWhiteSpace(def.Id)) continue;
                if (!Uri.TryCreate(def.Url, UriKind.Absolute, out var url)) continue;

                checks.Add(new HttpHealthStatusCheck(
                    def.Id,
                    url,
                    TimeSpan.FromSeconds(Math.Max(5, def.IntervalSeconds)),
                    TimeSpan.FromMilliseconds(Math.Max(250, def.TimeoutMs))));
            }
        }

        scheduler.Configure(checks);
    }

    public static T GetService<T>()
    {
        if (_provider == null) throw new Exception("The _provider was null");
        return _provider.GetRequiredService<T>();
    }

}