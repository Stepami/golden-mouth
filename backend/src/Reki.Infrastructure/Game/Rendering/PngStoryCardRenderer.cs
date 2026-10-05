using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Reki.Application.Game.Common;

namespace Reki.Infrastructure.Game.Rendering;

internal sealed class PngStoryCardRenderer : IStoryCardRenderer
{
    private const int Width = 108;
    private const int Height = 192;

    public async ValueTask RenderAsync(string postId, Stream target, CancellationToken cancellationToken)
    {
        await target.WriteAsync(PngEncoding.Signature, cancellationToken);

        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), Width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), Height);
        header[8] = 8;
        header[9] = 2;
        await PngEncoding.WriteChunkAsync(target, "IHDR", header, cancellationToken);

        using var compressed = new MemoryStream(capacity: Height * 8);
        await using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            byte[] row = PngEncoding.CreateRow(postId, Width);
            for (int rowIndex = 0; rowIndex < Height; rowIndex++)
            {
                row[1] = (byte)(48 + (rowIndex * 96 / Height));
                await zlib.WriteAsync(row, cancellationToken);
            }
        }

        if (!compressed.TryGetBuffer(out ArraySegment<byte> segment))
        {
            throw new InvalidOperationException("Unable to access the bounded PNG buffer.");
        }

        ReadOnlyMemory<byte> compressedData = segment.AsMemory(0, checked((int)compressed.Length));
        await PngEncoding.WriteChunkAsync(target, "IDAT", compressedData, cancellationToken);
        await PngEncoding.WriteChunkAsync(target, "IEND", ReadOnlyMemory<byte>.Empty, cancellationToken);
    }

    private static class PngEncoding
    {
        public static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

        public static byte[] CreateRow(string postId, int width)
        {
            byte seed = postId.Aggregate((byte)37, (current, character) => (byte)(current ^ character));
            byte[] row = new byte[(width * 3) + 1];
            row[0] = 0;
            for (int columnIndex = 0; columnIndex < width; columnIndex++)
            {
                int offset = 1 + (columnIndex * 3);
                row[offset] = (byte)(92 + (seed % 64));
                row[offset + 1] = 48;
                row[offset + 2] = (byte)(130 + (columnIndex * 60 / width));
            }

            return row;
        }

        public static async ValueTask WriteChunkAsync(
            Stream target,
            string type,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken)
        {
            byte[] typeBytes = Encoding.ASCII.GetBytes(type);
            byte[] length = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
            await target.WriteAsync(length, cancellationToken);
            await target.WriteAsync(typeBytes, cancellationToken);
            await target.WriteAsync(data, cancellationToken);

            byte[] crcInput = new byte[typeBytes.Length + data.Length];
            typeBytes.CopyTo(crcInput, 0);
            data.CopyTo(crcInput.AsMemory(typeBytes.Length));
            byte[] crc = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(crc, CalculateCrc32(crcInput));
            await target.WriteAsync(crc, cancellationToken);
        }

        private static uint CalculateCrc32(ReadOnlySpan<byte> data)
        {
            uint crc = uint.MaxValue;
            foreach (byte value in data)
            {
                crc = UpdateCrc(crc, value);
            }

            return ~crc;
        }

        private static uint UpdateCrc(uint crc, byte value)
        {
            crc ^= value;
            for (int bitIndex = 0; bitIndex < 8; bitIndex++)
            {
                uint mask = (uint)-(int)(crc & 1);
                crc = (crc >> 1) ^ (0xEDB88320 & mask);
            }

            return crc;
        }
    }
}
