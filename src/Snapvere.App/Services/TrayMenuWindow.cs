using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Snapvere.Shared;
using Windows.Graphics;
using Windows.System;

namespace Snapvere.App.Services;

/// <summary>
/// Tray-first command surface for capture, settings, history and app lifecycle.
/// </summary>
public sealed class TrayMenuWindow : Window
{
    private const int FlyoutWidth = 418;
    private const int FlyoutHeight = 578;

    private readonly Action<TrayCommand> _commandHandler;
    private readonly Action _recentCapturesHandler;
    private readonly Action _languageHandler;
    private readonly string _languageCode;
    private readonly bool _screenRecordingActive;
    private readonly List<TextBlock> _shortcutHints = new();
    private FrameworkElement? _brandMark;
    private TextBlock? _productText;
    private Grid? _header;
    private bool _hasActivated;
    private bool _closingForCommand;

    public TrayMenuWindow(
        Action<TrayCommand> commandHandler,
        Action recentCapturesHandler,
        Action languageHandler,
        string? languageCode = null,
        bool screenRecordingActive = false)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _recentCapturesHandler = recentCapturesHandler ?? throw new ArgumentNullException(nameof(recentCapturesHandler));
        _languageHandler = languageHandler ?? throw new ArgumentNullException(nameof(languageHandler));
        _languageCode = SnapvereLocalization.NormalizeLanguageCode(languageCode ?? SnapvereLanguageState.CurrentLanguageCode);
        _screenRecordingActive = screenRecordingActive;
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

    private string L(string key) => SnapvereLocalization.T(key, _languageCode);

    private string Tagline()
        => string.Equals(_languageCode, "hr", StringComparison.OrdinalIgnoreCase)
            ? "Snimi. Uredi. Gotovo."
            : "Capture. Edit. Done.";

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Surface,
            Padding = new Thickness(18, 18, 18, 14)
        };
        root.KeyDown += Root_KeyDown;
        root.SizeChanged += (_, args) => ApplyResponsiveLayout(root, args.NewSize.Width);
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(BuildHeader());

        var actions = new StackPanel { Spacing = 3, Margin = new Thickness(0, 12, 0, 0) };
        actions.Children.Add(CreateMenuButton("\uE722", L("CaptureRegion"), "Print Screen", TrayCommand.RegionCapture, primary: true));
        actions.Children.Add(CreateMenuButton("\uE7F4", L("CaptureWindow"), "Ctrl + Shift + 2", TrayCommand.WindowCapture));
        actions.Children.Add(CreateMenuButton("\uE7F8", L("CaptureScreen"), "Ctrl + Shift + 3", TrayCommand.ScreenCapture));
        actions.Children.Add(CreateMenuButton(
            "\uE714",
            L(_screenRecordingActive ? "StopScreenRecording" : "StartScreenRecording"),
            string.Empty,
            _screenRecordingActive
                ? TrayCommand.StopScreenRecording
                : TrayCommand.StartScreenRecording,
            danger: _screenRecordingActive));
        actions.Children.Add(CreateSeparator());
        actions.Children.Add(CreateMenuButton("\uE713", L("Settings"), string.Empty, TrayCommand.Show));
        actions.Children.Add(CreateActionButton("\uE81C", L("RecentCaptures"), string.Empty, _recentCapturesHandler));
        actions.Children.Add(CreateMenuButton("\uE838", L("OpenCaptureFolder"), string.Empty, TrayCommand.OpenCaptureFolder));
        actions.Children.Add(CreateMenuButton("\uE946", L("About"), string.Empty, TrayCommand.About));
        actions.Children.Add(CreateSeparator());
        actions.Children.Add(CreateMenuButton("\uE7E8", L("Exit"), string.Empty, TrayCommand.Exit, danger: true));
        var actionScroller = new ScrollViewer
        {
            Margin = new Thickness(0, 12, 0, 0),
            VerticalScrollMode = ScrollMode.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = actions,
            IsTabStop = false
        };
        actions.Margin = new Thickness(0);
        Grid.SetRow(actionScroller, 1);
        root.Children.Add(actionScroller);

        var footer = new Grid { Margin = new Thickness(5, 8, 5, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var ready = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        ready.Children.Add(new Border
        {
            Width = 7,
            Height = 7,
            CornerRadius = new CornerRadius(4),
            Background = _screenRecordingActive
                ? Brush(0xFF, 0xEC, 0x5F, 0x74)
                : Brush(0xFF, 0x56, 0xD6, 0xAE)
        });
        ready.Children.Add(Text(
            L(_screenRecordingActive ? "ScreenRecordingActive" : "Ready"),
            10,
            _screenRecordingActive ? Strong : Muted));
        footer.Children.Add(ready);
        var version = typeof(TrayMenuWindow).Assembly.GetName().Version?.ToString(3);
        var versionText = Text(version is null ? "SNAPVERE" : $"v{version}", 10, Subtle);
        Grid.SetColumn(versionText, 1);
        footer.Children.Add(versionText);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        return new Border
        {
            Background = Surface,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Child = root
        };
    }

    private FrameworkElement BuildHeader()
    {
        var header = new Grid { Height = 58 };
        _header = header;
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _brandMark = BuildBrandMark();
        header.Children.Add(_brandMark);

        var identity = new StackPanel
        {
            Spacing = 0,
            Margin = new Thickness(14, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var product = new TextBlock
        {
            FontSize = 25,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            CharacterSpacing = 35
        };
        product.Inlines.Add(new Run { Text = "SNAP", Foreground = Strong });
        product.Inlines.Add(new Run { Text = "VERE", Foreground = Accent });
        _productText = product;
        identity.Children.Add(product);
        identity.Children.Add(Text(Tagline(), 10.5, Muted));
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);

        var languageButton = new Button
        {
            Width = 38,
            Height = 38,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(12),
            Background = Brush(0xFF, 0x14, 0x1A, 0x28),
            BorderBrush = Brush(0xFF, 0x2B, 0x36, 0x4B),
            BorderThickness = new Thickness(1),
            Content = new FontIcon
            {
                Glyph = "\uE774",
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 15,
                Foreground = Accent
            },
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(languageButton, L("Language"));
        ToolTipService.SetToolTip(languageButton, L("ChooseLanguage"));
        languageButton.Click += (_, _) => InvokeAction(_languageHandler);
        Grid.SetColumn(languageButton, 2);
        header.Children.Add(languageButton);
        return header;
    }

    private static FrameworkElement BuildBrandMark()
    {
        var mark = new Grid { Width = 54, Height = 54 };
        mark.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(16),
            Background = AccentGradient(),
            BorderBrush = Brush(0x70, 0xC9, 0xC0, 0xFF),
            BorderThickness = new Thickness(1)
        });
        var shard = new Grid
        {
            Width = 30,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        shard.Children.Add(new Border
        {
            Width = 8,
            Height = 29,
            CornerRadius = new CornerRadius(4),
            Background = Strong,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = 38 }
        });
        shard.Children.Add(new Border
        {
            Width = 5,
            Height = 20,
            CornerRadius = new CornerRadius(3),
            Background = Brush(0xF0, 0xBD, 0xED, 0xFF),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, -6, 0, 0),
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

    private Button CreateButton(
        string glyph,
        string title,
        string shortcut,
        Action action,
        bool primary,
        bool danger)
    {
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.Children.Add(new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new CornerRadius(9),
            Background = primary ? Brush(0x90, 0x65, 0x47, 0xD8) : danger ? Brush(0x28, 0xEC, 0x5F, 0x74) : Brush(0xFF, 0x14, 0x1A, 0x28),
            BorderBrush = primary ? Brush(0xFF, 0x86, 0x67, 0xF4) : danger ? Brush(0x45, 0xEC, 0x5F, 0x74) : Brush(0xFF, 0x2B, 0x36, 0x4B),
            BorderThickness = new Thickness(1),
            Child = new FontIcon
            {
                Glyph = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 14,
                Foreground = danger ? Brush(0xFF, 0xFF, 0xAE, 0xB7) : Strong
            }
        });
        var label = Text(title, 11.5, danger ? Brush(0xFF, 0xFF, 0xB6, 0xBF) : Strong, Microsoft.UI.Text.FontWeights.SemiBold);
        label.VerticalAlignment = VerticalAlignment.Center;
        label.TextWrapping = TextWrapping.Wrap;
        label.TextTrimming = TextTrimming.CharacterEllipsis;
        label.MaxLines = 2;
        Grid.SetColumn(label, 1);
        content.Children.Add(label);
        if (!string.IsNullOrWhiteSpace(shortcut))
        {
            var hint = Text(shortcut, 9, primary ? Brush(0xFF, 0xDA, 0xD2, 0xFF) : Subtle);
            hint.VerticalAlignment = VerticalAlignment.Center;
            hint.TextTrimming = TextTrimming.CharacterEllipsis;
            hint.MaxWidth = 108;
            _shortcutHints.Add(hint);
            Grid.SetColumn(hint, 2);
            content.Children.Add(hint);
        }
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            MinHeight = primary ? 52 : 38,
            Padding = new Thickness(10, 4, 10, 4),
            CornerRadius = new CornerRadius(12),
            Background = primary ? Brush(0xFF, 0x39, 0x28, 0x78) : Transparent,
            BorderBrush = primary ? Brush(0xFF, 0x86, 0x67, 0xF4) : Transparent,
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
            Margin = new Thickness(8, 2, 8, 2),
            Background = Brush(0xFF, 0x2B, 0x36, 0x4B)
        };

    private void ApplyResponsiveLayout(Grid root, double width)
    {
        var compact = width < 350;
        var veryCompact = width < 300;

        root.Padding = compact
            ? new Thickness(12, 14, 12, 12)
            : new Thickness(18, 18, 18, 14);

        foreach (var hint in _shortcutHints)
        {
            hint.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_brandMark is not null)
        {
            _brandMark.Visibility = veryCompact ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_productText is not null)
        {
            _productText.FontSize = veryCompact ? 20 : compact ? 22 : 25;
        }

        if (_header is not null)
        {
            _header.Height = compact ? 52 : 58;
        }
    }

    private void InvokeCommand(TrayCommand command)
    {
        if (_closingForCommand) return;
        _closingForCommand = true;
        Close();
        _commandHandler(command);
    }

    private void InvokeAction(Action action)
    {
        if (_closingForCommand) return;
        _closingForCommand = true;
        Close();
        action();
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape || _closingForCommand) return;
        e.Handled = true;
        Close();
    }

    private void TrayMenuWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            if (_hasActivated && !_closingForCommand) Close();
            return;
        }
        if (!_hasActivated)
        {
            AppWindow.Resize(DpiAwareWindowSizing.ScaleSizeToWorkArea(this, FlyoutWidth, FlyoutHeight));
            PositionNearCursor();
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
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSizeToWorkArea(this, FlyoutWidth, FlyoutHeight));
    }

    private void PositionNearCursor()
    {
        if (!NativeMethods.GetCursorPos(out var cursor)) return;
        var flyoutWidth = AppWindow.Size.Width;
        var flyoutHeight = AppWindow.Size.Height;
        var monitor = NativeMethods.MonitorFromPoint(cursor, 2);
        var info = new NativeMethods.MonitorInfo { Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (monitor == nint.Zero || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            var fallbackX = cursor.X - flyoutWidth + 24;
            var fallbackY = cursor.Y - flyoutHeight - 12;
            var virtualLeft = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricVirtualScreenX);
            var virtualTop = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricVirtualScreenY);
            var virtualWidth = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricVirtualScreenWidth);
            var virtualHeight = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricVirtualScreenHeight);

            if (virtualWidth > 0 && virtualHeight > 0)
            {
                const int margin = 8;
                var fallbackMinX = virtualLeft + margin;
                var fallbackMaxX = Math.Max(fallbackMinX, virtualLeft + virtualWidth - flyoutWidth - margin);
                var fallbackMinY = virtualTop + margin;
                var fallbackMaxY = Math.Max(fallbackMinY, virtualTop + virtualHeight - flyoutHeight - margin);
                fallbackX = Math.Clamp(fallbackX, fallbackMinX, fallbackMaxX);
                fallbackY = Math.Clamp(fallbackY, fallbackMinY, fallbackMaxY);
            }

            AppWindow.Move(new PointInt32(fallbackX, fallbackY));
            return;
        }
        var minX = info.WorkArea.Left + 8;
        var maxX = Math.Max(minX, info.WorkArea.Right - flyoutWidth - 8);
        var minY = info.WorkArea.Top + 8;
        var maxY = Math.Max(minY, info.WorkArea.Bottom - flyoutHeight - 8);
        AppWindow.Move(new PointInt32(
            Math.Clamp(cursor.X - flyoutWidth + 24, minX, maxX),
            Math.Clamp(cursor.Y - flyoutHeight - 12, minY, maxY)));
    }

    private static TextBlock Text(string value, double size, SolidColorBrush foreground, Windows.UI.Text.FontWeight? weight = null)
        => new() { Text = value, FontSize = size, Foreground = foreground, FontWeight = weight ?? Microsoft.UI.Text.FontWeights.Normal };

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

    private static SolidColorBrush Surface => Brush(0xFF, 0x0D, 0x13, 0x21);
    private static SolidColorBrush Outline => Brush(0xFF, 0x52, 0x61, 0x7F);
    private static SolidColorBrush Strong => Brush(0xFF, 0xF7, 0xF5, 0xFF);
    private static SolidColorBrush Muted => Brush(0xFF, 0xAF, 0xB6, 0xC8);
    private static SolidColorBrush Subtle => Brush(0xFF, 0x7F, 0x89, 0xA1);
    private static SolidColorBrush Accent => Brush(0xFF, 0xA7, 0x7C, 0xFF);
    private static SolidColorBrush Transparent => Brush(0x00, 0, 0, 0);

    private static class NativeMethods
    {
        internal const int SystemMetricVirtualScreenX = 76;
        internal const int SystemMetricVirtualScreenY = 77;
        internal const int SystemMetricVirtualScreenWidth = 78;
        internal const int SystemMetricVirtualScreenHeight = 79;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct Point { internal int X; internal int Y; }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct Rect { internal int Left; internal int Top; internal int Right; internal int Bottom; }

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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int index);
    }
}
