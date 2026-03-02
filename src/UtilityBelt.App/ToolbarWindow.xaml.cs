using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using UtilityBelt.App.Models;

namespace UtilityBelt.App;

public partial class ToolbarWindow : Window
{
    private readonly DispatcherTimer _dismissTimer;
    private bool _pinned;
    private bool _isAnimating;

    public ToolbarWindow()
    {
        InitializeComponent();

        _dismissTimer = new DispatcherTimer();
        _dismissTimer.Tick += (_, _) =>
        {
            _dismissTimer.Stop();
            HideAnimated();
        };
    }

    public int Columns { get; private set; } = 4;

    public UiSettings? CurrentUiSettings { get; private set; }

    public void ApplySettingsAndViewModel(UiSettings ui, ToolbarViewModel vm)
    {
        if (ui is null) throw new ArgumentNullException(nameof(ui));
        if (vm is null) throw new ArgumentNullException(nameof(vm));

        CurrentUiSettings = ui;

        Topmost = ui.AlwaysOnTop;
        Width = ui.Width;
        Columns = Math.Max(1, ui.Columns);

        vm.BeginDragRequested += (_, _) => BeginDrag();
        vm.PinnedChanged += (_, pinned) => _pinned = pinned;
        DataContext = vm;

        ApplyPosition(ui);
        ConfigureAutoHide(ui);
    }

    private void ConfigureAutoHide(UiSettings ui)
    {
        _dismissTimer.Stop();

        if (!ui.AutoHideEnabled)
            return;

        _dismissTimer.Interval = TimeSpan.FromMilliseconds(Math.Clamp(ui.AutoHideDelayMs, 0, 10_000));
    }

    public void ShowAnimated()
    {
        _pinned = false; // resets each time it shows

        var ui = CurrentUiSettings;
        if (ui is null || !ui.SlideAnimationEnabled)
        {
            Show();
            Activate();
            return;
        }

        var wa = SystemParameters.WorkArea;
        var margin = ui.MarginFromEdge;

        // Slide in from the nearest horizontal edge, based on current (or default) position.
        var showOnRight = (Left + (Width / 2)) >= (wa.Left + wa.Width / 2);
        var targetLeft = showOnRight ? (wa.Right - Width - margin) : (wa.Left + margin);
        var targetTop = Math.Clamp(Top, wa.Top + margin, Math.Max(wa.Top + margin, wa.Bottom - Height - margin));

        // Start just off-screen.
        Left = showOnRight ? wa.Right + 4 : wa.Left - Width - 4;
        Top = targetTop;

        Show();
        Activate();

        AnimateWindowProperty(LeftProperty, targetLeft, ui.SlideAnimationDurationMs);
    }

    public void HideAnimated()
    {
        var ui = CurrentUiSettings;
        if (ui is null || !ui.SlideAnimationEnabled)
        {
            Hide();
            return;
        }

        if (_isAnimating)
            return;

        var wa = SystemParameters.WorkArea;
        var hideToRight = (Left + (Width / 2)) >= (wa.Left + wa.Width / 2);
        var offscreenLeft = hideToRight ? wa.Right + 4 : wa.Left - Width - 4;

        _isAnimating = true;
        var anim = new DoubleAnimation
        {
            To = offscreenLeft,
            Duration = TimeSpan.FromMilliseconds(Math.Clamp(ui.SlideAnimationDurationMs, 1, 2000)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        anim.Completed += (_, _) =>
        {
            _isAnimating = false;
            Hide();
        };

        BeginAnimation(LeftProperty, anim);
    }

    private void AnimateWindowProperty(DependencyProperty property, double to, int durationMs)
    {
        var anim = new DoubleAnimation
        {
            To = to,
            Duration = TimeSpan.FromMilliseconds(Math.Clamp(durationMs, 1, 2000)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        BeginAnimation(property, anim);
    }

    private void BeginDrag()
    {
        try
        {
            // Hide timer can fight the drag.
            _dismissTimer.Stop();

            DragMove();
        }
        catch
        {
            // DragMove can throw if mouse isn't down; ignore.
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Apply initial position even if settings haven't been loaded yet.
        ApplyPosition(CurrentUiSettings);
    }

    private void ApplyPosition(UiSettings? ui)
    {
        if (ui is null)
        {
            PositionTopRight(12);
            return;
        }

        if (ui.RememberPosition && ui.Left.HasValue && ui.Top.HasValue)
        {
            Left = ui.Left.Value;
            Top = ui.Top.Value;
            ClampToWorkArea();
        }
        else
        {
            PositionTopRight(ui.MarginFromEdge);
        }
    }

    private void ClampToWorkArea()
    {
        var wa = SystemParameters.WorkArea;

        if (Width <= 0 || Height <= 0) return;

        // Clamp the top-left so the window remains reachable.
        Left = Math.Clamp(Left, wa.Left, Math.Max(wa.Left, wa.Right - Width));
        Top = Math.Clamp(Top, wa.Top, Math.Max(wa.Top, wa.Bottom - Height));
    }

    private void PositionTopRight(double margin)
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - margin;
        Top = wa.Top + margin;
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        HideAnimated();
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Right)
        {
            HideAnimated();
            e.Handled = true;
            return;
        }
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        var ui = CurrentUiSettings;
        if (ui is null || !ui.AutoHideEnabled) return;
        if (_pinned) return;

        _dismissTimer.Stop();
        _dismissTimer.Start();
    }

    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        _dismissTimer.Stop();
    }
}
