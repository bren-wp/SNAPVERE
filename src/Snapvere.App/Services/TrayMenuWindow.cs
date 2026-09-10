using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.System;

namespace Snapvere.App.Services;

/// <summary>
/// Compact premium command surface shown from the notification-area icon.
/// It deliberately stays small and capture-first; SNAPVERE does not open a
/// dashboard during normal startup.
/// </summary>
public sealed class TrayMenuWindow : Window
{
    private const int FlyoutWidth = 364;
    private const int FlyoutHeight = 528;

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

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Brush(0xFF, 0x08, 0x09, 0x0F),
            Padding = new Thickness(14)
        };
        root.KeyDown += Root_KeyDown;
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(BuildHeader());

        var actions = new StackPanel
        {
            Spacing = 5,
            Margin = new Thickness(0, 14, 0, 0)
        };
        actions.Children.Add(CreateMenuButton("\uE722", "Capture region", "Print Screen", TrayCommand.RegionCapture, primary: true));
        actions.Children.Add(CreateMenuButton("\uE7F4", "Capture window", "Ctrl + Shift + 2", TrayCommand.WindowCapture));
        actions.Children.Add(CreateMenuButton("\uE7F8", "Capture screen", "Ctrl + Shift + 4", TrayCommand.ScreenCapture));
        actions.Children.Add(CreateSeparator());
        actions.Children.Add(CreateMenuButton("\uE838", "Open capture folder", "", TrayCommand.OpenCaptureFolder));
        actions.Children.Add(CreateActionButton("\uE81C", "Recent captures", "", _recentCapturesHandler));
        actions.Children.Add(CreateMenuButton("\uE713", "Options", "", TrayCommand.Show));
        actions.Children.Add(CreateMenuButton("\uE946", "About SNAPVERE", "", TrayCommand.About));
        actions.Children.Add(CreateSeparator());
        actions.Children.Add(CreateMenuButton("\uE7E8", "Exit", "", TrayCommand.Exit, danger: true));
        Grid.SetRow(actions, 1);
        root.Children.Add(actions);

        var footer = new Grid { Margin = new Thickness(4, 10, 4, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var ready = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 7,
            VerticalAlignment = VerticalAlignment.Center
        };
        ready.Children.Add(new Border
        {
            Width = 7,
            Height = 7,
            CornerRadius = new CornerRadius(4),
            Background = Brush(0xFF, 0x56, 0xD6, 0xAE)
        });
        ready.Children.Add(Text("Ready", 10, Muted));
        footer.Children.Add(ready);

        var version = typeof(TrayMenuWindow).Assembly.GetName().Version?.ToString(3) ?? "dev";
        var versionText = Text($"v{version}", 10, Subtle);
        Grid.SetColumn(versionText, 1);
        footer.Children.Add(versionText);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        return new Border
        {
            Background = Brush(0xFF, 0x08, 0x09, 0x0F),
            BorderBrush = Brush(0xFF, 0x29, 0x2D, 0x3A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Child = root
        };
    }

    private FrameworkElement BuildHeader()
    {
        var header = new Grid { Padding = new Thickness(2, 2, 2, 0) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        header.Children.Add(BuildBrandMark());

        var title = new StackPanel
        {
            Spacing = 1,
            Margin = new Thickness(11, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var product = Text("SNAPVERE", 16, Strong, Microsoft.UI.Text.FontWeights.Bold);
        product.CharacterSpacing = 60;
        title.Children.Add(product);
        title.Children.Add(Text("Capture. Edit. Done.", 10, Muted));
        Grid.SetColumn(title, 1);
        header.Children.Add(title);

        var privacy = new Border
        {
            Padding = new Thickness(8, 5, 8, 5),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0x30, 0x3D, 0xC6, 0xA1),
            BorderBrush = Brush(0x55, 0x54, 0xD7, 0xB4),
            BorderThickness = new Thickness(1),
            Child = Text("LOCAL", 8, Brush(0xFF, 0x83, 0xE4, 0xC6), Microsoft.UI.Text.FontWeights.Bold)
        };
        Grid.SetColumn(privacy, 2);
        header.Children.Add(privacy);
        return header;
    }

    private static FrameworkElement BuildBrandMark()
    {
        var mark = new Grid { Width = 42, Height = 42 };
        mark.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(13),
            Background = AccentGradient(),
            BorderBrush = Brush(0x66, 0xC9, 0xC0, 0xFF),
            BorderThickness = new Thickness(1)
        });

        var shard = new Grid
        {
            Width = 24,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        shard.Children.Add(new Border
        {
            Width = 7,
            Height = 24,
            CornerRadius = new CornerRadius(4),
            Background = Strong,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = 38 }
        });
        shard.Children.Add(new Border
        {
            Width = 4,
            Height = 17,
            CornerRadius = new CornerRadius(3),
            Background = Brush(0xE8, 0xBD, 0xED, 0xFF),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, -5, 0, 0),
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = 38 }
        });
        mark.Children.Add(shard);
        return mark;
    }

    private Button CreateMenuButton(
        string glyph,
        string title,
        string shortcut,
        TrayCommand command,
        bool primary = false,
        bool danger = false)
        => CreateButton(glyph, title, shortcut, () => InvokeCommand(command), primary, danger);

    private Button CreateActionButton(string glyph, string title, string shortcut, Action action)
        => CreateButton(glyph, title, shortcut, () => InvokeAction(action), primary: false, danger: false);

    private static Button CreateButton(
        string glyph,
        string title,
        string shortcut,
        Action action,
        bool primary,
        bool danger)
    {
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var iconTile = new Border
        {
            Width = 31,
            Height = 31,
            CornerRadius = new CornerRadius(10),
            Background = primary
                ? Brush(0x66, 0x6D, 0x52, 0xE8)
                : danger
                    ? Brush(0x28, 0xEC, 0x5F, 0x74)
                    : Brush(0xFF, 0x16, 0x18, 0x22),
            BorderBrush = primary
                ? Brush(0x70, 0xA8, 0x91, 0xFF)
                : danger
                    ? Brush(0x40, 0xEC, 0x5F, 0x74)
                    : Brush(0xFF, 0x29, 0x2D, 0x3A),
            BorderThickness = new Thickness(1),
            Child = new FontIcon
            {
                Glyph = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 14,
                Foreground = danger ? Brush(0xFF, 0xFF, 0xA9, 0xB4) : Strong
            }
        };
        content.Children.Add(iconTile);

        var label = Text(title, 11.5, danger ? Brush(0xFF, 0xFF, 0xB6, 0xBF) : Strong, Microsoft.UI.Text.FontWeights.SemiBold);
        label.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(label, 1);
        content.Children.Add(label);

        if (!string.IsNullOrWhiteSpace(shortcut))
        {
            var hint = Text(shortcut, 9, primary ? Brush(0xFF, 0xD4, 0xCD, 0xFF) : Subtle);
            hint.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(hint, 2);
            content.Children.Add(hint);
        }

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            MinHeight = 43,
            Padding = new Thickness(8, 6, 9, 6),
            CornerRadius = new CornerRadius(12),
            Background = primary ? Brush(0x58, 0x59, 0x42, 0xB2) : Transparent,
            BorderBrush = primary ? Brush(0x80, 0x9D, 0x86, 0xFF) : Transparent,
            BorderThickness = new Thickness(1),
            Content = content
        };
        AutomationProperties.SetName(button, title);
        button.Click += (_, _) => action();
        return button;
    }

    private static Border CreateSeparator()
        => new()
        {
            Height = 1,
            Margin = new Thickness(7, 3, 7, 3),
            Background = Brush(0xFF, 0x22, 0x25, 0x30)
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

    private static LinearGradientBrush AccentGradient()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1)
        };
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x62, 0x4B, 0xE8), Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x8B, 0x5C, 0xF6), Offset = 0.58 });
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x37, 0xB6, 0xD4), Offset = 1 });
        return brush;
    }

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Strong => Brush(0xFF, 0xF7, 0xF5, 0xFF);
    private static SolidColorBrush Muted => Brush(0xFF, 0xAF, 0xAC, 0xBD);
    private static SolidColorBrush Subtle => Brush(0xFF, 0x77, 0x77, 0x88);
    private static SolidColorBrush Transparent => Brush(0x00, 0, 0, 0);

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
