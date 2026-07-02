using System.Buffers;
using System.IO.Compression;

namespace CrossFire.Replay.Compression;

/// <summary>
/// Brotli decompress with an encoded-size budget (stream may end before the budget is consumed).
/// </summary>
internal static class NativeBrotliDecompressBuffer
{
    public static bool TryDecompress(
        ReadOnlySpan<byte> encodedBuffer,
        int encodedSizeBudget,
        int decodedCapacity,
        Span<byte> decodedBuffer,
        out int totalOut)
    {
        totalOut = 0;

        if (encodedBuffer.IsEmpty || decodedCapacity <= 0 || decodedBuffer.IsEmpty)
            return false;

        var budget = Math.Min(encodedSizeBudget, encodedBuffer.Length);
        var input = encodedBuffer[..budget];
        var output = decodedBuffer[..Math.Min(decodedCapacity, decodedBuffer.Length)];

        var decoder = new BrotliDecoder();
        var status = decoder.Decompress(input, output, out _, out var written);

        if (status != OperationStatus.Done || written <= 0)
            return false;

        totalOut = written;
        return true;
    }
}
