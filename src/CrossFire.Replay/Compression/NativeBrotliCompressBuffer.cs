using System.Buffers;
using System.IO.Compression;

namespace CrossFire.Replay.Compression;

/// <summary>
/// Brotli compression counterpart to <see cref="NativeBrotliDecompressBuffer"/>.
/// </summary>
internal static class NativeBrotliCompressBuffer
{
    public static byte[] Compress(ReadOnlySpan<byte> payload, int quality = 11, int window = 22)
    {
        if (payload.IsEmpty)
            return Array.Empty<byte>();

        var maxSize = BrotliEncoder.GetMaxCompressedLength(payload.Length);
        var encoder = new BrotliEncoder(quality, window);
        var buffer = new byte[maxSize];

        var status = encoder.Compress(payload, buffer, out _, out var written, isFinalBlock: true);
        if (status != OperationStatus.Done || written <= 0)
            throw new InvalidOperationException("Brotli compression failed.");

        return buffer[..written];
    }
}
