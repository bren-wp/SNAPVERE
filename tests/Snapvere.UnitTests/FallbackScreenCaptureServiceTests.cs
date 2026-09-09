using Snapvere.Capture;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;

namespace Snapvere.UnitTests;

public sealed class FallbackScreenCaptureServiceTests
{
    private static readonly DisplayDescriptor Display = new(
        "DISPLAY1",
        new PixelRect(0, 0, 1920, 1080),
        new PixelRect(0, 0, 1920, 1040),
        96,
        96,
        true,
        "DISPLAY1");

    [Fact]
    public async Task CaptureDisplayAsync_ReturnsPrimaryFrame_WhenPrimarySucceeds()
    {
        var primaryFrame = CreateFrame("primary");
        var primary = new FakeCaptureService((_, _, _) => ValueTask.FromResult(primaryFrame));
        var fallback = new FakeCaptureService((_, _, _) =>
            throw new InvalidOperationException("Fallback must not be called."));
        var service = new FallbackScreenCaptureService(primary, fallback);

        var result = await service.CaptureDisplayAsync(Display, includeCursor: false);

        Assert.Same(primaryFrame, result);
        Assert.Equal(1, primary.CallCount);
        Assert.Equal(0, fallback.CallCount);
    }

    [Fact]
    public async Task CaptureDisplayAsync_UsesFallback_WhenPrimaryIsUnsupported()
    {
        var fallbackFrame = CreateFrame("fallback");
        var primary = new FakeCaptureService((_, _, _) =>
            throw new NotSupportedException("WGC unavailable."));
        var fallback = new FakeCaptureService((_, _, _) => ValueTask.FromResult(fallbackFrame));
        var service = new FallbackScreenCaptureService(primary, fallback);

        var result = await service.CaptureDisplayAsync(Display, includeCursor: true);

        Assert.Same(fallbackFrame, result);
        Assert.Equal(1, primary.CallCount);
        Assert.Equal(1, fallback.CallCount);
    }

    [Fact]
    public async Task CaptureDisplayAsync_DoesNotFallback_WhenPrimaryIsCancelled()
    {
        var primary = new FakeCaptureService((_, _, _) =>
            ValueTask.FromException<CaptureFrame>(new OperationCanceledException()));
        var fallback = new FakeCaptureService((_, _, _) => ValueTask.FromResult(CreateFrame("fallback")));
        var service = new FallbackScreenCaptureService(primary, fallback);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.CaptureDisplayAsync(Display, includeCursor: false).AsTask());

        Assert.Equal(1, primary.CallCount);
        Assert.Equal(0, fallback.CallCount);
    }

    [Fact]
    public async Task CaptureDisplayAsync_UsesFallback_WhenPrimaryTimesOut()
    {
        var fallbackFrame = CreateFrame("fallback-timeout");
        var primary = new FakeCaptureService((_, _, _) =>
            ValueTask.FromException<CaptureFrame>(new TimeoutException("No frame.")));
        var fallback = new FakeCaptureService((_, _, _) => ValueTask.FromResult(fallbackFrame));
        var service = new FallbackScreenCaptureService(primary, fallback);

        var result = await service.CaptureDisplayAsync(Display, includeCursor: false);

        Assert.Same(fallbackFrame, result);
        Assert.Equal(1, fallback.CallCount);
    }

    private static CaptureFrame CreateFrame(string sourceId)
    {
        var pixels = new byte[] { 1, 2, 3, 255 };
        return new CaptureFrame(
            new PixelSize(1, 1),
            4,
            pixels,
            DateTimeOffset.UnixEpoch,
            sourceId);
    }

    private sealed class FakeCaptureService(
        Func<DisplayDescriptor, bool, CancellationToken, ValueTask<CaptureFrame>> capture)
        : IScreenCaptureService
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public ValueTask<CaptureFrame> CaptureDisplayAsync(
            DisplayDescriptor display,
            bool includeCursor,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            return capture(display, includeCursor, cancellationToken);
        }
    }
}
