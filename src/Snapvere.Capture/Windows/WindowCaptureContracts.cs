using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Windows;

/// <summary>
/// Stable description of a capturable top-level Windows desktop window.
/// The native handle is process-local metadata and is never persisted.
/// </summary>
public sealed record WindowDescriptor(
    nint NativeHandle,
    string Title,
    PixelRect Bounds,
    uint ProcessId,
    string? ClassName);

public interface IWindowDiscovery
{
    IReadOnlyList<WindowDescriptor> GetWindows();

    WindowDescriptor? TryGetWindowAtPoint(PixelPoint point);
}
