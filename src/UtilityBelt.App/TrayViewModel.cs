using System;
using System.Windows.Input;

namespace UtilityBelt.App;

public sealed class TrayViewModel
{
    public TrayViewModel(Action toggleToolbar, Action reloadConfig, Action toggleChecks, Action openLogs, Action exit)
    {
        ToggleToolbarCommand = new RelayCommand(_ => toggleToolbar());
        ReloadConfigCommand = new RelayCommand(_ => reloadConfig());
        ToggleChecksCommand = new RelayCommand(_ => toggleChecks());
        OpenLogsCommand = new RelayCommand(_ => openLogs());
        ExitCommand = new RelayCommand(_ => exit());
    }

    public ICommand ToggleToolbarCommand { get; }
    public ICommand ReloadConfigCommand { get; }
    public ICommand ToggleChecksCommand { get; }
    public ICommand OpenLogsCommand { get; }
    public ICommand ExitCommand { get; }
}
