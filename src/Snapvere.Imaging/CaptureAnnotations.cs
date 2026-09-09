using Snapvere.Capture;

namespace Snapvere.Imaging;

public enum CaptureAnnotationKind
{
    Pen,
    Line,
    Arrow,
    Rectangle,
    Highlight
}

public readonly record struct CaptureAnnotationPoint(int X, int Y);

public readonly record struct CaptureAnnotationColor(byte Red, byte Green, byte Blue, byte Alpha = byte.MaxValue)
{
    public static CaptureAnnotationColor Coral => new(0xFF, 0x5A, 0x72);
    public static CaptureAnnotationColor Amber => new(0xFF, 0xC8, 0x57);
    public static CaptureAnnotationColor Mint => new(0x45, 0xD6, 0xA2);
    public static CaptureAnnotationColor Indigo => new(0x7C, 0x6C, 0xFF);
}

public sealed record CaptureAnnotation(
    CaptureAnnotationKind Kind,
    IReadOnlyList<CaptureAnnotationPoint> Points,
    CaptureAnnotationColor Color,
    int Thickness)
{
    public void Validate()
    {
        if (Points is null || Points.Count == 0)
        {
            throw new ArgumentException("An annotation must contain at least one point.", nameof(Points));
        }

        if (Thickness is < 1 or > 96)
        {
            throw new ArgumentOutOfRangeException(nameof(Thickness), "Annotation thickness must be between 1 and 96 pixels.");
        }

        var requiredPoints = Kind is CaptureAnnotationKind.Pen or CaptureAnnotationKind.Highlight ? 1 : 2;
        if (Points.Count < requiredPoints)
        {
            throw new ArgumentException($"{Kind} requires at least {requiredPoints} point(s).", nameof(Points));
        }
    }
}

public static class CaptureFrameAnnotator
{
    public static CaptureFrame Apply(
        CaptureFrame source,
        IReadOnlyList<CaptureAnnotation>? annotations)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.Validate();

        if (annotations is null || annotations.Count == 0)
        {
            return source;
        }

        var pixels = source.Bgra8Pixels.ToArray();
        foreach (var annotation in annotations)
        {
            ArgumentNullException.ThrowIfNull(annotation);
            annotation.Validate();
            DrawAnnotation(pixels, source.Stride, source.Size.Width, source.Size.Height, annotation);
        }

        var result = new CaptureFrame(
            source.Size,
            source.Stride,
            pixels,
            source.CapturedAt,
            source.SourceId);
        result.Validate();
        return result;
    }

    private static void DrawAnnotation(
        byte[] pixels,
        int stride,
        int width,
        int height,
        CaptureAnnotation annotation)
    {
        var color = annotation.Kind == CaptureAnnotationKind.Highlight
            ? annotation.Color with { Alpha = Math.Min(annotation.Color.Alpha, (byte)96) }
            : annotation.Color;

        switch (annotation.Kind)
        {
            case CaptureAnnotationKind.Pen:
            case CaptureAnnotationKind.Highlight:
                if (annotation.Points.Count == 1)
                {
                    DrawDisc(pixels, stride, width, height, annotation.Points[0], annotation.Thickness, color);
                    return;
                }

                for (var index = 1; index < annotation.Points.Count; index++)
                {
                    DrawSegment(
                        pixels,
                        stride,
                        width,
                        height,
                        annotation.Points[index - 1],
                        annotation.Points[index],
                        annotation.Thickness,
                        color);
                }
                break;

            case CaptureAnnotationKind.Line:
                DrawSegment(
                    pixels,
                    stride,
                    width,
                    height,
                    annotation.Points[0],
                    annotation.Points[^1],
                    annotation.Thickness,
                    color);
                break;

            case CaptureAnnotationKind.Rectangle:
                DrawRectangle(
                    pixels,
                    stride,
                    width,
                    height,
                    annotation.Points[0],
                    annotation.Points[^1],
                    annotation.Thickness,
                    color);
                break;

            case CaptureAnnotationKind.Arrow:
                DrawArrow(
                    pixels,
                    stride,
                    width,
                    height,
                    annotation.Points[0],
                    annotation.Points[^1],
                    annotation.Thickness,
                    color);
                break;
        }
    }

    private static void DrawRectangle(
        byte[] pixels,
        int stride,
        int width,
        int height,
        CaptureAnnotationPoint first,
        CaptureAnnotationPoint second,
        int thickness,
        CaptureAnnotationColor color)
    {
        var left = Math.Min(first.X, second.X);
        var top = Math.Min(first.Y, second.Y);
        var right = Math.Max(first.X, second.X);
        var bottom = Math.Max(first.Y, second.Y);

        var topLeft = new CaptureAnnotationPoint(left, top);
        var topRight = new CaptureAnnotationPoint(right, top);
        var bottomRight = new CaptureAnnotationPoint(right, bottom);
        var bottomLeft = new CaptureAnnotationPoint(left, bottom);

        DrawSegment(pixels, stride, width, height, topLeft, topRight, thickness, color);
        DrawSegment(pixels, stride, width, height, topRight, bottomRight, thickness, color);
        DrawSegment(pixels, stride, width, height, bottomRight, bottomLeft, thickness, color);
        DrawSegment(pixels, stride, width, height, bottomLeft, topLeft, thickness, color);
    }

    private static void DrawArrow(
        byte[] pixels,
        int stride,
        int width,
        int height,
        CaptureAnnotationPoint start,
        CaptureAnnotationPoint end,
        int thickness,
        CaptureAnnotationColor color)
    {
        DrawSegment(pixels, stride, width, height, start, end, thickness, color);

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt((double)dx * dx + (double)dy * dy);
        if (length < 1d)
        {
            return;
        }

        var headLength = Math.Clamp(Math.Max(10d, thickness * 4d), 10d, Math.Max(10d, length * 0.45d));
        var angle = Math.Atan2(dy, dx);
        const double wingAngle = 0.58d;

        var wingOne = new CaptureAnnotationPoint(
            checked((int)Math.Round(end.X - headLength * Math.Cos(angle - wingAngle))),
            checked((int)Math.Round(end.Y - headLength * Math.Sin(angle - wingAngle))));
        var wingTwo = new CaptureAnnotationPoint(
            checked((int)Math.Round(end.X - headLength * Math.Cos(angle + wingAngle))),
            checked((int)Math.Round(end.Y - headLength * Math.Sin(angle + wingAngle))));

        DrawSegment(pixels, stride, width, height, end, wingOne, thickness, color);
        DrawSegment(pixels, stride, width, height, end, wingTwo, thickness, color);
    }

    private static void DrawSegment(
        byte[] pixels,
        int stride,
        int width,
        int height,
        CaptureAnnotationPoint start,
        CaptureAnnotationPoint end,
        int thickness,
        CaptureAnnotationColor color)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (steps == 0)
        {
            DrawDisc(pixels, stride, width, height, start, thickness, color);
            return;
        }

        for (var step = 0; step <= steps; step++)
        {
            var progress = step / (double)steps;
            var point = new CaptureAnnotationPoint(
                checked((int)Math.Round(start.X + dx * progress)),
                checked((int)Math.Round(start.Y + dy * progress)));
            DrawDisc(pixels, stride, width, height, point, thickness, color);
        }
    }

    private static void DrawDisc(
        byte[] pixels,
        int stride,
        int width,
        int height,
        CaptureAnnotationPoint center,
        int thickness,
        CaptureAnnotationColor color)
    {
        var radius = Math.Max(1, thickness) / 2d;
        var radiusSquared = radius * radius;
        var minX = Math.Max(0, checked((int)Math.Floor(center.X - radius)));
        var maxX = Math.Min(width - 1, checked((int)Math.Ceiling(center.X + radius)));
        var minY = Math.Max(0, checked((int)Math.Floor(center.Y - radius)));
        var maxY = Math.Min(height - 1, checked((int)Math.Ceiling(center.Y + radius)));

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var deltaX = x - center.X;
                var deltaY = y - center.Y;
                if ((double)deltaX * deltaX + (double)deltaY * deltaY <= radiusSquared)
                {
                    BlendPixel(pixels, stride, x, y, color);
                }
            }
        }
    }

    private static void BlendPixel(
        byte[] pixels,
        int stride,
        int x,
        int y,
        CaptureAnnotationColor color)
    {
        var offset = checked(y * stride + x * 4);
        var alpha = color.Alpha;
        var inverseAlpha = byte.MaxValue - alpha;

        pixels[offset] = BlendChannel(pixels[offset], color.Blue, alpha, inverseAlpha);
        pixels[offset + 1] = BlendChannel(pixels[offset + 1], color.Green, alpha, inverseAlpha);
        pixels[offset + 2] = BlendChannel(pixels[offset + 2], color.Red, alpha, inverseAlpha);
        pixels[offset + 3] = byte.MaxValue;
    }

    private static byte BlendChannel(byte destination, byte source, int alpha, int inverseAlpha)
        => checked((byte)((source * alpha + destination * inverseAlpha + 127) / 255));
}
