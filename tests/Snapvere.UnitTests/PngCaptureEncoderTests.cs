using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Snapvere.Capture;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;

namespace Snapvere.UnitTests;

public sealed class PngCaptureEncoderTests
{
    [Fact]
    public async Task EncodeAsync_WritesValidDimensionsAndRgbaPixels()
    {
        var frame = new CaptureFrame(
            new PixelSize(2, 1),
            8,
            new byte[]
            {
                0, 0, 255, 255,
                0, 255, 0, 128
            },
            DateTimeOffset.UnixEpoch,
            "test");

        var encoder = new PngCaptureEncoder();
        await using var output = new MemoryStream();

        await encoder.EncodeAsync(frame, output);

        var png = output.ToArray();
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.AsSpan(0, 8).ToArray());

        var offset = 8;
        var idat = new MemoryStream();
        var sawIhdr = false;
        var sawIend = false;

        while (offset < png.Length)
        {
            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset, 4)));
            offset += 4;
            var type = Encoding.ASCII.GetString(png, offset, 4);
            offset += 4;
            var data = png.AsSpan(offset, length).ToArray();
            offset += length;
            offset += 4; // CRC

            switch (type)
            {
                case "IHDR":
                    Assert.Equal(2u, BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0, 4)));
                    Assert.Equal(1u, BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4, 4)));
                    sawIhdr = true;
                    break;
                case "IDAT":
                    idat.Write(data);
                    break;
                case "IEND":
                    sawIend = true;
                    break;
            }
        }

        Assert.True(sawIhdr);
        Assert.True(sawIend);

        idat.Position = 0;
        await using var zlib = new ZLibStream(idat, CompressionMode.Decompress, leaveOpen: true);
        await using var raw = new MemoryStream();
        await zlib.CopyToAsync(raw);

        Assert.Equal(
            new byte[] { 0, 255, 0, 0, 255, 0, 255, 0, 128 },
            raw.ToArray());
    }
}
