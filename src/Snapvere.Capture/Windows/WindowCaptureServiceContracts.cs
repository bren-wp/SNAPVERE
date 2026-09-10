namespace Snapvere.Capture.Windows;

public interface IWindowCaptureService
{
    ValueTask<CaptureFrame> CaptureWindowAsync(
        WindowDescriptor window,
        bool includeCursor,
        CancellationToken cancellationToken = default);
}
