using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Capture.Geometry;
using Snapvere.Capture.Region;
using Snapvere.Domain.Capture;
using Windows.Graphics;
using Windows.System;

namespace Snapvere.App;

public sealed record RegionCaptureOutcome(
    bool IsCancelled,
    CaptureSaveResult? SaveResult);

public sealed partial class RegionCaptureWindow : Window
{
    private const double HandleRadius = 5d;

    private readonly RegionCaptureWorkflow _workflow;
    private readonly RegionCaptureSession _session;
    private readonly TaskCompletionSource<RegionCaptureOutcome> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private PixelRect _selection;
    private PixelRect _dragStartSelection;
    private PixelPoint _dragAnchor;
    private SelectionHandle _activeHandle;
    private bool _isCreatingSelection;
    private bool _pointerCaptured;
    private bool _shiftDown;
    private bool _saving;

    public RegionCaptureWindow(
        RegionCaptureWorkflow workflow,
        RegionCaptureSession session)
    {
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _session = session ?? throw new ArgumentNullException(nameof(session));

        InitializeComponent();
        ConfigureOverlayWindow();

        OverlayRoot.Loaded += OverlayRoot_Loaded;
        OverlayRoot.SizeChanged += OverlayRoot_SizeChanged;
        Closed += RegionCaptureWindow_Closed;
    }

    public Task<RegionCaptureOutcome> ShowAsync()
    {
        Activate();
        return _completion.Task;
    }

    private void ConfigureOverlayWindow()
    {
        var bounds = _session.Display.Bounds.Normalize();

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        AppWindow.MoveAndResize(new RectInt32(
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height));
    }

    private async void OverlayRoot_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            FrozenImage.Source = await CreateFrozenBitmapAsync(_session.FrozenFrame);
            UpdateSelectionVisuals();
            _ = KeyboardFocusTarget.Focus(FocusState.Programmatic);
        }
        catch (Exception exception)
        {
            ShowStatus(GetUserFacingError(exception), isBusy: false);
        }
    }

    private void OverlayRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateSelectionVisuals();

    private void OverlayCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_saving)
        {
            return;
        }

        var pointerPoint = e.GetCurrentPoint(OverlayCanvas);
        if (!pointerPoint.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var point = ToDesktopPixel(pointerPoint.Position.X, pointerPoint.Position.Y);
        if (!_selection.IsEmpty && Contains(_selection, point))
        {
            BeginManipulation(SelectionHandle.Body, point, e);
        }
        else
        {
            _selection = default;
            _isCreatingSelection = true;
            _activeHandle = SelectionHandle.None;
            _dragAnchor = point;
            _dragStartSelection = default;
            _pointerCaptured = OverlayCanvas.CapturePointer(e.Pointer);
            UpdateSelectionVisuals();
        }

        e.Handled = true;
    }

    private void Handle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_saving || _selection.IsEmpty || sender is not FrameworkElement element)
        {
            return;
        }

        if (!Enum.TryParse<SelectionHandle>(element.Tag?.ToString(), out var handle) ||
            handle is SelectionHandle.None or SelectionHandle.Body)
        {
            return;
        }

        var pointerPoint = e.GetCurrentPoint(OverlayCanvas);
        var point = ToDesktopPixel(pointerPoint.Position.X, pointerPoint.Position.Y);
        BeginManipulation(handle, point, e);
        e.Handled = true;
    }

    private void BeginManipulation(
        SelectionHandle handle,
        PixelPoint point,
        PointerRoutedEventArgs e)
    {
        _isCreatingSelection = false;
        _activeHandle = handle;
        _dragAnchor = point;
        _dragStartSelection = _selection;
        _pointerCaptured = OverlayCanvas.CapturePointer(e.Pointer);
    }

    private void OverlayCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_saving || !_pointerCaptured)
        {
            return;
        }

        var pointerPoint = e.GetCurrentPoint(OverlayCanvas);
        var current = ToDesktopPixel(pointerPoint.Position.X, pointerPoint.Position.Y);
        var bounds = _session.Display.Bounds.Normalize();

        if (_isCreatingSelection)
        {
            _selection = RegionSelectionGeometry.CreateFromDrag(_dragAnchor, current, bounds);
        }
        else
        {
            var deltaX = checked(current.X - _dragAnchor.X);
            var deltaY = checked(current.Y - _dragAnchor.Y);

            _selection = _activeHandle == SelectionHandle.Body
                ? RegionSelectionGeometry.Move(_dragStartSelection, deltaX, deltaY, bounds)
                : RegionSelectionGeometry.Resize(
                    _dragStartSelection,
                    _activeHandle,
                    deltaX,
                    deltaY,
                    bounds,
                    minimumWidth: 1,
                    minimumHeight: 1);
        }

        UpdateSelectionVisuals();
        e.Handled = true;
    }

    private void OverlayCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        EndPointerManipulation(e);
        e.Handled = true;
    }

    private void OverlayCanvas_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _pointerCaptured = false;
        _isCreatingSelection = false;
        _activeHandle = SelectionHandle.None;
    }

    private void EndPointerManipulation(PointerRoutedEventArgs e)
    {
        if (_pointerCaptured)
        {
            OverlayCanvas.ReleasePointerCapture(e.Pointer);
        }

        _pointerCaptured = false;
        _isCreatingSelection = false;
        _activeHandle = SelectionHandle.None;
        _ = KeyboardFocusTarget.Focus(FocusState.Programmatic);
    }

    private async void OverlayCanvas_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (!_selection.IsEmpty && !_saving)
        {
            await CommitSelectionAsync();
            e.Handled = true;
        }
    }

    private async void OverlayRoot_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Shift)
        {
            _shiftDown = true;
            return;
        }

        if (_saving)
        {
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Escape:
                CancelCapture();
                e.Handled = true;
                break;

            case VirtualKey.Enter when !_selection.IsEmpty:
                await CommitSelectionAsync();
                e.Handled = true;
                break;

            case VirtualKey.Delete:
                _selection = default;
                UpdateSelectionVisuals();
                e.Handled = true;
                break;

            case VirtualKey.Left:
                NudgeSelection(-GetKeyboardStep(), 0);
                e.Handled = true;
                break;

            case VirtualKey.Right:
                NudgeSelection(GetKeyboardStep(), 0);
                e.Handled = true;
                break;

            case VirtualKey.Up:
                NudgeSelection(0, -GetKeyboardStep());
                e.Handled = true;
                break;

            case VirtualKey.Down:
                NudgeSelection(0, GetKeyboardStep());
                e.Handled = true;
                break;
        }
    }

    private void OverlayRoot_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Shift)
        {
            _shiftDown = false;
        }
    }

    private void NudgeSelection(int deltaX, int deltaY)
    {
        if (_selection.IsEmpty)
        {
            return;
        }

        _selection = RegionSelectionGeometry.Move(
            _selection,
            deltaX,
            deltaY,
            _session.Display.Bounds);

        UpdateSelectionVisuals();
    }

    private int GetKeyboardStep() => _shiftDown ? 10 : 1;

    private async Task CommitSelectionAsync()
    {
        if (_saving || _selection.IsEmpty)
        {
            return;
        }

        _saving = true;
        ShowStatus("Saving selected region…", isBusy: true);

        try
        {
            var result = await _workflow.SaveSelectionAsync(_session, _selection);
            _completion.TrySetResult(new RegionCaptureOutcome(false, result));
            Close();
        }
        catch (Exception exception)
        {
            _saving = false;
            ShowStatus(GetUserFacingError(exception), isBusy: false);
            _ = KeyboardFocusTarget.Focus(FocusState.Programmatic);
        }
    }

    private void CancelCapture()
    {
        if (_saving)
        {
            return;
        }

        _completion.TrySetResult(new RegionCaptureOutcome(true, null));
        Close();
    }

    private void RegionCaptureWindow_Closed(object sender, WindowEventArgs args)
        => _completion.TrySetResult(new RegionCaptureOutcome(true, null));

    private void ShowStatus(string message, bool isBusy)
    {
        OverlayStatusText.Text = message;
        SaveProgress.IsActive = isBusy;
        OverlayStatus.Visibility = Visibility.Visible;
    }

    private void UpdateSelectionVisuals()
    {
        var totalWidth = Math.Max(0d, OverlayCanvas.ActualWidth);
        var totalHeight = Math.Max(0d, OverlayCanvas.ActualHeight);
        SetRectangle(FullDim, 0, 0, totalWidth, totalHeight);

        if (_selection.IsEmpty)
        {
            FullDim.Visibility = Visibility.Visible;
            SetSelectionChromeVisibility(Visibility.Collapsed);
            return;
        }

        FullDim.Visibility = Visibility.Collapsed;
        SetSelectionChromeVisibility(Visibility.Visible);

        var displayBounds = _session.Display.Bounds.Normalize();
        var selection = RegionSelectionGeometry.Clamp(_selection, displayBounds);
        var x = DpiCoordinateTransformer.PhysicalToLogical(
            checked(selection.X - displayBounds.X),
            _session.Display.DpiX);
        var y = DpiCoordinateTransformer.PhysicalToLogical(
            checked(selection.Y - displayBounds.Y),
            _session.Display.DpiY);
        var width = DpiCoordinateTransformer.PhysicalToLogical(selection.Width, _session.Display.DpiX);
        var height = DpiCoordinateTransformer.PhysicalToLogical(selection.Height, _session.Display.DpiY);
        var right = Math.Min(totalWidth, x + width);
        var bottom = Math.Min(totalHeight, y + height);

        x = Math.Clamp(x, 0d, totalWidth);
        y = Math.Clamp(y, 0d, totalHeight);
        width = Math.Max(0d, right - x);
        height = Math.Max(0d, bottom - y);

        SetRectangle(TopDim, 0, 0, totalWidth, y);
        SetRectangle(LeftDim, 0, y, x, height);
        SetRectangle(RightDim, right, y, Math.Max(0d, totalWidth - right), height);
        SetRectangle(BottomDim, 0, bottom, totalWidth, Math.Max(0d, totalHeight - bottom));

        Canvas.SetLeft(SelectionBorder, x);
        Canvas.SetTop(SelectionBorder, y);
        SelectionBorder.Width = width;
        SelectionBorder.Height = height;

        SelectionSizeText.Text = $"{selection.Width} × {selection.Height}";
        SelectionBadge.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var badgeWidth = SelectionBadge.DesiredSize.Width;
        var badgeHeight = SelectionBadge.DesiredSize.Height;
        var badgeX = Math.Clamp(x, 0d, Math.Max(0d, totalWidth - badgeWidth));
        var badgeY = y >= badgeHeight + 8d
            ? y - badgeHeight - 8d
            : Math.Min(totalHeight - badgeHeight, bottom + 8d);
        Canvas.SetLeft(SelectionBadge, badgeX);
        Canvas.SetTop(SelectionBadge, Math.Max(0d, badgeY));

        PlaceHandle(TopLeftHandle, x, y);
        PlaceHandle(TopHandle, x + (width / 2d), y);
        PlaceHandle(TopRightHandle, right, y);
        PlaceHandle(RightHandle, right, y + (height / 2d));
        PlaceHandle(BottomRightHandle, right, bottom);
        PlaceHandle(BottomHandle, x + (width / 2d), bottom);
        PlaceHandle(BottomLeftHandle, x, bottom);
        PlaceHandle(LeftHandle, x, y + (height / 2d));
    }

    private void SetSelectionChromeVisibility(Visibility visibility)
    {
        TopDim.Visibility = visibility;
        LeftDim.Visibility = visibility;
        RightDim.Visibility = visibility;
        BottomDim.Visibility = visibility;
        SelectionBorder.Visibility = visibility;
        SelectionBadge.Visibility = visibility;
        TopLeftHandle.Visibility = visibility;
        TopHandle.Visibility = visibility;
        TopRightHandle.Visibility = visibility;
        RightHandle.Visibility = visibility;
        BottomRightHandle.Visibility = visibility;
        BottomHandle.Visibility = visibility;
        BottomLeftHandle.Visibility = visibility;
        LeftHandle.Visibility = visibility;
    }

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

    private static void PlaceHandle(Ellipse handle, double centerX, double centerY)
    {
        Canvas.SetLeft(handle, centerX - HandleRadius);
        Canvas.SetTop(handle, centerY - HandleRadius);
    }

    private PixelPoint ToDesktopPixel(double xDip, double yDip)
    {
        var bounds = _session.Display.Bounds.Normalize();
        var local = DpiCoordinateTransformer.LogicalToPhysical(
            xDip,
            yDip,
            _session.Display.DpiX,
            _session.Display.DpiY);

        var x = checked(bounds.X + local.X);
        var y = checked(bounds.Y + local.Y);

        return new PixelPoint(
            Math.Clamp(x, bounds.Left, bounds.Right),
            Math.Clamp(y, bounds.Top, bounds.Bottom));
    }

    private static bool Contains(PixelRect rectangle, PixelPoint point)
    {
        var normalized = rectangle.Normalize();
        return point.X >= normalized.Left && point.X < normalized.Right &&
               point.Y >= normalized.Top && point.Y < normalized.Bottom;
    }

    private static async Task<WriteableBitmap> CreateFrozenBitmapAsync(CaptureFrame frame)
    {
        frame.Validate();

        var rowLength = checked(frame.Size.Width * 4);
        var packedPixels = new byte[checked(rowLength * frame.Size.Height)];
        var source = frame.Bgra8Pixels.Span;

        for (var y = 0; y < frame.Size.Height; y++)
        {
            source
                .Slice(checked(y * frame.Stride), rowLength)
                .CopyTo(packedPixels.AsSpan(checked(y * rowLength), rowLength));
        }

        var bitmap = new WriteableBitmap(frame.Size.Width, frame.Size.Height);
        using var pixelStream = bitmap.PixelBuffer.AsStream();
        pixelStream.Position = 0;
        await pixelStream.WriteAsync(packedPixels);
        bitmap.Invalidate();
        return bitmap;
    }

    private static string GetUserFacingError(Exception exception)
        => exception switch
        {
            UnauthorizedAccessException => "Windows denied access to the capture folder.",
            IOException => "The selected region was captured, but the PNG file could not be saved.",
            ArgumentException => "The selected region is no longer valid. Select the region again.",
            InvalidOperationException => exception.Message,
            _ => "SNAPVERE could not complete the region capture. Press Esc and try again."
        };
}
