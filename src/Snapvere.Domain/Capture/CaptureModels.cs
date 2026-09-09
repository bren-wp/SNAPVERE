namespace Snapvere.Domain.Capture;

public readonly record struct PixelPoint(int X, int Y);

public readonly record struct PixelSize(int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Left => X;
    public int Top => Y;
    public int Right => checked(X + Width);
    public int Bottom => checked(Y + Height);
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public PixelRect Normalize()
    {
        var left = Math.Min(X, Right);
        var top = Math.Min(Y, Bottom);
        var right = Math.Max(X, Right);
        var bottom = Math.Max(Y, Bottom);
        return new PixelRect(left, top, right - left, bottom - top);
    }
}

public sealed record DisplayDescriptor(
    string Id,
    PixelRect Bounds,
    PixelRect WorkArea,
    uint DpiX,
    uint DpiY,
    bool IsPrimary,
    string? FriendlyName);

public enum CaptureMode
{
    Region,
    Window,
    FullScreen,
    Monitor,
    AllScreens,
    LastRegion,
    FixedSize,
    Delayed,
    Scrolling,
    Freeform
}
