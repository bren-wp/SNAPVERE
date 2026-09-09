using System.Runtime.InteropServices;
using Snapvere.Capture;
using Snapvere.Domain.Capture;

namespace Snapvere.UnitTests;

public sealed class ResilientScreenCaptureServiceTests
{
    private static readonly DisplayDescriptor Display = new(
        "test-display",
        new PixelRect(0, 0, 2, 2),
        new PixelRect(0, 0, 2, 2),
        96,
        96,
        true,
        "Test display");

    [Fact]
    public async Task PreferredSuccess_DoesNotInvokeFallback()
    {
        var expected = CreateFrame("preferred");
        var preferred = FakeCaptureService.Return(expected);
        var fallback = FakeCaptureService.Return(CreateFrame("fallback"));
        var service = new ResilientScreenCaptureService(preferred, fallback);

        var actual = await service.CaptureDisplayAsync(Display, includeCursor: true);

        Assert.Same(expected, actual);
        Assert.Equal(1, preferred.CallCount);
        Assert.Equal(0, fallback.CallCount);
        Assert.True(preferred.LastIncludeCursor);
    }

    [Fact]
    public async Task ExpectedPreferredFailures_InvokeFallback()
    {
        Exception[] expectedFailures =
        [
            new PlatformNotSupportedException(),
            new TimeoutException(),
            new COMException(),
            new InvalidOperationException()
        ];

        foreach (var exception in expectedFailures)
        {
            var expected = CreateFrame("fallback");
            var preferred = FakeCaptureService.Throw(exception);
            var fallback = FakeCaptureService.Return(expected);
            var service = new ResilientScreenCaptureService(preferred, fallback);

            var actual = await service.CaptureDisplayAsync(Display, includeCursor: false);

            Assert.Same(expected, actual);
            Assert.Equal(1, preferred.CallCount);
            Assert.Equal(1, fallback.CallCount);
        }
    }

    [Fact]
    public async Task CallerCancellation_DoesNotInvokeFallback()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var preferred = FakeCaptureService.Return(CreateFrame("preferred"));
        var fallback = FakeCaptureService.Return(CreateFrame("fallback"));
        var service = new ResilientScreenCaptureService(preferred, fallback);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.CaptureDisplayAsync(Display, false, cancellation.Token));

        Assert.Equal(0, preferred.CallCount);
        Assert.Equal(0, fallback.CallCount);
    }

    [Fact]
    public async Task PreferredCancellation_DoesNotInvokeFallbackWhenCallerIsCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        var preferred = new FakeCaptureService((_, _, token) =>
        {
            cancellation.Cancel();
            return ValueTask.FromCanceled<CaptureFrame>(token);
        });
        var fallback = FakeCaptureService.Return(CreateFrame("fallback"));
        var service = new ResilientScreenCaptureService(preferred, fallback);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.CaptureDisplayAsync(Display, false, cancellation.Token));

        Assert.Equal(1, preferred.CallCount);
        Assert.Equal(0, fallback.CallCount);
    }

    [Fact]
    public async Task UnexpectedPreferredFailure_IsNotHiddenByFallback()
    {
        var preferred = FakeCaptureService.Throw(new ArgumentException("programming error"));
        var fallback = FakeCaptureService.Return(CreateFrame("fallback"));
        var service = new ResilientScreenCaptureService(preferred, fallback);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.CaptureDisplayAsync(Display, false));

        Assert.Equal("programming error", exception.Message);
        Assert.Equal(1, preferred.CallCount);
        Assert.Equal(0, fallback.CallCount);
    }

    private static CaptureFrame CreateFrame(string source)
        => new(
            new PixelSize(2, 2),
            8,
            new byte[16],
            DateTimeOffset.UnixEpoch,
            source);

    private sealed class FakeCaptureService(
        Func<DisplayDescriptor, bool, CancellationToken, ValueTask<CaptureFrame>> capture)
        : IScreenCaptureService
    {
        private readonly Func<DisplayDescriptor, bool, CancellationToken, ValueTask<CaptureFrame>> _capture = capture;

        public int CallCount { get; private set; }
        public bool LastIncludeCursor { get; private set; }

        public static FakeCaptureService Return(CaptureFrame frame)
            => new((_, _, _) => ValueTask.FromResult(frame));

        public static FakeCaptureService Throw(Exception exception)
            => new((_, _, _) => ValueTask.FromException<CaptureFrame>(exception));

        public ValueTask<CaptureFrame> CaptureDisplayAsync(
            DisplayDescriptor display,
            bool includeCursor,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastIncludeCursor = includeCursor;
            return _capture(display, includeCursor, cancellationToken);
        }
    }
}
