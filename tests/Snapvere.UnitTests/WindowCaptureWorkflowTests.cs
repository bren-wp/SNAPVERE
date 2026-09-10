using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;

namespace Snapvere.UnitTests;

public sealed class WindowCaptureWorkflowTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "SNAPVERE-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void DiscoveryMethodsForwardToWindowDiscovery()
    {
        var first = CreateWindow(new nint(101), "Editor", new PixelRect(10, 20, 800, 600));
        var second = CreateWindow(new nint(202), "Browser", new PixelRect(900, 40, 1000, 700));
        var discovery = new FakeWindowDiscovery([first, second], second);
        var workflow = CreateWorkflow(discovery, new FakeWindowCaptureService(CreateFrame()));

        Assert.Equal([first, second], workflow.GetAvailableWindows());
        Assert.Same(second, workflow.TryGetWindowAtPoint(new PixelPoint(950, 100)));
        Assert.Equal(new PixelPoint(950, 100), discovery.LastHitTestPoint);
    }

    [Fact]
    public async Task CaptureWindowWritesPngThroughSharedAtomicWriter()
    {
        var target = CreateWindow(new nint(303), "Terminal", new PixelRect(-120, 60, 640, 480));
        var frame = CreateFrame();
        var captureService = new FakeWindowCaptureService(frame);
        var workflow = CreateWorkflow(new FakeWindowDiscovery([target], target), captureService);

        var result = await workflow.CaptureWindowToDefaultFolderAsync(
            target,
            includeCursor: false,
            CancellationToken.None);

        Assert.True(File.Exists(result.FilePath));
        Assert.Equal(frame.Size.Width, result.Width);
        Assert.Equal(frame.Size.Height, result.Height);
        Assert.Same(target, captureService.LastWindow);
        Assert.False(captureService.LastIncludeCursor);

        var signature = await File.ReadAllBytesAsync(result.FilePath, CancellationToken.None);
        Assert.True(signature.Length > 8);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, signature[..8]);
    }

    [Fact]
    public async Task CallerCancellationDoesNotInvokeWindowCaptureBackend()
    {
        var target = CreateWindow(new nint(404), "Cancelled", new PixelRect(0, 0, 320, 240));
        var captureService = new FakeWindowCaptureService(CreateFrame());
        var workflow = CreateWorkflow(new FakeWindowDiscovery([target], target), captureService);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => workflow.CaptureWindowToDefaultFolderAsync(target, true, cancellation.Token));

        Assert.Null(captureService.LastWindow);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_temporaryDirectory))
            {
                Directory.Delete(_temporaryDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private WindowCaptureWorkflow CreateWorkflow(
        IWindowDiscovery discovery,
        IWindowCaptureService captureService)
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 9, 10, 12, 30, 0, TimeSpan.Zero));
        var writer = new CaptureFileWriter(
            new PngCaptureEncoder(),
            new CapturePathProvider(_temporaryDirectory),
            timeProvider);
        return new WindowCaptureWorkflow(discovery, captureService, writer);
    }

    private static WindowDescriptor CreateWindow(nint handle, string title, PixelRect bounds)
        => new(handle, title, bounds, 1234, "TestWindowClass");

    private static CaptureFrame CreateFrame()
    {
        var pixels = new byte[]
        {
            10, 20, 30, 255,
            40, 50, 60, 255,
            70, 80, 90, 255,
            100, 110, 120, 255
        };

        return new CaptureFrame(
            new PixelSize(2, 2),
            8,
            pixels,
            new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero),
            "window-test");
    }

    private sealed class FakeWindowDiscovery(
        IReadOnlyList<WindowDescriptor> windows,
        WindowDescriptor? hitTestResult) : IWindowDiscovery
    {
        public PixelPoint? LastHitTestPoint { get; private set; }

        public IReadOnlyList<WindowDescriptor> GetWindows() => windows;

        public WindowDescriptor? TryGetWindowAtPoint(PixelPoint point)
        {
            LastHitTestPoint = point;
            return hitTestResult;
        }
    }

    private sealed class FakeWindowCaptureService(CaptureFrame frame) : IWindowCaptureService
    {
        public WindowDescriptor? LastWindow { get; private set; }
        public bool LastIncludeCursor { get; private set; }

        public ValueTask<CaptureFrame> CaptureWindowAsync(
            WindowDescriptor window,
            bool includeCursor,
            CancellationToken cancellationToken = default)
        {
            LastWindow = window;
            LastIncludeCursor = includeCursor;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(frame);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
