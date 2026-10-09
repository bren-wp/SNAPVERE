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
    private const int FlyoutWidth = 420;
    private const int FlyoutHeight = 488;
    private const int FlyoutEdgeMargin = 4;
    private const int CursorGap = 4;
    private const int CursorHorizontalAnchorOffset = 20;

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
            ? (_screenRecordingActive ? "Videosnimanje je u tijeku" : "Spreman za snimanje")
            : (_screenRecordingActive ? "Screen recording in progress" : "Ready to capture");

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = SnapvereBrand.Obsidian,
            Padding = new Thickness(18, 18, 18, 14)
        };
        root.KeyDown += Root_KeyDown;
        root.SizeChanged += (_, args) => ApplyResponsiveLayout(root, args.NewSize.Width);
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(BuildHeader());

        var actions = new StackPanel { Spacing = 8, Margin = new Thickness(0, 14, 0, 0) };
        actions.Children.Add(CreateMenuButton("\uE722", L("CaptureRegion"), "Ctrl + Shift + 1", TrayCommand.RegionCapture, primary: true));
        actions.Children.Add(CreateMenuButton("\uE7F4", L("CaptureWindow"), "Ctrl + Shift + 2", TrayCommand.WindowCapture));
        actions.Children.Add(CreateMenuButton("\uE7F8", L("CaptureScreen"), "Ctrl + Shift + 3", TrayCommand.ScreenCapture));
        actions.Children.Add(CreateMenuButton(
            "\uE714",
            L(_screenRecordingActive ? "StopScreenRecording" : "StartScreenRecording"),
            string.Empty,
            _screenRecordingActive
                ? TrayCommand.StopScreenRecording
                : TrayCommand.StartScreenRecording,
            danger: _screenRecordingActive,
            recording: true));

        var actionScroller = new ScrollViewer
        {
            Margin = new Thickness(0, 10, 0, 0),
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

        // Match the reference hierarchy: three captures, recording, then
        // two readable utility actions. Less frequently used commands remain
        // accessible with keyboard focus and automation labels below.
        var footer = new Grid { Margin = new Thickness(2, 10, 2, 0) };
        footer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        footer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        footer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var quickActions = new Grid();
        quickActions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        quickActions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var folder = CreateFooterLinkButton("\uE838", L("OpenCaptureFolder"),
            () => InvokeCommand(TrayCommand.OpenCaptureFolder));
        quickActions.Children.Add(folder);
        var settings = CreateFooterLinkButton("\uE713", L("Settings"),
            () => InvokeCommand(TrayCommand.Show));
        Grid.SetColumn(settings, 1);
        quickActions.Children.Add(settings);
        footer.Children.Add(quickActions);

        var separator = CreateSeparator();
        separator.Margin = new Thickness(0, 10, 0, 8);
        Grid.SetRow(separator, 1);
        footer.Children.Add(separator);

        var footerBottom = new Grid();
        footerBottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerBottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // The header already communicates readiness/recording status. Use the
        // lower utility area for the reference's truthful privacy assurance.
        var privacyCaption = Text(
            string.Equals(_languageCode, "hr", StringComparison.OrdinalIgnoreCase)
                ? "PRIVATNO. NA TVOM UREĐAJU."
                : "PRIVATE. ON YOUR DEVICE.",
            9,
            SnapvereBrand.Subtle,
            Microsoft.UI.Text.FontWeights.SemiBold);
        privacyCaption.VerticalAlignment = VerticalAlignment.Center;
        footerBottom.Children.Add(privacyCaption);

        var utilities = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        utilities.Children.Add(CreateFooterIconButton("\uE81C", L("RecentCaptures"), () => InvokeAction(_recentCapturesHandler)));
        utilities.Children.Add(CreateFooterIconButton("\uE774", L("Language"), () => InvokeAction(_languageHandler)));
        utilities.Children.Add(CreateFooterIconButton("\uE946", L("About"), () => InvokeCommand(TrayCommand.About)));
        utilities.Children.Add(CreateFooterIconButton("\uE7E8", L("Exit"), () => InvokeCommand(TrayCommand.Exit), danger: true));
        Grid.SetColumn(utilities, 1);
        footerBottom.Children.Add(utilities);
        Grid.SetRow(footerBottom, 2);
        footer.Children.Add(footerBottom);

        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        return new Border
        {
            Background = Surface,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Child = root
        };
    }

    private FrameworkElement BuildHeader()
    {
        var header = new Grid { Height = 58 };
        _header = header;
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _brandMark = SnapvereBrand.CreateMark(50);
        header.Children.Add(_brandMark);

        var identity = new StackPanel
        {
            Spacing = 0,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var product = SnapvereBrand.CreateWordmark(24);
        _productText = product;
        identity.Children.Add(product);
        identity.Children.Add(Text(Tagline(), 10.5,
            _screenRecordingActive ? SnapvereBrand.Danger : SnapvereBrand.Ice));
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);

        return header;
    }

    private Button CreateFooterLinkButton(string glyph, string label, Action action)
    {
        var contents = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        contents.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 15,
            Foreground = SnapvereBrand.Lavender
        });
        contents.Children.Add(Text(label, 10.5, SnapvereBrand.Muted, Microsoft.UI.Text.FontWeights.SemiBold));
        var button = new Button
        {
            MinHeight = 38,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Content = contents,
            Padding = new Thickness(4),
            Background = Brush(0x00, 0, 0, 0),
            BorderBrush = Brush(0x00, 0, 0, 0),
            BorderThickness = new Thickness(0)
        };
        AutomationProperties.SetName(button, label);
        button.Click += (_, _) => action();
        return button;
    }

    private Button CreateFooterIconButton(string glyph, string accessibleName, Action action, bool danger = false)
    {
        var button = new Button
        {
            Width = 34,
            Height = 34,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(10),
            Background = SnapvereBrand.Slate,
            BorderBrush = danger ? Brush(0x55, 0xF0, 0x63, 0x82) : SnapvereBrand.Outline,
            BorderThickness = new Thickness(1),
            Content = new FontIcon
            {
                Glyph = glyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 13,
                Foreground = danger ? SnapvereBrand.Danger : SnapvereBrand.Muted
            }
        };
        AutomationProperties.SetName(button, accessibleName);
        ToolTipService.SetToolTip(button, accessibleName);
        button.Click += (_, _) => action();
        return button;
    }

    private Button CreateMenuButton(
        string glyph,
        string title,
        string shortcut,
        TrayCommand command,
        bool primary = false,
        bool danger = false,
        bool recording = false)
        => CreateButton(glyph, title, shortcut, () => InvokeCommand(command), primary, danger, recording);

    private Button CreateButton(
        string glyph,
        string title,
        string shortcut,
        Action action,
        bool primary,
        bool danger,
        bool recording)
    {
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Recording has a real status indicator, not a fabricated audio control.
        var icon = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(8),
            Background = primary ? Brush(0x25, 0xA4, 0x8B, 0xFF) : Brush(0x00, 0, 0, 0),
            Child = recording
                ? new TextBlock
                {
                    Text = "●",
                    FontSize = 19,
                    Foreground = SnapvereBrand.Danger,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
                : new FontIcon
                {
                    Glyph = glyph,
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 17,
                    Foreground = SnapvereBrand.Lavender
                }
        };
        content.Children.Add(icon);

        var copy = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center
        };
        var titleText = Text(title, 12, SnapvereBrand.Strong, Microsoft.UI.Text.FontWeights.SemiBold);
        titleText.TextWrapping = TextWrapping.Wrap;
        titleText.MaxLines = 2;
        copy.Children.Add(titleText);
        if (!string.IsNullOrWhiteSpace(shortcut))
        {
            var hint = Text(shortcut, 9.5, SnapvereBrand.Muted);
            hint.TextTrimming = TextTrimming.CharacterEllipsis;
            _shortcutHints.Add(hint);
            copy.Children.Add(hint);
        }
        Grid.SetColumn(copy, 1);
        content.Children.Add(copy);

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            MinHeight = recording ? 54 : primary ? 54 : 50,
            Padding = new Thickness(12, 5, 12, 5),
            CornerRadius = new CornerRadius(13),
            Background = recording
                ? Brush(0xFF, 0x2A, 0x21, 0x48)
                : primary ? Brush(0xFF, 0x2F, 0x26, 0x4E) : SnapvereBrand.Slate,
            BorderBrush = recording
                ? Brush(0xFF, 0x69, 0x4E, 0xAB)
                : primary ? Brush(0xFF, 0x77, 0x62, 0xAF) : SnapvereBrand.Outline,
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
            ? new Thickness(12, 12, 12, 10)
            : new Thickness(16, 16, 16, 12);

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
            _productText.FontSize = veryCompact ? 20 : compact ? 22 : 24;
        }

        if (_header is not null)
        {
            _header.Height = compact ? 50 : 54;
        }
    }

    private void InvokeCommand(TrayCommand command)
    {
        if (_closingForCommand) return;
        _closingForCommand = true;
        CloseForActionBestEffort("Close tray menu for command");
        _commandHandler(command);
    }

    private void InvokeAction(Action action)
    {
        if (_closingForCommand) return;
        _closingForCommand = true;
        CloseForActionBestEffort("Close tray menu for action");
        action();
    }

    private void CloseForActionBestEffort(string operation)
    {
        try
        {
            Close();
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record(operation, exception);
        }
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
        var info = new NativeMethods.MonitorInfo
        {
            Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>()
        };

        if (monitor != nint.Zero && NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            var placement = TrayPopupPlacementPolicy.Place(
                cursor.X,
                cursor.Y,
                flyoutWidth,
                flyoutHeight,
                info.WorkArea.Left,
                info.WorkArea.Top,
                info.WorkArea.Right,
                info.WorkArea.Bottom,
                FlyoutEdgeMargin,
                CursorGap,
                CursorHorizontalAnchorOffset);

            AppWindow.Move(new PointInt32(placement.X, placement.Y));
            return;
        }

        var virtualLeft = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricVirtualScreenX);
        var virtualTop = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricVirtualScreenY);
        var virtualWidth = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricVirtualScreenWidth);
        var virtualHeight = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricVirtualScreenHeight);

        if (virtualWidth > 0 && virtualHeight > 0)
        {
            var placement = TrayPopupPlacementPolicy.Place(
                cursor.X,
                cursor.Y,
                flyoutWidth,
                flyoutHeight,
                virtualLeft,
                virtualTop,
                checked(virtualLeft + virtualWidth),
                checked(virtualTop + virtualHeight),
                FlyoutEdgeMargin,
                CursorGap,
                CursorHorizontalAnchorOffset);

            AppWindow.Move(new PointInt32(placement.X, placement.Y));
            return;
        }

        AppWindow.Move(new PointInt32(
            cursor.X - flyoutWidth + CursorHorizontalAnchorOffset,
            cursor.Y - flyoutHeight - CursorGap));
    }

    private static TextBlock Text(string value, double size, SolidColorBrush foreground, Windows.UI.Text.FontWeight? weight = null)
        => new() { Text = value, FontSize = size, Foreground = foreground, FontWeight = weight ?? Microsoft.UI.Text.FontWeights.Normal };

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Surface => SnapvereBrand.Surface;
    private static SolidColorBrush Outline => SnapvereBrand.Outline;
    private static SolidColorBrush Strong => SnapvereBrand.Strong;
    private static SolidColorBrush Muted => SnapvereBrand.Muted;
    private static SolidColorBrush Subtle => SnapvereBrand.Subtle;

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
