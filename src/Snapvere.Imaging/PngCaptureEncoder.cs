using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Snapvere.Capture;

namespace Snapvere.Imaging;

public sealed class PngCaptureEncoder
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly byte[] IhdrType = Encoding.ASCII.GetBytes("IHDR");
    private static readonly byte[] IdatType = Encoding.ASCII.GetBytes("IDAT");
    private static readonly byte[] IendType = Encoding.ASCII.GetBytes("IEND");

    public async Task EncodeAsync(
        CaptureFrame frame,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(destination);

        if (!destination.CanWrite)
        {
            throw new ArgumentException("Destination stream must be writable.", nameof(destination));
        }

        frame.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        await destination.WriteAsync(Signature, cancellationToken).ConfigureAwait(false);

        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0, 4), checked((uint)frame.Size.Width));
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4, 4), checked((uint)frame.Size.Height));
        header[8] = 8;
        header[9] = 6;
        header[10] = 0;
        header[11] = 0;
        header[12] = 0;
        await WriteChunkAsync(destination, IhdrType, header, cancellationToken).ConfigureAwait(false);

        var compressed = BuildCompressedImageData(frame, cancellationToken);
        await WriteChunkAsync(destination, IdatType, compressed, cancellationToken).ConfigureAwait(false);
        await WriteChunkAsync(destination, IendType, ReadOnlyMemory<byte>.Empty, cancellationToken).ConfigureAwait(false);
    }

    private static byte[] BuildCompressedImageData(
        CaptureFrame frame,
        CancellationToken cancellationToken)
    {
        var source = frame.Bgra8Pixels.Span;
        var rowLength = checked(frame.Size.Width * 4);
        var scanline = new byte[checked(rowLength + 1)];

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            for (var y = 0; y < frame.Size.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                scanline[0] = 0;

                var sourceRow = source.Slice(checked(y * frame.Stride), rowLength);
                for (var x = 0; x < frame.Size.Width; x++)
                {
                    var sourceOffset = checked(x * 4);
                    var destinationOffset = checked(1 + sourceOffset);

                    scanline[destinationOffset] = sourceRow[sourceOffset + 2];
                    scanline[destinationOffset + 1] = sourceRow[sourceOffset + 1];
                    scanline[destinationOffset + 2] = sourceRow[sourceOffset];
                    scanline[destinationOffset + 3] = sourceRow[sourceOffset + 3];
                }

                zlib.Write(scanline, 0, scanline.Length);
            }
        }

        return compressed.ToArray();
    }

    private static async Task WriteChunkAsync(
        Stream destination,
        byte[] type,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        var length = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, checked((uint)data.Length));
        await destination.WriteAsync(length, cancellationToken).ConfigureAwait(false);
        await destination.WriteAsync(type, cancellationToken).ConfigureAwait(false);
        await destination.WriteAsync(data, cancellationToken).ConfigureAwait(false);

        var crcBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, Crc32.Compute(type, data.Span));
        await destination.WriteAsync(crcBytes, cancellationToken).ConfigureAwait(false);
    }

    private static class Crc32
    {
        private const uint Polynomial = 0xEDB88320;
        private static readonly uint[] Table = CreateTable();

        internal static uint Compute(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
        {
            var crc = uint.MaxValue;
            crc = Update(crc, first);
            crc = Update(crc, second);
            return crc ^ uint.MaxValue;
        }

        private static uint Update(uint crc, ReadOnlySpan<byte> data)
        {
            foreach (var value in data)
            {
                crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
            }

            return crc;
        }

        private static uint[] CreateTable()
        {
            var table = new uint[256];
            for (uint index = 0; index < table.Length; index++)
            {
                var value = index;
                for (var bit = 0; bit < 8; bit++)
                {
                    value = (value & 1) != 0
                        ? Polynomial ^ (value >> 1)
                        : value >> 1;
                }

                table[index] = value;
            }

            return table;
        }
    }
}
