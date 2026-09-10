using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.System;

namespace Snapvere.App.Services;

/// <summary>
/// Branded tray flyout shown only from the notification-area icon. The hidden
/// capture coordinator stays out of sight; this window is intentionally a
/// compact command surface, not a second application dashboard.
/// </summary>
public sealed class TrayMenuWindow : Window
{
    private const int FlyoutWidth = 372;
    private const int FlyoutHeight = 548;

    private readonly Action<TrayCommand> _commandHandler;
    private readonly Action _recentCapturesHandler;
    private bool _hasActivated;
    private bool _closingForCommand;

    public TrayMenuWindow(Action<TrayCommand> commandHandler, Action recentCapturesHandler)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _recentCapturesHandler = recentCapturesHandler ?? throw new ArgumentNullException(nameof(recentCapturesHandler));
        Title = "SNAPVERE";
        Content = BuildContent();
        ConfigureWindow();
        Activated += TrayMenuWindow_Activated;
    }

    public void ShowNearTray()
    {
        PositionNearCursor();
        Activate();
    }

    private UIElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Brush(0x0D, 0x12, 0x20),
            Padding = new Thickness(14)
        };
        root.KeyDown += Root_KeyDown;
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = BuildHeader();
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var actions = new StackPanel
        {
            Spacing = 5,
            Margin = new Thickness(0, 12, 0, 0)
        };
        actions.Children.Add(CreateMenuButton("\uE8B7", "Capture region", "Print Screen · Ctrl + Shift + 1", TrayCommand.RegionCapture, true));
        actions.Children.Add(CreateMenuButton("\uE737", "Capture window", "Ctrl + Shift + 2", TrayCommand.WindowCapture));
        actions.Children.Add(CreateMenuButton("\uE7F4", "Capture screen", "Ctrl + Shift + 4", TrayCommand.ScreenCapture));
        actions.Children.Add(CreateSeparator());
        actions.Children.Add(CreateMenuButton("\uE838", "Open capture folder", "Pictures\\SNAPVERE", TrayCommand.OpenCaptureFolder));
        actions.Children.Add(CreateActionButton("\uE81C", "Recent captures", string.Empty, _recentCapturesHandler));
        actions.Children.Add(CreateMenuButton("\uE713", "Options / Preferences", string.Empty, TrayCommand.Show));
        actions.Children.Add(CreateMenuButton("\uE946", "About SNAPVERE", string.Empty, TrayCommand.About));
        actions.Children.Add(CreateSeparator());
        actions.Children.Add(CreateMenuButton("\uE7E8", "Exit SNAPVERE", string.Empty, TrayCommand.Exit));

        Grid.SetRow(actions, 1);
        root.Children.Add(actions);

        var footer = new Grid
        {
            Margin = new Thickness(2, 10, 2, 0)
        };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var ready = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        ready.Children.Add(new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = Brush(0x38, 0xD9, 0x83)
        });
        ready.Children.Add(Text("Ready in tray", 10, Brush(0xA8, 0xB5, 0xCC)));
        footer.Children.Add(ready);

        var version = typeof(TrayMenuWindow).Assembly.GetName().Version?.ToString(3) ?? "dev";
        var versionText = Text($"v{version}", 10, Brush(0x81, 0x8D, 0xA3));
        Grid.SetColumn(versionText, 1);
        footer.Children.Add(versionText);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        return new Border
        {
            Background = Brush(0x0D, 0x12, 0x20),
            BorderBrush = Brush(0x42, 0x4E, 0x68),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Child = root
        };
    }

    private Grid BuildHeader()
    {
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var mark = BuildBrandMark();
        header.Children.Add(mark);

        var title = new StackPanel
        {
            Spacing = 1,
            Margin = new Thickness(11, 1, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        title.Children.Add(Text("SNAPVERE", 18, Brush(0xF7, 0xF5, 0xFF), Microsoft.UI.Text.FontWeights.SemiBold));
        title.Children.Add(Text("Capture. Edit. Done.", 10, Brush(0x9E, 0xA9, 0xBE)));
        Grid.SetColumn(title, 1);
        header.Children.Add(title);
        return header;
    }

    private static Grid BuildBrandMark()
    {
        var mark = new Grid
        {
            Width = 44,
            Height = 44
        };
        mark.Children.Add(new Border
        {
            Background = Brush(0x1A, 0x16, 0x30),
            BorderBrush = Brush(0x68, 0x50, 0xD8),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11)
        });

        mark.Children.Add(new Border
        {
            Width = 8,
            Height = 27,
            CornerRadius = new CornerRadius(5),
            Background = Brush(0x8F, 0x5D, 0xFF),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = 38 }
        });
        mark.Children.Add(new Border
        {
            Width = 4,
            Height = 22,
            CornerRadius = new CornerRadius(3),
            Background = Brush(0xD6, 0xB9, 0xFF),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(7, -4, 0, 0),
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = 38 }
        });
        return mark;
    }

    private Button CreateMenuButton(string glyph, string title, string shortcut, TrayCommand command, bool primary = false)
        => CreateButton(glyph, title, shortcut, () => InvokeCommand(command), primary);

    private Button CreateActionButton(string glyph, string title, string shortcut, Action action)
        => CreateButton(glyph, title, shortcut, () => InvokeAction(action), primary: false);

    private static Button CreateButton(
        string glyph,
        string title,
        string shortcut,
        Action action,
        bool primary)
    {
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 17,
            Foreground = primary ? Brush(0xF1, 0xEA, 0xFF) : Brush(0x91, 0xA7, 0xFF),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        content.Children.Add(icon);

        var label = Text(title, 12, Brush(0xF4, 0xF5, 0xF8), Microsoft.UI.Text.FontWeights.SemiBold);
        label.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(label, 1);
        content.Children.Add(label);

        if (!string.IsNullOrWhiteSpace(shortcut))
        {
            var hint = Text(shortcut, 9, primary ? Brush(0xD9, 0xD0, 0xFF) : Brush(0x8D, 0x99, 0xAE));
            hint.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(hint, 2);
            content.Children.Add(hint);
        }

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(9, 10, 10, 10),
            CornerRadius = new CornerRadius(9),
            Background = primary ? Brush(0x42, 0x31, 0x9A) : Brush(0x10, 0x16, 0x25),
            BorderBrush = primary ? Brush(0x8A, 0x6C, 0xFF) : Brush(0x26, 0x31, 0x47),
            BorderThickness = new Thickness(1),
            Content = content
        };
        button.Click += (_, _) => action();
        return button;
    }

    private static Border CreateSeparator()
        => new()
        {
            Height = 1,
            Margin = new Thickness(4, 4, 4, 4),
            Background = Brush(0x2A, 0x35, 0x4A)
        };

    private void InvokeCommand(TrayCommand command)
    {
        if (_closingForCommand)
        {
            return;
        }

        _closingForCommand = true;
        Close();
        _commandHandler(command);
    }

    private void InvokeAction(Action action)
    {
        if (_closingForCommand)
        {
            return;
        }

        _closingForCommand = true;
        Close();
        action();
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape || _closingForCommand)
        {
            return;
        }

        e.Handled = true;
        Close();
    }

    private void TrayMenuWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            if (_hasActivated && !_closingForCommand)
            {
                Close();
            }
            return;
        }

        _hasActivated = true;
    }

    private void ConfigureWindow()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        AppWindow.Resize(new SizeInt32(FlyoutWidth, FlyoutHeight));
    }

    private void PositionNearCursor()
    {
        if (!NativeMethods.GetCursorPos(out var cursor))
        {
            return;
        }

        var monitor = NativeMethods.MonitorFromPoint(cursor, 2);
        var info = new NativeMethods.MonitorInfo
        {
            Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>()
        };

        if (monitor == nint.Zero || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            AppWindow.Move(new PointInt32(cursor.X - FlyoutWidth + 24, cursor.Y - FlyoutHeight - 12));
            return;
        }

        var minX = info.WorkArea.Left + 8;
        var maxX = Math.Max(minX, info.WorkArea.Right - FlyoutWidth - 8);
        var minY = info.WorkArea.Top + 8;
        var maxY = Math.Max(minY, info.WorkArea.Bottom - FlyoutHeight - 8);
        var x = Math.Clamp(cursor.X - FlyoutWidth + 24, minX, maxX);
        var y = Math.Clamp(cursor.Y - FlyoutHeight - 12, minY, maxY);
        AppWindow.Move(new PointInt32(x, y));
    }

    private static TextBlock Text(
        string value,
        double size,
        SolidColorBrush foreground,
        Windows.UI.Text.FontWeight? weight = null)
        => new()
        {
            Text = value,
            FontSize = size,
            Foreground = foreground,
            FontWeight = weight ?? Microsoft.UI.Text.FontWeights.Normal
        };

    private static SolidColorBrush Brush(byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(0xFF, red, green, blue));

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct Point
        {
            internal int X;
            internal int Y;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct Rect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct MonitorInfo
        {
            internal uint Size;
            internal Rect Monitor;
            internal Rect WorkArea;
            internal uint Flags;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out Point point);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern nint MonitorFromPoint(Point point, uint flags);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo monitorInfo);
    }
}
