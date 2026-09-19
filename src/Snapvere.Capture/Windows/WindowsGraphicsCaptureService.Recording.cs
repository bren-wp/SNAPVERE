using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using WinRT;
using D3D = Windows.Win32.Graphics.Direct3D11;
using D3DCommon = Windows.Win32.Graphics.Direct3D;

namespace Snapvere.Capture.Windows;

public sealed partial class WindowsGraphicsCaptureService : IScreenRecordingService
{
    public async Task<ScreenRecordingSessionResult> RecordDisplayAsync(
        DisplayDescriptor display,
        bool includeCursor,
        Stream destination,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(destination);
        stopToken.ThrowIfCancellationRequested();

        if (!destination.CanWrite || !destination.CanSeek)
        {
            throw new ArgumentException(
                "Screen recording requires a writable seekable destination stream.",
                nameof(destination));
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) || !IsSupported())
        {
            throw new PlatformNotSupportedException(
                "Screen recording requires Windows 10 version 2004 (build 19041) or later.");
        }

        var bounds = display.Bounds.Normalize();
        var sourceSize = new PixelSize(bounds.Width, bounds.Height);
        var encodedSize = ScreenRecordingPolicy.GetEncodedSize(sourceSize);

        var monitor = ResolveMonitor(bounds);
        if (monitor == nint.Zero)
        {
            throw new InvalidOperationException(
                "SNAPVERE could not resolve the native monitor handle for screen recording.");
        }

        PInvoke.D3D11CreateDevice(
            pAdapter: null,
            D3DCommon.D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
            Software: default,
            D3D.D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
            pFeatureLevels: default,
            SDKVersion: D3D11SdkVersion,
            out var device,
            out _,
            out var context).ThrowOnFailure();

        var startedAt = _timeProvider.GetUtcNow();
        try
        {
            var winrtDevice = CreateDirect3DDevice(device);
            try
            {
                var item = CreateItemForMonitor(monitor);
                if (item.Size.Width != sourceSize.Width || item.Size.Height != sourceSize.Height)
                {
                    throw new InvalidOperationException(
                        "The selected display changed size before screen recording could start.");
                }

                using var frameSource = new RecordingFrameSource(
                    winrtDevice,
                    item,
                    sourceSize,
                    includeCursor);
                using var stopRegistration = stopToken.Register(
                    static state => ((RecordingFrameSource)state!).RequestStop(),
                    frameSource);

                var inputProperties = VideoEncodingProperties.CreateUncompressed(
                    MediaEncodingSubtypes.Bgra8,
                    (uint)sourceSize.Width,
                    (uint)sourceSize.Height);
                var descriptor = new VideoStreamDescriptor(inputProperties);
                var mediaSource = new MediaStreamSource(descriptor)
                {
                    BufferTime = TimeSpan.Zero
                };
                frameSource.Attach(mediaSource);

                var outputProfile = new MediaEncodingProfile();
                outputProfile.Container.Subtype = "MPEG4";
                outputProfile.Video.Subtype = MediaEncodingSubtypes.H264;
                outputProfile.Video.Width = (uint)encodedSize.Width;
                outputProfile.Video.Height = (uint)encodedSize.Height;
                outputProfile.Video.Bitrate = ScreenRecordingPolicy.GetBitrate(encodedSize);
                outputProfile.Video.FrameRate.Numerator = ScreenRecordingPolicy.FrameRate;
                outputProfile.Video.FrameRate.Denominator = 1;
                outputProfile.Video.PixelAspectRatio.Numerator = 1;
                outputProfile.Video.PixelAspectRatio.Denominator = 1;

                var transcoder = new MediaTranscoder
                {
                    HardwareAccelerationEnabled = true
                };

                using var randomAccess = destination.AsRandomAccessStream();
                frameSource.Start();

                var prepared = await transcoder
                    .PrepareMediaStreamSourceTranscodeAsync(mediaSource, randomAccess, outputProfile);
                if (!prepared.CanTranscode)
                {
                    throw new InvalidOperationException(
                        $"Windows Media Transcoder rejected the screen recording profile ({prepared.FailureReason}).");
                }

                await prepared.TranscodeAsync();
                await randomAccess.FlushAsync();

                if (frameSource.Failure is not null)
                {
                    throw new InvalidOperationException(
                        "Screen recording stopped because the capture source became unavailable.",
                        frameSource.Failure);
                }
            }
            finally
            {
                (winrtDevice as IDisposable)?.Dispose();
            }
        }
        finally
        {
            (context as IDisposable)?.Dispose();
            (device as IDisposable)?.Dispose();
        }

        return new ScreenRecordingSessionResult(
            sourceSize,
            encodedSize,
            startedAt,
            _timeProvider.GetUtcNow());
    }

    private sealed class RecordingFrameSource : IDisposable
    {
        private readonly object _gate = new();
        private readonly IDirect3DDevice _device;
        private readonly GraphicsCaptureItem _item;
        private readonly PixelSize _sourceSize;
        private readonly bool _includeCursor;
        private readonly SemaphoreSlim _frameAvailable = new(0, 1);
        private readonly HashSet<Direct3D11CaptureFrame> _inFlight = [];

        private Direct3D11CaptureFramePool? _framePool;
        private GraphicsCaptureSession? _session;
        private Direct3D11CaptureFrame? _latestFrame;
        private MediaStreamSource? _mediaSource;
        private TimeSpan? _firstTimestamp;
        private bool _stopping;
        private bool _disposed;

        internal RecordingFrameSource(
            IDirect3DDevice device,
            GraphicsCaptureItem item,
            PixelSize sourceSize,
            bool includeCursor)
        {
            _device = device;
            _item = item;
            _sourceSize = sourceSize;
            _includeCursor = includeCursor;
            _item.Closed += Item_Closed;
        }

        internal Exception? Failure { get; private set; }

        internal void Attach(MediaStreamSource mediaSource)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _mediaSource = mediaSource ?? throw new ArgumentNullException(nameof(mediaSource));
            _mediaSource.Starting += MediaSource_Starting;
            _mediaSource.SampleRequested += MediaSource_SampleRequested;
        }

        internal void Start()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_mediaSource is null)
            {
                throw new InvalidOperationException(
                    "The recording media source must be attached before capture starts.");
            }

            _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                _device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                numberOfBuffers: 2,
                _item.Size);
            _framePool.FrameArrived += FramePool_FrameArrived;

            _session = _framePool.CreateCaptureSession(_item);
            _session.IsCursorCaptureEnabled = _includeCursor;
            _session.StartCapture();
        }

        internal void RequestStop()
        {
            Direct3D11CaptureFrame? pending = null;
            lock (_gate)
            {
                if (_stopping || _disposed)
                {
                    return;
                }

                _stopping = true;
                pending = _latestFrame;
                _latestFrame = null;
            }

            pending?.Dispose();
            SignalFrameAvailable();
        }

        private void FramePool_FrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            Direct3D11CaptureFrame? frame = null;
            Direct3D11CaptureFrame? replaced = null;
            try
            {
                frame = sender.TryGetNextFrame();
                if (frame is null)
                {
                    return;
                }

                if (frame.ContentSize.Width != _sourceSize.Width ||
                    frame.ContentSize.Height != _sourceSize.Height)
                {
                    SetFailure(new InvalidOperationException(
                        "The recorded display changed resolution during screen recording."));
                    frame.Dispose();
                    return;
                }

                lock (_gate)
                {
                    if (_stopping || _disposed)
                    {
                        frame.Dispose();
                        return;
                    }

                    replaced = _latestFrame;
                    _latestFrame = frame;
                    frame = null;
                }

                replaced?.Dispose();
                SignalFrameAvailable();
            }
            catch (Exception exception)
            {
                frame?.Dispose();
                replaced?.Dispose();
                SetFailure(exception);
            }
        }

        private void Item_Closed(GraphicsCaptureItem sender, object args)
            => SetFailure(new InvalidOperationException(
                "The recorded display capture item closed unexpectedly."));

        private void MediaSource_Starting(
            MediaStreamSource sender,
            MediaStreamSourceStartingEventArgs args)
            => args.Request.SetActualStartPosition(TimeSpan.Zero);

        private async void MediaSource_SampleRequested(
            MediaStreamSource sender,
            MediaStreamSourceSampleRequestedEventArgs args)
        {
            var deferral = args.Request.GetDeferral();
            try
            {
                var frame = await TakeNextFrameAsync().ConfigureAwait(false);
                if (frame is null)
                {
                    args.Request.Sample = null;
                    return;
                }

                var origin = _firstTimestamp ??= frame.SystemRelativeTime;
                var timestamp = frame.SystemRelativeTime - origin;
                if (timestamp < TimeSpan.Zero)
                {
                    timestamp = TimeSpan.Zero;
                }

                var sample = MediaStreamSample.CreateFromDirect3D11Surface(
                    frame.Surface,
                    timestamp);

                lock (_gate)
                {
                    if (_disposed)
                    {
                        frame.Dispose();
                        args.Request.Sample = null;
                        return;
                    }

                    _inFlight.Add(frame);
                }

                sample.Processed += (_, _) => ReleaseFrame(frame);
                args.Request.Sample = sample;
            }
            catch (Exception exception)
            {
                SetFailure(exception);
                args.Request.Sample = null;
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async Task<Direct3D11CaptureFrame?> TakeNextFrameAsync()
        {
            while (true)
            {
                lock (_gate)
                {
                    if (_stopping || _disposed)
                    {
                        return null;
                    }

                    if (_latestFrame is not null)
                    {
                        var frame = _latestFrame;
                        _latestFrame = null;
                        return frame;
                    }
                }

                await _frameAvailable.WaitAsync().ConfigureAwait(false);
            }
        }

        private void ReleaseFrame(Direct3D11CaptureFrame frame)
        {
            var shouldDispose = false;
            lock (_gate)
            {
                shouldDispose = _inFlight.Remove(frame);
            }

            if (shouldDispose)
            {
                frame.Dispose();
            }
        }

        private void SetFailure(Exception exception)
        {
            lock (_gate)
            {
                if (Failure is null)
                {
                    Failure = exception;
                }
            }

            RequestStop();
        }

        private void SignalFrameAvailable()
        {
            if (_frameAvailable.CurrentCount == 0)
            {
                try
                {
                    _frameAvailable.Release();
                }
                catch (SemaphoreFullException)
                {
                }
            }
        }

        public void Dispose()
        {
            Direct3D11CaptureFrame? latest;
            Direct3D11CaptureFrame[] inFlight;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _stopping = true;
                latest = _latestFrame;
                _latestFrame = null;
                inFlight = [.. _inFlight];
                _inFlight.Clear();
            }

            if (_mediaSource is not null)
            {
                _mediaSource.Starting -= MediaSource_Starting;
                _mediaSource.SampleRequested -= MediaSource_SampleRequested;
                _mediaSource = null;
            }

            if (_framePool is not null)
            {
                _framePool.FrameArrived -= FramePool_FrameArrived;
            }

            _item.Closed -= Item_Closed;
            _session?.Dispose();
            _session = null;
            _framePool?.Dispose();
            _framePool = null;

            latest?.Dispose();
            foreach (var frame in inFlight)
            {
                frame.Dispose();
            }

            SignalFrameAvailable();
            _frameAvailable.Dispose();
        }
    }
}
