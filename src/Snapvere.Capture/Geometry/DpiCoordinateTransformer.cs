using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Geometry;

public static class DpiCoordinateTransformer
{
    public const double DefaultDpi = 96d;

    public static PixelPoint LogicalToPhysical(PixelPoint logical, uint dpiX, uint dpiY)
        => new(
            Scale(logical.X, dpiX),
            Scale(logical.Y, dpiY));

    public static PixelPoint LogicalToPhysical(double logicalX, double logicalY, uint dpiX, uint dpiY)
        => new(
            Scale(logicalX, dpiX),
            Scale(logicalY, dpiY));

    public static PixelRect LogicalToPhysical(PixelRect logical, uint dpiX, uint dpiY)
        => new(
            Scale(logical.X, dpiX),
            Scale(logical.Y, dpiY),
            Scale(logical.Width, dpiX),
            Scale(logical.Height, dpiY));

    public static PixelPoint PhysicalToLogical(PixelPoint physical, uint dpiX, uint dpiY)
        => new(
            UnscaleToInt(physical.X, dpiX),
            UnscaleToInt(physical.Y, dpiY));

    public static PixelRect PhysicalToLogical(PixelRect physical, uint dpiX, uint dpiY)
        => new(
            UnscaleToInt(physical.X, dpiX),
            UnscaleToInt(physical.Y, dpiY),
            UnscaleToInt(physical.Width, dpiX),
            UnscaleToInt(physical.Height, dpiY));

    public static double PhysicalToLogical(int physical, uint dpi)
        => physical * DefaultDpi / NormalizeDpi(dpi);

    public static int LogicalToPhysical(double logical, uint dpi)
        => Scale(logical, dpi);

    private static int Scale(int value, uint dpi)
        => Scale((double)value, dpi);

    private static int Scale(double value, uint dpi)
        => checked((int)Math.Round(value * NormalizeDpi(dpi) / DefaultDpi, MidpointRounding.AwayFromZero));

    private static int UnscaleToInt(int value, uint dpi)
        => checked((int)Math.Round(PhysicalToLogical(value, dpi), MidpointRounding.AwayFromZero));

    private static double NormalizeDpi(uint dpi)
        => dpi == 0 ? DefaultDpi : dpi;
}
