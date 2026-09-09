using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Capture.Geometry;
using Snapvere.Capture.Region;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage.Streams;
using Windows.System;

namespace Snapvere.App;

public sealed record RegionCaptureOutcome(
    bool IsCancelled,
    CaptureSaveResult? SaveResult,
    bool CopiedToClipboard = false);

public sealed partial class RegionCaptureWindow : Window
{
    private const double HandleRadius = 5d;

    private readonly RegionCaptureWorkflow _workflow;
    private readonly PngCaptureEncoder _pngEncoder;
    private readonly RegionCaptureSession _session;
    private readonly TaskCompletionSource<RegionCaptureOutcome> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<CaptureAnnotation> _annotations = [];
    private readonly List<CaptureAnnotationPoint> _draftPoints = [];

    private PixelRect _selection;
    private PixelRect _dragStartSelection;
    private PixelPoint _dragAnchor;
    private SelectionHandle _activeHandle;
    private CaptureAnnotationKind? _activeAnnotationTool;
    private CaptureAnnotationColor _annotationColor = CaptureAnnotationColor.Coral;
    private bool _isCreatingSelection;
    private bool _pointerCaptured;
    private bool _annotationInProgress;
    private bool _shiftDown;
    private bool _saving;

    public RegionCaptureWindow(
        RegionCaptureWorkflow workflow,
        PngCaptureEncoder pngEncoder,
        RegionCaptureSession session)
    {
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _pngEncoder = pngEncoder ?? throw new ArgumentNullException(nameof(pngEncoder));
        _session = session ?? throw new ArgumentNullException(nameof(session));

        InitializeComponent();
        ConfigureOverlayWindow();
        UpdateToolButtonStates();

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
        if (_activeAnnotationTool is not null && !_selection.IsEmpty && Contains(_selection, point))
        {
            BeginAnnotation(point, e);
        }
        else if (!_selection.IsEmpty && Contains(_selection, point))
        {
            BeginManipulation(SelectionHandle.Body, point, e);
        }
        else
        {
            _selection = default;
            _annotations.Clear();
            _draftPoints.Clear();
            _activeAnnotationTool = null;
            _isCreatingSelection = true;
            _activeHandle = SelectionHandle.None;
            _dragAnchor = point;
            _dragStartSelection = default;
            _pointerCaptured = OverlayCanvas.CapturePointer(e.Pointer);
            UpdateToolButtonStates();
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

        _activeAnnotationTool = null;
        UpdateToolButtonStates();
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
        _annotationInProgress = false;
        _isCreatingSelection = false;
        _activeHandle = handle;
        _dragAnchor = point;
        _dragStartSelection = _selection;
        _pointerCaptured = OverlayCanvas.CapturePointer(e.Pointer);
    }

    private void BeginAnnotation(PixelPoint desktopPoint, PointerRoutedEventArgs e)
    {
        if (_activeAnnotationTool is null || _selection.IsEmpty)
        {
            return;
        }

        _annotationInProgress = true;
        _isCreatingSelection = false;
        _activeHandle = SelectionHandle.None;
        _draftPoints.Clear();
        _draftPoints.Add(ToSelectionLocal(desktopPoint));
        _pointerCaptured = OverlayCanvas.CapturePointer(e.Pointer);
        RenderAnnotations();
    }

    private void OverlayCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_saving || !_pointerCaptured)
        {
            return;
        }

        var pointerPoint = e.GetCurrentPoint(OverlayCanvas);
        var current = ToDesktopPixel(pointerPoint.Position.X, pointerPoint.Position.Y);

        if (_annotationInProgress)
        {
            UpdateDraftAnnotation(current);
            RenderAnnotations();
            e.Handled = true;
            return;
        }

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

    private void UpdateDraftAnnotation(PixelPoint desktopPoint)
    {
        if (_activeAnnotationTool is null || _draftPoints.Count == 0)
        {
            return;
        }

        var localPoint = ToSelectionLocal(desktopPoint);
        if (_activeAnnotationTool is CaptureAnnotationKind.Pen or CaptureAnnotationKind.Highlight)
        {
            var previous = _draftPoints[^1];
            if (previous != localPoint)
            {
                _draftPoints.Add(localPoint);
            }
            return;
        }

        if (_draftPoints.Count == 1)
        {
            _draftPoints.Add(localPoint);
        }
        else
        {
            _draftPoints[^1] = localPoint;
        }
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
        _annotationInProgress = false;
        _draftPoints.Clear();
        RenderAnnotations();
    }

    private void EndPointerManipulation(PointerRoutedEventArgs e)
    {
        if (_annotationInProgress)
        {
            FinalizeDraftAnnotation();
        }

        if (_pointerCaptured)
        {
            OverlayCanvas.ReleasePointerCapture(e.Pointer);
        }

        _pointerCaptured = false;
        _isCreatingSelection = false;
        _activeHandle = SelectionHandle.None;
        _annotationInProgress = false;
        _draftPoints.Clear();
        UpdateSelectionVisuals();
        _ = KeyboardFocusTarget.Focus(FocusState.Programmatic);
    }

    private void FinalizeDraftAnnotation()
    {
        if (_activeAnnotationTool is null || _draftPoints.Count == 0)
        {
            return;
        }

        if (_activeAnnotationTool is not (CaptureAnnotationKind.Pen or CaptureAnnotationKind.Highlight) &&
            _draftPoints.Count == 1)
        {
            _draftPoints.Add(_draftPoints[0]);
        }

        _annotations.Add(new CaptureAnnotation(
            _activeAnnotationTool.Value,
            _draftPoints.ToArray(),
            _annotationColor,
            GetAnnotationThickness(_activeAnnotationTool.Value)));
    }

    private async void OverlayCanvas_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (_activeAnnotationTool is null && !_selection.IsEmpty && !_saving)
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
                if (_activeAnnotationTool is not null)
                {
                    _activeAnnotationTool = null;
                    UpdateToolButtonStates();
                    e.Handled = true;
                    break;
                }

                CancelCapture();
                e.Handled = true;
                break;

            case VirtualKey.Enter when !_selection.IsEmpty:
                await CommitSelectionAsync();
                e.Handled = true;
                break;

            case VirtualKey.Delete:
                _selection = default;
                _annotations.Clear();
                _activeAnnotationTool = null;
                UpdateToolButtonStates();
                UpdateSelectionVisuals();
                e.Handled = true;
                break;

            case VirtualKey.Z when IsControlDown():
                UndoLastAnnotation();
                e.Handled = true;
                break;

            case VirtualKey.C when IsControlDown() && !_selection.IsEmpty:
                await CopySelectionAsync();
                e.Handled = true;
                break;

            case VirtualKey.Left when _activeAnnotationTool is null:
                NudgeSelection(-GetKeyboardStep(), 0);
                e.Handled = true;
                break;

            case VirtualKey.Right when _activeAnnotationTool is null:
                NudgeSelection(GetKeyboardStep(), 0);
                e.Handled = true;
                break;

            case VirtualKey.Up when _activeAnnotationTool is null:
                NudgeSelection(0, -GetKeyboardStep());
                e.Handled = true;
                break;

            case VirtualKey.Down when _activeAnnotationTool is null:
                NudgeSelection(0, GetKeyboardStep());
                e.Handled = true;
                break;
        }
    }

    private static bool IsControlDown()
    {
        var state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        return (state & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
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

    private void AnnotationToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (string.Equals(element.Tag?.ToString(), "None", StringComparison.Ordinal))
        {
            _activeAnnotationTool = null;
        }
        else if (Enum.TryParse<CaptureAnnotationKind>(element.Tag?.ToString(), out var tool))
        {
            _activeAnnotationTool = tool;
        }

        UpdateToolButtonStates();
        _ = KeyboardFocusTarget.Focus(FocusState.Programmatic);
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        _annotationColor = element.Tag?.ToString() switch
        {
            "Amber" => CaptureAnnotationColor.Amber,
            "Mint" => CaptureAnnotationColor.Mint,
            "Indigo" => CaptureAnnotationColor.Indigo,
            _ => CaptureAnnotationColor.Coral
        };
        _ = KeyboardFocusTarget.Focus(FocusState.Programmatic);
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        UndoLastAnnotation();
        _ = KeyboardFocusTarget.Focus(FocusState.Programmatic);
    }

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
        => await CopySelectionAsync();

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
        => await CommitSelectionAsync();

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => CancelCapture();

    private void UndoLastAnnotation()
    {
        if (_annotations.Count == 0)
        {
            return;
        }

        _annotations.RemoveAt(_annotations.Count - 1);
        RenderAnnotations();
        UpdateToolButtonStates();
    }

    private void UpdateToolButtonStates()
    {
        var activeBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x5D, 0x52, 0xD9));
        var inactiveBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x21, 0x25, 0x2E));

        MoveToolButton.Background = _activeAnnotationTool is null ? activeBrush : inactiveBrush;
        PenToolButton.Background = _activeAnnotationTool == CaptureAnnotationKind.Pen ? activeBrush : inactiveBrush;
        LineToolButton.Background = _activeAnnotationTool == CaptureAnnotationKind.Line ? activeBrush : inactiveBrush;
        ArrowToolButton.Background = _activeAnnotationTool == CaptureAnnotationKind.Arrow ? activeBrush : inactiveBrush;
        RectangleToolButton.Background = _activeAnnotationTool == CaptureAnnotationKind.Rectangle ? activeBrush : inactiveBrush;
        HighlightToolButton.Background = _activeAnnotationTool == CaptureAnnotationKind.Highlight ? activeBrush : inactiveBrush;
        UndoButton.IsEnabled = _annotations.Count > 0;
    }

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
            var result = await _workflow.SaveSelectionAsync(_session, _selection, _annotations);
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

    private async Task CopySelectionAsync()
    {
        if (_saving || _selection.IsEmpty)
        {
            return;
        }

        _saving = true;
        ShowStatus("Copying selection to clipboard…", isBusy: true);

        try
        {
            var frame = _workflow.CreateSelectionFrame(_session, _selection, _annotations);
            using var png = new MemoryStream();
            await _pngEncoder.EncodeAsync(frame, png);

            using var randomAccessStream = new InMemoryRandomAccessStream();
            var pngBytes = png.ToArray();
            await randomAccessStream.WriteAsync(pngBytes.AsBuffer());
            randomAccessStream.Seek(0);

            var package = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            package.SetBitmap(RandomAccessStreamReference.CreateFromStream(randomAccessStream));
            Clipboard.SetContent(package);
            Clipboard.Flush();

            _completion.TrySetResult(new RegionCaptureOutcome(false, null, CopiedToClipboard: true));
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
            ToolPalette.Visibility = Visibility.Collapsed;
            ActionPalette.Visibility = Visibility.Collapsed;
            CaptureHint.Visibility = Visibility.Visible;
            AnnotationLayer.Children.Clear();
            AnnotationLayer.Clip = null;
            return;
        }

        FullDim.Visibility = Visibility.Collapsed;
        SetSelectionChromeVisibility(Visibility.Visible);
        ToolPalette.Visibility = Visibility.Visible;
        ActionPalette.Visibility = Visibility.Visible;
        CaptureHint.Visibility = Visibility.Collapsed;

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

        PositionFloatingPalettes(x, y, right, bottom, totalWidth, totalHeight);
        RenderAnnotations();
        UpdateToolButtonStates();
    }

    private void PositionFloatingPalettes(
        double x,
        double y,
        double right,
        double bottom,
        double totalWidth,
        double totalHeight)
    {
        ToolPalette.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        ActionPalette.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

        var toolWidth = ToolPalette.DesiredSize.Width;
        var toolHeight = ToolPalette.DesiredSize.Height;
        var actionWidth = ActionPalette.DesiredSize.Width;
        var actionHeight = ActionPalette.DesiredSize.Height;

        var toolX = right + 8d + toolWidth <= totalWidth
            ? right + 8d
            : x >= toolWidth + 8d
                ? x - toolWidth - 8d
                : Math.Clamp(right - toolWidth - 8d, 0d, Math.Max(0d, totalWidth - toolWidth));
        var toolY = Math.Clamp(y, 0d, Math.Max(0d, totalHeight - toolHeight));

        var actionX = Math.Clamp(right - actionWidth, 0d, Math.Max(0d, totalWidth - actionWidth));
        var actionY = bottom + 8d + actionHeight <= totalHeight
            ? bottom + 8d
            : Math.Max(0d, y - actionHeight - 8d);

        Canvas.SetLeft(ToolPalette, toolX);
        Canvas.SetTop(ToolPalette, toolY);
        Canvas.SetLeft(ActionPalette, actionX);
        Canvas.SetTop(ActionPalette, actionY);
    }

    private void RenderAnnotations()
    {
        AnnotationLayer.Children.Clear();
        if (_selection.IsEmpty)
        {
            AnnotationLayer.Clip = null;
            return;
        }

        var displayBounds = _session.Display.Bounds.Normalize();
        var selection = RegionSelectionGeometry.Clamp(_selection, displayBounds);
        var selectionX = DpiCoordinateTransformer.PhysicalToLogical(
            checked(selection.X - displayBounds.X),
            _session.Display.DpiX);
        var selectionY = DpiCoordinateTransformer.PhysicalToLogical(
            checked(selection.Y - displayBounds.Y),
            _session.Display.DpiY);
        var selectionWidth = DpiCoordinateTransformer.PhysicalToLogical(selection.Width, _session.Display.DpiX);
        var selectionHeight = DpiCoordinateTransformer.PhysicalToLogical(selection.Height, _session.Display.DpiY);

        AnnotationLayer.Clip = new RectangleGeometry
        {
            Rect = new Windows.Foundation.Rect(selectionX, selectionY, selectionWidth, selectionHeight)
        };

        foreach (var annotation in _annotations)
        {
            AddAnnotationVisual(annotation, selectionX, selectionY);
        }

        if (_annotationInProgress && _activeAnnotationTool is not null && _draftPoints.Count > 0)
        {
            var points = _draftPoints.ToArray();
            if (_activeAnnotationTool is not (CaptureAnnotationKind.Pen or CaptureAnnotationKind.Highlight) && points.Length == 1)
            {
                points = [points[0], points[0]];
            }

            AddAnnotationVisual(
                new CaptureAnnotation(
                    _activeAnnotationTool.Value,
                    points,
                    _annotationColor,
                    GetAnnotationThickness(_activeAnnotationTool.Value)),
                selectionX,
                selectionY);
        }
    }

    private void AddAnnotationVisual(CaptureAnnotation annotation, double selectionX, double selectionY)
    {
        var brush = AnnotationBrush(annotation);
        var thickness = Math.Max(
            1d,
            DpiCoordinateTransformer.PhysicalToLogical(annotation.Thickness, _session.Display.DpiX));

        switch (annotation.Kind)
        {
            case CaptureAnnotationKind.Pen:
            case CaptureAnnotationKind.Highlight:
            {
                var polyline = new Polyline
                {
                    Stroke = brush,
                    StrokeThickness = thickness
                };
                foreach (var point in annotation.Points)
                {
                    polyline.Points.Add(ToOverlayPoint(point, selectionX, selectionY));
                }
                AnnotationLayer.Children.Add(polyline);
                break;
            }

            case CaptureAnnotationKind.Line:
                AddLineVisual(annotation.Points[0], annotation.Points[^1], selectionX, selectionY, brush, thickness);
                break;

            case CaptureAnnotationKind.Rectangle:
                AddRectangleVisual(annotation, selectionX, selectionY, brush, thickness);
                break;

            case CaptureAnnotationKind.Arrow:
                AddArrowVisual(annotation, selectionX, selectionY, brush, thickness);
                break;
        }
    }

    private void AddLineVisual(
        CaptureAnnotationPoint start,
        CaptureAnnotationPoint end,
        double selectionX,
        double selectionY,
        SolidColorBrush brush,
        double thickness)
    {
        var first = ToOverlayPoint(start, selectionX, selectionY);
        var second = ToOverlayPoint(end, selectionX, selectionY);
        AnnotationLayer.Children.Add(new Line
        {
            X1 = first.X,
            Y1 = first.Y,
            X2 = second.X,
            Y2 = second.Y,
            Stroke = brush,
            StrokeThickness = thickness
        });
    }

    private void AddRectangleVisual(
        CaptureAnnotation annotation,
        double selectionX,
        double selectionY,
        SolidColorBrush brush,
        double thickness)
    {
        var first = ToOverlayPoint(annotation.Points[0], selectionX, selectionY);
        var second = ToOverlayPoint(annotation.Points[^1], selectionX, selectionY);
        var left = Math.Min(first.X, second.X);
        var top = Math.Min(first.Y, second.Y);
        var rectangle = new Rectangle
        {
            Width = Math.Abs(second.X - first.X),
            Height = Math.Abs(second.Y - first.Y),
            Stroke = brush,
            StrokeThickness = thickness
        };
        Canvas.SetLeft(rectangle, left);
        Canvas.SetTop(rectangle, top);
        AnnotationLayer.Children.Add(rectangle);
    }

    private void AddArrowVisual(
        CaptureAnnotation annotation,
        double selectionX,
        double selectionY,
        SolidColorBrush brush,
        double thickness)
    {
        var start = ToOverlayPoint(annotation.Points[0], selectionX, selectionY);
        var end = ToOverlayPoint(annotation.Points[^1], selectionX, selectionY);
        AnnotationLayer.Children.Add(new Line
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = brush,
            StrokeThickness = thickness
        });

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1d)
        {
            return;
        }

        var headLength = Math.Clamp(Math.Max(10d, thickness * 4d), 10d, Math.Max(10d, length * 0.45d));
        var angle = Math.Atan2(dy, dx);
        const double wingAngle = 0.58d;

        for (var direction = -1; direction <= 1; direction += 2)
        {
            var wing = angle + direction * wingAngle;
            AnnotationLayer.Children.Add(new Line
            {
                X1 = end.X,
                Y1 = end.Y,
                X2 = end.X - headLength * Math.Cos(wing),
                Y2 = end.Y - headLength * Math.Sin(wing),
                Stroke = brush,
                StrokeThickness = thickness
            });
        }
    }

    private Windows.Foundation.Point ToOverlayPoint(
        CaptureAnnotationPoint point,
        double selectionX,
        double selectionY)
        => new(
            selectionX + DpiCoordinateTransformer.PhysicalToLogical(point.X, _session.Display.DpiX),
            selectionY + DpiCoordinateTransformer.PhysicalToLogical(point.Y, _session.Display.DpiY));

    private static SolidColorBrush AnnotationBrush(CaptureAnnotation annotation)
    {
        var alpha = annotation.Kind == CaptureAnnotationKind.Highlight
            ? Math.Min(annotation.Color.Alpha, (byte)96)
            : annotation.Color.Alpha;
        return new SolidColorBrush(Windows.UI.Color.FromArgb(
            alpha,
            annotation.Color.Red,
            annotation.Color.Green,
            annotation.Color.Blue));
    }

    private static int GetAnnotationThickness(CaptureAnnotationKind kind)
        => kind switch
        {
            CaptureAnnotationKind.Highlight => 18,
            CaptureAnnotationKind.Pen => 4,
            _ => 4
        };

    private CaptureAnnotationPoint ToSelectionLocal(PixelPoint desktopPoint)
    {
        var selection = _selection.Normalize();
        var x = Math.Clamp(desktopPoint.X - selection.X, 0, Math.Max(0, selection.Width - 1));
        var y = Math.Clamp(desktopPoint.Y - selection.Y, 0, Math.Max(0, selection.Height - 1));
        return new CaptureAnnotationPoint(x, y);
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
            UnauthorizedAccessException => "Windows denied access to the capture folder or clipboard.",
            IOException => "The selected region was captured, but the PNG file or clipboard stream could not be completed.",
            ArgumentException => "The selected region is no longer valid. Select the region again.",
            InvalidOperationException => exception.Message,
            _ => "SNAPVERE could not complete the region capture. Press Esc and try again."
        };
}
