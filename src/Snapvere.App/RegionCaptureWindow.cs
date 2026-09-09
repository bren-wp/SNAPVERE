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

/// <summary>
/// Runtime-safe region selection and inline editor. The entire visual tree is
/// constructed in C# so installed and portable builds do not depend on a
/// secondary Window XAML resource being resolved by Application.LoadComponent.
/// </summary>
public sealed class RegionCaptureWindow : Window
{
    private const double HandleRadius = 5d;

    private readonly RegionCaptureWorkflow _workflow;
    private readonly PngCaptureEncoder _pngEncoder;
    private readonly RegionCaptureSession _session;
    private readonly TaskCompletionSource<RegionCaptureOutcome> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<CaptureAnnotation> _annotations = [];
    private readonly List<CaptureAnnotationPoint> _draftPoints = [];

    private readonly Grid _overlayRoot;
    private readonly Image _frozenImage;
    private readonly Canvas _overlayCanvas;
    private readonly Canvas _annotationLayer;
    private readonly Rectangle _fullDim;
    private readonly Rectangle _topDim;
    private readonly Rectangle _leftDim;
    private readonly Rectangle _rightDim;
    private readonly Rectangle _bottomDim;
    private readonly Border _selectionBorder;
    private readonly Border _selectionBadge;
    private readonly TextBlock _selectionSizeText;
    private readonly Ellipse _topLeftHandle;
    private readonly Ellipse _topHandle;
    private readonly Ellipse _topRightHandle;
    private readonly Ellipse _rightHandle;
    private readonly Ellipse _bottomRightHandle;
    private readonly Ellipse _bottomHandle;
    private readonly Ellipse _bottomLeftHandle;
    private readonly Ellipse _leftHandle;
    private readonly Border _toolPalette;
    private readonly Border _actionPalette;
    private readonly Border _captureHint;
    private readonly Border _overlayStatus;
    private readonly ProgressRing _saveProgress;
    private readonly TextBlock _overlayStatusText;
    private readonly Button _keyboardFocusTarget;
    private readonly Button _moveToolButton;
    private readonly Button _penToolButton;
    private readonly Button _lineToolButton;
    private readonly Button _arrowToolButton;
    private readonly Button _rectangleToolButton;
    private readonly Button _highlightToolButton;
    private readonly Button _undoButton;

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

        Title = "SNAPVERE — Region Capture";

        _frozenImage = new Image
        {
            Stretch = Stretch.Fill,
            IsHitTestVisible = false
        };
        _overlayCanvas = new Canvas { Background = Brush(0x00, 0x00, 0x00, 0x00) };
        _annotationLayer = new Canvas { IsHitTestVisible = false };

        _fullDim = CreateDimRectangle();
        _topDim = CreateDimRectangle(Visibility.Collapsed);
        _leftDim = CreateDimRectangle(Visibility.Collapsed);
        _rightDim = CreateDimRectangle(Visibility.Collapsed);
        _bottomDim = CreateDimRectangle(Visibility.Collapsed);

        _selectionBorder = new Border
        {
            BorderBrush = Brush(0xFF, 0x7C, 0x6C, 0xFF),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(2),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        _selectionSizeText = Text(string.Empty, 12, Brush(0xFF, 0xFF, 0xFF, 0xFF), Microsoft.UI.Text.FontWeights.SemiBold);
        _selectionBadge = new Border
        {
            Background = Brush(0xF0, 0x15, 0x17, 0x1C),
            BorderBrush = Brush(0x40, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(9, 5, 9, 5),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
            Child = _selectionSizeText
        };

        _topLeftHandle = CreateResizeHandle(SelectionHandle.TopLeft, "Resize top left");
        _topHandle = CreateResizeHandle(SelectionHandle.Top, "Resize top");
        _topRightHandle = CreateResizeHandle(SelectionHandle.TopRight, "Resize top right");
        _rightHandle = CreateResizeHandle(SelectionHandle.Right, "Resize right");
        _bottomRightHandle = CreateResizeHandle(SelectionHandle.BottomRight, "Resize bottom right");
        _bottomHandle = CreateResizeHandle(SelectionHandle.Bottom, "Resize bottom");
        _bottomLeftHandle = CreateResizeHandle(SelectionHandle.BottomLeft, "Resize bottom left");
        _leftHandle = CreateResizeHandle(SelectionHandle.Left, "Resize left");

        _moveToolButton = CreateToolButton("Move", "None", "Move or resize selection");
        _penToolButton = CreateToolButton("Pen", nameof(CaptureAnnotationKind.Pen), "Freehand pen");
        _lineToolButton = CreateToolButton("Line", nameof(CaptureAnnotationKind.Line), "Straight line");
        _arrowToolButton = CreateToolButton("Arrow", nameof(CaptureAnnotationKind.Arrow), "Arrow");
        _rectangleToolButton = CreateToolButton("Box", nameof(CaptureAnnotationKind.Rectangle), "Rectangle");
        _highlightToolButton = CreateToolButton("Mark", nameof(CaptureAnnotationKind.Highlight), "Highlighter");
        _undoButton = CreatePaletteButton("Undo", "Undo last annotation");
        _undoButton.Click += UndoButton_Click;

        _toolPalette = BuildToolPalette();
        _actionPalette = BuildActionPalette();
        _captureHint = BuildCaptureHint();

        _saveProgress = new ProgressRing { Width = 18, Height = 18, IsActive = false };
        _overlayStatusText = Text(string.Empty, 12, Brush(0xFF, 0xFF, 0xFF, 0xFF));
        _overlayStatusText.TextWrapping = TextWrapping.Wrap;
        _overlayStatusText.MaxWidth = 620;
        _overlayStatus = BuildStatusPanel();

        _keyboardFocusTarget = new Button
        {
            Width = 1,
            Height = 1,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Opacity = 0,
            IsTabStop = true
        };
        AutomationProperties.SetName(_keyboardFocusTarget, "Region capture keyboard input");

        _overlayRoot = BuildRoot();
        Content = _overlayRoot;

        WireEvents();
        ConfigureOverlayWindow();
        UpdateToolButtonStates();
    }

    public Task<RegionCaptureOutcome> ShowAsync()
    {
        Activate();
        return _completion.Task;
    }

    private Grid BuildRoot()
    {
        var root = new Grid
        {
            Background = Brush(0xFF, 0x00, 0x00, 0x00),
            RequestedTheme = ElementTheme.Dark
        };
        root.Children.Add(_frozenImage);
        root.Children.Add(_overlayCanvas);
        root.Children.Add(_annotationLayer);
        root.Children.Add(_captureHint);
        root.Children.Add(_overlayStatus);
        root.Children.Add(_keyboardFocusTarget);

        _overlayCanvas.Children.Add(_fullDim);
        _overlayCanvas.Children.Add(_topDim);
        _overlayCanvas.Children.Add(_leftDim);
        _overlayCanvas.Children.Add(_rightDim);
        _overlayCanvas.Children.Add(_bottomDim);
        _overlayCanvas.Children.Add(_selectionBorder);
        _overlayCanvas.Children.Add(_selectionBadge);
        _overlayCanvas.Children.Add(_topLeftHandle);
        _overlayCanvas.Children.Add(_topHandle);
        _overlayCanvas.Children.Add(_topRightHandle);
        _overlayCanvas.Children.Add(_rightHandle);
        _overlayCanvas.Children.Add(_bottomRightHandle);
        _overlayCanvas.Children.Add(_bottomHandle);
        _overlayCanvas.Children.Add(_bottomLeftHandle);
        _overlayCanvas.Children.Add(_leftHandle);
        _overlayCanvas.Children.Add(_toolPalette);
        _overlayCanvas.Children.Add(_actionPalette);
        return root;
    }

    private Border BuildToolPalette()
    {
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(_moveToolButton);
        stack.Children.Add(_penToolButton);
        stack.Children.Add(_lineToolButton);
        stack.Children.Add(_arrowToolButton);
        stack.Children.Add(_rectangleToolButton);
        stack.Children.Add(_highlightToolButton);
        stack.Children.Add(Separator());

        var firstColors = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        firstColors.Children.Add(CreateColorButton("Coral", 0xFF, 0x5A, 0x72));
        firstColors.Children.Add(CreateColorButton("Amber", 0xFF, 0xC8, 0x57));
        stack.Children.Add(firstColors);

        var secondColors = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        secondColors.Children.Add(CreateColorButton("Mint", 0x45, 0xD6, 0xA2));
        secondColors.Children.Add(CreateColorButton("Indigo", 0x7C, 0x6C, 0xFF));
        stack.Children.Add(secondColors);
        stack.Children.Add(Separator());
        stack.Children.Add(_undoButton);

        return Palette(stack);
    }

    private Border BuildActionPalette()
    {
        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        var copy = CreatePaletteButton("Copy", "Copy selection to clipboard");
        copy.Click += CopyButton_Click;
        var save = CreatePaletteButton("Save", "Save PNG");
        save.Click += SaveButton_Click;
        var close = CreatePaletteButton("Close", "Cancel capture");
        close.Click += CancelButton_Click;
        stack.Children.Add(copy);
        stack.Children.Add(save);
        stack.Children.Add(close);
        return Palette(stack);
    }

    private Border BuildCaptureHint()
    {
        var hint = Text(
            "Drag to select · choose a tool to annotate · Enter save · Esc cancel",
            12,
            Brush(0xE8, 0xFF, 0xFF, 0xFF));
        return new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16),
            Padding = new Thickness(12, 8, 12, 8),
            Background = Brush(0xE8, 0x11, 0x13, 0x18),
            BorderBrush = Brush(0x30, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            IsHitTestVisible = false,
            Child = hint
        };
    }

    private Border BuildStatusPanel()
    {
        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        stack.Children.Add(_saveProgress);
        stack.Children.Add(_overlayStatusText);
        return new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(20),
            Padding = new Thickness(14, 9, 14, 9),
            Background = Brush(0xF0, 0x15, 0x17, 0x1C),
            BorderBrush = Brush(0x38, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
            Child = stack
        };
    }

    private void WireEvents()
    {
        _overlayRoot.Loaded += OverlayRoot_Loaded;
        _overlayRoot.SizeChanged += OverlayRoot_SizeChanged;
        _overlayRoot.KeyDown += OverlayRoot_KeyDown;
        _overlayRoot.KeyUp += OverlayRoot_KeyUp;

        _overlayCanvas.PointerPressed += OverlayCanvas_PointerPressed;
        _overlayCanvas.PointerMoved += OverlayCanvas_PointerMoved;
        _overlayCanvas.PointerReleased += OverlayCanvas_PointerReleased;
        _overlayCanvas.PointerCaptureLost += OverlayCanvas_PointerCaptureLost;
        _overlayCanvas.DoubleTapped += OverlayCanvas_DoubleTapped;
        Closed += RegionCaptureWindow_Closed;
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

        AppWindow.MoveAndResize(new RectInt32(bounds.X, bounds.Y, bounds.Width, bounds.Height));
    }

    private async void OverlayRoot_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _frozenImage.Source = await CreateFrozenBitmapAsync(_session.FrozenFrame);
            UpdateSelectionVisuals();
            _ = _keyboardFocusTarget.Focus(FocusState.Programmatic);
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

        var pointerPoint = e.GetCurrentPoint(_overlayCanvas);
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
            _pointerCaptured = _overlayCanvas.CapturePointer(e.Pointer);
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
        var pointerPoint = e.GetCurrentPoint(_overlayCanvas);
        var point = ToDesktopPixel(pointerPoint.Position.X, pointerPoint.Position.Y);
        BeginManipulation(handle, point, e);
        e.Handled = true;
    }

    private void BeginManipulation(SelectionHandle handle, PixelPoint point, PointerRoutedEventArgs e)
    {
        _annotationInProgress = false;
        _isCreatingSelection = false;
        _activeHandle = handle;
        _dragAnchor = point;
        _dragStartSelection = _selection;
        _pointerCaptured = _overlayCanvas.CapturePointer(e.Pointer);
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
        _pointerCaptured = _overlayCanvas.CapturePointer(e.Pointer);
        RenderAnnotations();
    }

    private void OverlayCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_saving || !_pointerCaptured)
        {
            return;
        }

        var pointerPoint = e.GetCurrentPoint(_overlayCanvas);
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
            if (_draftPoints[^1] != localPoint)
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
            _overlayCanvas.ReleasePointerCapture(e.Pointer);
        }

        _pointerCaptured = false;
        _isCreatingSelection = false;
        _activeHandle = SelectionHandle.None;
        _annotationInProgress = false;
        _draftPoints.Clear();
        UpdateSelectionVisuals();
        _ = _keyboardFocusTarget.Focus(FocusState.Programmatic);
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
                }
                else
                {
                    CancelCapture();
                }
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

    private void OverlayRoot_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Shift)
        {
            _shiftDown = false;
        }
    }

    private static bool IsControlDown()
    {
        var state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        return (state & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
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
        _ = _keyboardFocusTarget.Focus(FocusState.Programmatic);
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
        _ = _keyboardFocusTarget.Focus(FocusState.Programmatic);
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        UndoLastAnnotation();
        _ = _keyboardFocusTarget.Focus(FocusState.Programmatic);
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
        var active = Brush(0xFF, 0x5D, 0x52, 0xD9);
        var inactive = Brush(0xFF, 0x21, 0x25, 0x2E);
        _moveToolButton.Background = _activeAnnotationTool is null ? active : inactive;
        _penToolButton.Background = _activeAnnotationTool == CaptureAnnotationKind.Pen ? active : inactive;
        _lineToolButton.Background = _activeAnnotationTool == CaptureAnnotationKind.Line ? active : inactive;
        _arrowToolButton.Background = _activeAnnotationTool == CaptureAnnotationKind.Arrow ? active : inactive;
        _rectangleToolButton.Background = _activeAnnotationTool == CaptureAnnotationKind.Rectangle ? active : inactive;
        _highlightToolButton.Background = _activeAnnotationTool == CaptureAnnotationKind.Highlight ? active : inactive;
        _undoButton.IsEnabled = _annotations.Count > 0;
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
            _ = _keyboardFocusTarget.Focus(FocusState.Programmatic);
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
            await randomAccessStream.WriteAsync(png.ToArray().AsBuffer());
            randomAccessStream.Seek(0);

            var package = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
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
            _ = _keyboardFocusTarget.Focus(FocusState.Programmatic);
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
        _overlayStatusText.Text = message;
        _saveProgress.IsActive = isBusy;
        _overlayStatus.Visibility = Visibility.Visible;
    }

    private void UpdateSelectionVisuals()
    {
        var totalWidth = Math.Max(0d, _overlayCanvas.ActualWidth);
        var totalHeight = Math.Max(0d, _overlayCanvas.ActualHeight);
        SetRectangle(_fullDim, 0, 0, totalWidth, totalHeight);

        if (_selection.IsEmpty)
        {
            _fullDim.Visibility = Visibility.Visible;
            SetSelectionChromeVisibility(Visibility.Collapsed);
            _toolPalette.Visibility = Visibility.Collapsed;
            _actionPalette.Visibility = Visibility.Collapsed;
            _captureHint.Visibility = Visibility.Visible;
            _annotationLayer.Children.Clear();
            _annotationLayer.Clip = null;
            return;
        }

        _fullDim.Visibility = Visibility.Collapsed;
        SetSelectionChromeVisibility(Visibility.Visible);
        _toolPalette.Visibility = Visibility.Visible;
        _actionPalette.Visibility = Visibility.Visible;
        _captureHint.Visibility = Visibility.Collapsed;

        var displayBounds = _session.Display.Bounds.Normalize();
        var selection = RegionSelectionGeometry.Clamp(_selection, displayBounds);
        var x = DpiCoordinateTransformer.PhysicalToLogical(selection.X - displayBounds.X, _session.Display.DpiX);
        var y = DpiCoordinateTransformer.PhysicalToLogical(selection.Y - displayBounds.Y, _session.Display.DpiY);
        var width = DpiCoordinateTransformer.PhysicalToLogical(selection.Width, _session.Display.DpiX);
        var height = DpiCoordinateTransformer.PhysicalToLogical(selection.Height, _session.Display.DpiY);
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

        Canvas.SetLeft(_selectionBorder, x);
        Canvas.SetTop(_selectionBorder, y);
        _selectionBorder.Width = width;
        _selectionBorder.Height = height;

        _selectionSizeText.Text = $"{selection.Width} × {selection.Height}";
        _selectionBadge.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var badgeWidth = _selectionBadge.DesiredSize.Width;
        var badgeHeight = _selectionBadge.DesiredSize.Height;
        Canvas.SetLeft(_selectionBadge, Math.Clamp(x, 0d, Math.Max(0d, totalWidth - badgeWidth)));
        var badgeY = y >= badgeHeight + 8d ? y - badgeHeight - 8d : Math.Min(totalHeight - badgeHeight, bottom + 8d);
        Canvas.SetTop(_selectionBadge, Math.Max(0d, badgeY));

        PlaceHandle(_topLeftHandle, x, y);
        PlaceHandle(_topHandle, x + width / 2d, y);
        PlaceHandle(_topRightHandle, right, y);
        PlaceHandle(_rightHandle, right, y + height / 2d);
        PlaceHandle(_bottomRightHandle, right, bottom);
        PlaceHandle(_bottomHandle, x + width / 2d, bottom);
        PlaceHandle(_bottomLeftHandle, x, bottom);
        PlaceHandle(_leftHandle, x, y + height / 2d);

        PositionFloatingPalettes(x, y, right, bottom, totalWidth, totalHeight);
        RenderAnnotations();
        UpdateToolButtonStates();
    }

    private void PositionFloatingPalettes(double x, double y, double right, double bottom, double totalWidth, double totalHeight)
    {
        _toolPalette.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        _actionPalette.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var toolWidth = _toolPalette.DesiredSize.Width;
        var toolHeight = _toolPalette.DesiredSize.Height;
        var actionWidth = _actionPalette.DesiredSize.Width;
        var actionHeight = _actionPalette.DesiredSize.Height;

        var toolX = right + 8d + toolWidth <= totalWidth
            ? right + 8d
            : x >= toolWidth + 8d
                ? x - toolWidth - 8d
                : Math.Clamp(right - toolWidth - 8d, 0d, Math.Max(0d, totalWidth - toolWidth));
        Canvas.SetLeft(_toolPalette, toolX);
        Canvas.SetTop(_toolPalette, Math.Clamp(y, 0d, Math.Max(0d, totalHeight - toolHeight)));

        Canvas.SetLeft(_actionPalette, Math.Clamp(right - actionWidth, 0d, Math.Max(0d, totalWidth - actionWidth)));
        Canvas.SetTop(
            _actionPalette,
            bottom + 8d + actionHeight <= totalHeight ? bottom + 8d : Math.Max(0d, y - actionHeight - 8d));
    }

    private void RenderAnnotations()
    {
        _annotationLayer.Children.Clear();
        if (_selection.IsEmpty)
        {
            _annotationLayer.Clip = null;
            return;
        }

        var displayBounds = _session.Display.Bounds.Normalize();
        var selection = RegionSelectionGeometry.Clamp(_selection, displayBounds);
        var selectionX = DpiCoordinateTransformer.PhysicalToLogical(selection.X - displayBounds.X, _session.Display.DpiX);
        var selectionY = DpiCoordinateTransformer.PhysicalToLogical(selection.Y - displayBounds.Y, _session.Display.DpiY);
        var selectionWidth = DpiCoordinateTransformer.PhysicalToLogical(selection.Width, _session.Display.DpiX);
        var selectionHeight = DpiCoordinateTransformer.PhysicalToLogical(selection.Height, _session.Display.DpiY);

        _annotationLayer.Clip = new RectangleGeometry
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
        var thickness = Math.Max(1d, DpiCoordinateTransformer.PhysicalToLogical(annotation.Thickness, _session.Display.DpiX));

        switch (annotation.Kind)
        {
            case CaptureAnnotationKind.Pen:
            case CaptureAnnotationKind.Highlight:
                var polyline = new Polyline { Stroke = brush, StrokeThickness = thickness };
                foreach (var point in annotation.Points)
                {
                    polyline.Points.Add(ToOverlayPoint(point, selectionX, selectionY));
                }
                _annotationLayer.Children.Add(polyline);
                break;

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
        _annotationLayer.Children.Add(new Line
        {
            X1 = first.X,
            Y1 = first.Y,
            X2 = second.X,
            Y2 = second.Y,
            Stroke = brush,
            StrokeThickness = thickness
        });
    }

    private void AddRectangleVisual(CaptureAnnotation annotation, double selectionX, double selectionY, SolidColorBrush brush, double thickness)
    {
        var first = ToOverlayPoint(annotation.Points[0], selectionX, selectionY);
        var second = ToOverlayPoint(annotation.Points[^1], selectionX, selectionY);
        var rectangle = new Rectangle
        {
            Width = Math.Abs(second.X - first.X),
            Height = Math.Abs(second.Y - first.Y),
            Stroke = brush,
            StrokeThickness = thickness
        };
        Canvas.SetLeft(rectangle, Math.Min(first.X, second.X));
        Canvas.SetTop(rectangle, Math.Min(first.Y, second.Y));
        _annotationLayer.Children.Add(rectangle);
    }

    private void AddArrowVisual(CaptureAnnotation annotation, double selectionX, double selectionY, SolidColorBrush brush, double thickness)
    {
        var start = ToOverlayPoint(annotation.Points[0], selectionX, selectionY);
        var end = ToOverlayPoint(annotation.Points[^1], selectionX, selectionY);
        _annotationLayer.Children.Add(new Line
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
            _annotationLayer.Children.Add(new Line
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

    private Windows.Foundation.Point ToOverlayPoint(CaptureAnnotationPoint point, double selectionX, double selectionY)
        => new(
            selectionX + DpiCoordinateTransformer.PhysicalToLogical(point.X, _session.Display.DpiX),
            selectionY + DpiCoordinateTransformer.PhysicalToLogical(point.Y, _session.Display.DpiY));

    private static SolidColorBrush AnnotationBrush(CaptureAnnotation annotation)
    {
        var alpha = annotation.Kind == CaptureAnnotationKind.Highlight
            ? Math.Min(annotation.Color.Alpha, (byte)96)
            : annotation.Color.Alpha;
        return Brush(alpha, annotation.Color.Red, annotation.Color.Green, annotation.Color.Blue);
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
        return new CaptureAnnotationPoint(
            Math.Clamp(desktopPoint.X - selection.X, 0, Math.Max(0, selection.Width - 1)),
            Math.Clamp(desktopPoint.Y - selection.Y, 0, Math.Max(0, selection.Height - 1)));
    }

    private void SetSelectionChromeVisibility(Visibility visibility)
    {
        _topDim.Visibility = visibility;
        _leftDim.Visibility = visibility;
        _rightDim.Visibility = visibility;
        _bottomDim.Visibility = visibility;
        _selectionBorder.Visibility = visibility;
        _selectionBadge.Visibility = visibility;
        _topLeftHandle.Visibility = visibility;
        _topHandle.Visibility = visibility;
        _topRightHandle.Visibility = visibility;
        _rightHandle.Visibility = visibility;
        _bottomRightHandle.Visibility = visibility;
        _bottomHandle.Visibility = visibility;
        _bottomLeftHandle.Visibility = visibility;
        _leftHandle.Visibility = visibility;
    }

    private static Rectangle CreateDimRectangle(Visibility visibility = Visibility.Visible)
        => new()
        {
            Fill = Brush(0xA0, 0x00, 0x00, 0x00),
            IsHitTestVisible = false,
            Visibility = visibility
        };

    private Ellipse CreateResizeHandle(SelectionHandle handle, string automationName)
    {
        var ellipse = new Ellipse
        {
            Tag = handle.ToString(),
            Width = 10,
            Height = 10,
            Fill = Brush(0xFF, 0xFF, 0xFF, 0xFF),
            Stroke = Brush(0xFF, 0x7C, 0x6C, 0xFF),
            StrokeThickness = 2,
            Visibility = Visibility.Collapsed
        };
        AutomationProperties.SetName(ellipse, automationName);
        ellipse.PointerPressed += Handle_PointerPressed;
        return ellipse;
    }

    private Button CreateToolButton(string text, string tag, string tooltip)
    {
        var button = CreatePaletteButton(text, tooltip);
        button.Tag = tag;
        button.MinWidth = 58;
        button.Click += AnnotationToolButton_Click;
        return button;
    }

    private static Button CreatePaletteButton(string text, string tooltip)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(8, 5, 8, 5),
            MinHeight = 30,
            Foreground = Brush(0xFF, 0xFF, 0xFF, 0xFF)
        };
        ToolTipService.SetToolTip(button, tooltip);
        return button;
    }

    private Button CreateColorButton(string tag, byte red, byte green, byte blue)
    {
        var button = new Button
        {
            Tag = tag,
            Width = 25,
            Height = 25,
            Padding = new Thickness(0),
            Content = new Ellipse
            {
                Width = 13,
                Height = 13,
                Fill = Brush(0xFF, red, green, blue)
            }
        };
        ToolTipService.SetToolTip(button, tag);
        button.Click += ColorButton_Click;
        return button;
    }

    private static Rectangle Separator()
        => new()
        {
            Height = 1,
            Fill = Brush(0x35, 0xFF, 0xFF, 0xFF),
            Margin = new Thickness(4, 2, 4, 2)
        };

    private static Border Palette(UIElement child)
        => new()
        {
            Visibility = Visibility.Collapsed,
            Background = Brush(0xF5, 0x11, 0x13, 0x18),
            BorderBrush = Brush(0x45, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(5),
            Child = child
        };

    private static TextBlock Text(string value, double size, SolidColorBrush foreground, Microsoft.UI.Text.FontWeight? weight = null)
        => new()
        {
            Text = value,
            FontSize = size,
            Foreground = foreground,
            FontWeight = weight ?? Microsoft.UI.Text.FontWeights.Normal
        };

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static void SetRectangle(Rectangle rectangle, double x, double y, double width, double height)
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
