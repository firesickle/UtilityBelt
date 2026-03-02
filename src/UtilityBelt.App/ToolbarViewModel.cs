using System.Collections.ObjectModel;
using System.Windows.Input;
using UtilityBelt.App.Models;
using UtilityBelt.App.Services;

namespace UtilityBelt.App;

public sealed class ToolbarViewModel
{
    private readonly ActionRunner _runner;
    private bool _pinned;

    public ToolbarViewModel(ActionRunner runner, IEnumerable<ButtonDefinition> buttons, int columns)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));

        Columns = Math.Max(1, columns);
        Buttons = new ObservableCollection<ButtonDefinition>((buttons ?? Enumerable.Empty<ButtonDefinition>()));

        RunButtonCommand = new RelayCommand(p =>
        {
            if (p is ButtonDefinition def)
                _runner.Run(def);
        });

        BeginDragCommand = new RelayCommand(_ => BeginDragRequested?.Invoke(this, EventArgs.Empty));

        TogglePinnedCommand = new RelayCommand(_ =>
        {
            _pinned = !_pinned;
            PinnedChanged?.Invoke(this, _pinned);
        });
    }

    public int Columns { get; }

    public ObservableCollection<ButtonDefinition> Buttons { get; }

    public ICommand RunButtonCommand { get; }

    /// <summary>
    /// Raised when the user initiates a drag on the toolbar.
    /// The view (ToolbarWindow) handles DragMove().
    /// </summary>
    public ICommand BeginDragCommand { get; }

    /// <summary>
    /// Left-clicking the window background toggles "pinned" mode.
    /// When pinned, auto-hide is suppressed until the next time the window is shown.
    /// </summary>
    public ICommand TogglePinnedCommand { get; }

    public event EventHandler? BeginDragRequested;

    public event EventHandler<bool>? PinnedChanged;
}
