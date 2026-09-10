using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Snapvere.Capture;
using Snapvere.Capture.Geometry;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;
using Windows.Graphics;
using Windows.System;

namespace Snapvere.App;

/// <summary>
/// One monitor-local Window Capture picker surface. It is constructed entirely
/// in C# to avoid secondary XAML resource loading and uses the monitor's native
/// DPI to translate pointer coordinates and window bounds without drift.
/// </summary>
public sealed class WindowTargetOverlayWindow : Window
{
    private readonly DisplayDescriptor _display;
    private readonly CaptureFrame _frozenFrame;
    private readonly IReadOnlyList<WindowDescriptor> _windows;
    private readonly Action<WindowDescriptor?> _targetChanged;
    private readonly Action<WindowDescriptor> _targetSelected;
    private readonly Action _cancelled;

    private readonly Grid _root;
    private readonly Image _frozenImage;
    private readonly Canvas _chrome;
    private readonly Canvas _inputLayer;
    private readonly Rectangle _fullDim;
    private readonly Rectangle _topDim;
    private readonly Rectangle _leftDim;
    private readonly Rectangle _rightDim;
    private readonly Rectangle _bottomDim;
    private readonly Border _targetBorder;
    private readonly Border _targetLabel;
    private readonly TextBlock _targetText;
    private readonly Button _focusTarget;

    private WindowDescriptor? _target;
    private bool _closedByCoordinator;

    public WindowTargetOverlayWindow(
        DisplayDescriptor display,
        CaptureFrame frozenFrame,
        IReadOnlyList<WindowDescriptor> windows,
        Action<WindowDescriptor?> targetChanged,
        Action<WindowDescriptor> targetSelected,
        Action cancelled)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _frozenFrame = frozenFrame ?? throw new ArgumentNullException(nameof(frozenFrame));
        _windows = windows ?? throw new ArgumentNullException(nameof(windows));
        _targetChanged = targetChanged ?? throw new ArgumentNullException(nameof(targetChanged));
        _targetSelected = targetSelected ?? throw new ArgumentNullException(nameof(targetSelected));
        _cancelled = cancelled ?? throw new ArgumentNullException(nameof(cancelled));

        Title = "SNAPVERE — Window Capture";

        _frozenImage = new Image
        {
            Stretch = Stretch.Fill,
            IsHitTestVisible = false
        };
        _chrome = new Canvas { IsHitTestVisible = false };
        _inputLayer = new Canvas { Background = Brush(0x01, 0x00, 0x00, 0x00) };

        _fullDim = CreateDimRectangle();
        _topDim = CreateDimRectangle(Visibility.Collapsed);
        _leftDim = CreateDimRectangle(Visibility.Collapsed);
        _rightDim = CreateDimRectangle(Visibility.Collapsed);
        _bottomDim = CreateDimRectangle(Visibility.Collapsed);

        _targetBorder = new Border
        {
            BorderBrush = AccentBrush,
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(7),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };

        _targetText = new TextBlock
        {
            FontSize = 11.5,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Strong,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 460
        };
        _targetLabel = new Border
        {
            Padding = new Thickness(11, 7, 11, 7),
            Background = Brush(0xF4, 0x0C, 0x0E, 0x15),
            BorderBrush = Brush(0xA0, 0x8D, 0x79, 0xFF),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Child = _targetText,
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };

        _focusTarget = new Button
        {
            Width = 1,
            Height = 1,
            Opacity = 0,
            IsTabStop = true,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        AutomationProperties.SetName(_focusTarget, "Window capture keyboard input");

        _root = BuildRoot();
        Content = _root;
        ConfigureWindow();
        WireEvents();
    }

    public void Show() => Activate();

    public void SetTarget(WindowDescriptor? target)
    {
        _target = target;
        UpdateTargetVisual();
    }

    public void CloseSafely()
    {
        _closedByCoordinator = true;
        try
        {
            Close();
        }
        catch (InvalidOperationException)
        {
            // The native window may already be closing after selection/cancel.
        }
    }

    private Grid BuildRoot()
    {
        var root = new Grid
        {
            Background = Brush(0xFF, 0x00, 0x00, 0x00),
            RequestedTheme = ElementTheme.Dark
        };
        root.Children.Add(_frozenImage);

        _chrome.Children.Add(_fullDim);
        _chrome.Children.Add(_topDim);
        _chrome.Children.Add(_leftDim);
        _chrome.Children.Add(_rightDim);
        _chrome.Children.Add(_bottomDim);
        _chrome.Children.Add(_targetBorder);
        _chrome.Children.Add(_targetLabel);
        root.Children.Add(_chrome);
        root.Children.Add(_inputLayer);
        root.Children.Add(BuildHint());
        root.Children.Add(_focusTarget);
        return root;
    }

    private static Border BuildHint()
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 9,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(9),
            Background = Brush(0x5A, 0x67, 0x50, 0xD2),
            BorderBrush = Brush(0x70, 0xA5, 0x8E, 0xFF),
            BorderThickness = new Thickness(1),
            Child = new FontIcon
            {
                Glyph = "\uE7F4",
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 13,
                Foreground = Strong
            }
        });

        var copy = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(Text("Window Capture", 11, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        copy.Children.Add(Text("Point to a window · click to capture · Esc to cancel", 9.5, Muted));
        content.Children.Add(copy);

        return new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16),
            Padding = new Thickness(9, 8, 12, 8),
            Background = Brush(0xF2, 0x09, 0x0B, 0x11),
            BorderBrush = Brush(0x80, 0x45, 0x3D, 0x72),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            IsHitTestVisible = false,
            Child = content
        };
    }

    private void ConfigureWindow()
    {
        var bounds = _display.Bounds.Normalize();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        AppWindow.MoveAndResize(new RectInt32(bounds.X, bounds.Y, bounds.Width, bounds.Height));
    }

    private void WireEvents()
    {
        _root.Loaded += Root_Loaded;
        _root.SizeChanged += Root_SizeChanged;
        _root.KeyDown += Root_KeyDown;
        _inputLayer.PointerMoved += InputLayer_PointerMoved;
        _inputLayer.PointerPressed += InputLayer_PointerPressed;
        Closed += WindowTargetOverlayWindow_Closed;
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _frozenImage.Source = await CreateFrozenBitmapAsync(_frozenFrame);
            UpdateTargetVisual();
            _ = _focusTarget.Focus(FocusState.Programmatic);
        }
        catch
        {
            _cancelled();
        }
    }

    private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateTargetVisual();

    private void InputLayer_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(_inputLayer);
        var desktopPoint = ToDesktopPixel(pointer.Position.X, pointer.Position.Y);
        _targetChanged(WindowHitTesting.FindTopmostAtPoint(_windows, desktopPoint));
    }

    private void InputLayer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(_inputLayer);
        if (!pointer.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var desktopPoint = ToDesktopPixel(pointer.Position.X, pointer.Position.Y);
        var target = WindowHitTesting.FindTopmostAtPoint(_windows, desktopPoint);
        if (target is not null)
        {
            _targetSelected(target);
            e.Handled = true;
        }
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
        {
            return;
        }

        _cancelled();
        e.Handled = true;
    }

    private void WindowTargetOverlayWindow_Closed(object sender, WindowEventArgs args)
    {
        if (!_closedByCoordinator)
        {
            _cancelled();
        }
    }

    private void UpdateTargetVisual()
    {
        var totalWidth = Math.Max(0d, _chrome.ActualWidth);
        var totalHeight = Math.Max(0d, _chrome.ActualHeight);
        SetRectangle(_fullDim, 0, 0, totalWidth, totalHeight);

        var target = _target;
        if (target is null)
        {
            ShowNoTarget();
            return;
        }

        var displayBounds = _display.Bounds.Normalize();
        var targetBounds = target.Bounds.Normalize();
        var intersection = Intersect(displayBounds, targetBounds);
        if (intersection.IsEmpty)
        {
            ShowNoTarget();
            return;
        }

        _fullDim.Visibility = Visibility.Collapsed;
        _topDim.Visibility = Visibility.Visible;
        _leftDim.Visibility = Visibility.Visible;
        _rightDim.Visibility = Visibility.Visible;
        _bottomDim.Visibility = Visibility.Visible;
        _targetBorder.Visibility = Visibility.Visible;

        var x = DpiCoordinateTransformer.PhysicalToLogical(
            intersection.X - displayBounds.X,
            _display.DpiX);
        var y = DpiCoordinateTransformer.PhysicalToLogical(
            intersection.Y - displayBounds.Y,
            _display.DpiY);
        var width = DpiCoordinateTransformer.PhysicalToLogical(intersection.Width, _display.DpiX);
        var height = DpiCoordinateTransformer.PhysicalToLogical(intersection.Height, _display.DpiY);
        var right = Math.Min(totalWidth, x + width);
        var bottom = Math.Min(totalHeight, y + height);

        x = Math.Clamp(x, 0d, totalWidth);
        y = Math.Clamp(y, 0d, totalHeight);
        width = Math.Max(0d, right - x);
        height = Math.Max(0d, bottom - y);

        SetRectangle(_topDim, 0, 0, totalWidth, y);
        SetRectangle(_leftDim, 0, y, x, height);
        SetRectangle(_rightDim, right, y, Math.Max(0d, totalWidth - right), height);
        SetRectangle(_bottomDim, 0, bottom, totalWidth, Math.Max(0d, totalHeight - bottom));

        Canvas.SetLeft(_targetBorder, x);
        Canvas.SetTop(_targetBorder, y);
        _targetBorder.Width = width;
        _targetBorder.Height = height;

        var targetStartsOnThisDisplay =
            targetBounds.Left >= displayBounds.Left && targetBounds.Left < displayBounds.Right &&
            targetBounds.Top >= displayBounds.Top && targetBounds.Top < displayBounds.Bottom;
        if (targetStartsOnThisDisplay)
        {
            _targetText.Text = $"{target.Title}   •   {targetBounds.Width} × {targetBounds.Height}";
            _targetLabel.Visibility = Visibility.Visible;
            _targetLabel.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            var labelWidth = _targetLabel.DesiredSize.Width;
            var labelHeight = _targetLabel.DesiredSize.Height;
            Canvas.SetLeft(_targetLabel, Math.Clamp(x, 0d, Math.Max(0d, totalWidth - labelWidth)));
            Canvas.SetTop(
                _targetLabel,
                y >= labelHeight + 10d
                    ? y - labelHeight - 10d
                    : Math.Min(Math.Max(0d, totalHeight - labelHeight), bottom + 10d));
        }
        else
        {
            _targetLabel.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowNoTarget()
    {
        _fullDim.Visibility = Visibility.Visible;
        _topDim.Visibility = Visibility.Collapsed;
        _leftDim.Visibility = Visibility.Collapsed;
        _rightDim.Visibility = Visibility.Collapsed;
        _bottomDim.Visibility = Visibility.Collapsed;
        _targetBorder.Visibility = Visibility.Collapsed;
        _targetLabel.Visibility = Visibility.Collapsed;
    }

    private PixelPoint ToDesktopPixel(double xDip, double yDip)
    {
        var bounds = _display.Bounds.Normalize();
        var local = DpiCoordinateTransformer.LogicalToPhysical(
            xDip,
            yDip,
            _display.DpiX,
            _display.DpiY);
        var maxX = Math.Max(bounds.Left, bounds.Right - 1);
        var maxY = Math.Max(bounds.Top, bounds.Bottom - 1);
        return new PixelPoint(
            Math.Clamp(checked(bounds.Left + local.X), bounds.Left, maxX),
            Math.Clamp(checked(bounds.Top + local.Y), bounds.Top, maxY));
    }

    private static PixelRect Intersect(PixelRect first, PixelRect second)
    {
        var a = first.Normalize();
        var b = second.Normalize();
        var left = Math.Max(a.Left, b.Left);
        var top = Math.Max(a.Top, b.Top);
        var right = Math.Min(a.Right, b.Right);
        var bottom = Math.Min(a.Bottom, b.Bottom);
        return right <= left || bottom <= top
            ? default
            : new PixelRect(left, top, right - left, bottom - top);
    }

    private static Rectangle CreateDimRectangle(Visibility visibility = Visibility.Visible)
        => new()
        {
            Fill = Brush(0xB2, 0x03, 0x04, 0x08),
            IsHitTestVisible = false,
            Visibility = visibility
        };

    private static void SetRectangle(
        Rectangle rectangle,
        double x,
        double y,
        double width,
        double height)
    {
        Canvas.SetLeft(rectangle, x);
        Canvas.SetTop(rectangle, y);
        rectangle.Width = Math.Max(0d, width);
        rectangle.Height = Math.Max(0d, height);
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

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Strong => Brush(0xFF, 0xF7, 0xF5, 0xFF);
    private static SolidColorBrush Muted => Brush(0xFF, 0xB5, 0xB0, 0xC3);
    private static SolidColorBrush AccentBrush => Brush(0xFF, 0x8D, 0x79, 0xFF);

    private static async Task<WriteableBitmap> CreateFrozenBitmapAsync(CaptureFrame frame)
    {
        frame.Validate();
        var rowLength = checked(frame.Size.Width * 4);
        var packedPixels = new byte[checked(rowLength * frame.Size.Height)];
        var source = frame.Bgra8Pixels.Span;

        for (var y = 0; y < frame.Size.Height; y++)
        {
            source.Slice(checked(y * frame.Stride), rowLength)
                .CopyTo(packedPixels.AsSpan(checked(y * rowLength), rowLength));
        }

        var bitmap = new WriteableBitmap(frame.Size.Width, frame.Size.Height);
        using var pixelStream = bitmap.PixelBuffer.AsStream();
        pixelStream.Position = 0;
        await pixelStream.WriteAsync(packedPixels);
        bitmap.Invalidate();
        return bitmap;
    }
}
