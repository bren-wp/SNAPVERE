using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Geometry;

public static class DpiCoordinateTransformer
{
    public const double DefaultDpi = 96d;

    public static PixelPoint LogicalToPhysical(PixelPoint logical, uint dpiX, uint dpiY)
        => new(
            Scale(logical.X, dpiX),
            Scale(logical.Y, dpiY));

    public static PixelRect LogicalToPhysical(PixelRect logical, uint dpiX, uint dpiY)
        => new(
            Scale(logical.X, dpiX),
            Scale(logical.Y, dpiY),
            Scale(logical.Width, dpiX),
            Scale(logical.Height, dpiY));

    public static PixelPoint PhysicalToLogical(PixelPoint physical, uint dpiX, uint dpiY)
        => new(
            Unscale(physical.X, dpiX),
            Unscale(physical.Y, dpiY));

    public static PixelRect PhysicalToLogical(PixelRect physical, uint dpiX, uint dpiY)
        => new(
            Unscale(physical.X, dpiX),
            Unscale(physical.Y, dpiY),
            Unscale(physical.Width, dpiX),
            Unscale(physical.Height, dpiY));

    private static int Scale(int value, uint dpi)
        => checked((int)Math.Round(value * NormalizeDpi(dpi) / DefaultDpi, MidpointRounding.AwayFromZero));

    private static int Unscale(int value, uint dpi)
        => checked((int)Math.Round(value * DefaultDpi / NormalizeDpi(dpi), MidpointRounding.AwayFromZero));

    private static double NormalizeDpi(uint dpi)
        => dpi == 0 ? DefaultDpi : dpi;
}
