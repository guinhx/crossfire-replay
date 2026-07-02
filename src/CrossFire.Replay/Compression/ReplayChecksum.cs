using System.Security.Cryptography;
using System.Text;

namespace CrossFire.Replay.Compression;

/// <summary>
/// Replay body checksum used when optional CFR encryption is enabled.
/// </summary>
public static class ReplayChecksum
{
    public const int BlockSize = 0x20;

    /// <summary>Builds the 32-byte prefix written before the CFR body when USE_REPLAY_CRYPT is enabled.</summary>
    public static byte[] ComputeBlock(ReadOnlySpan<byte> body)
    {
        var md5Hex = ToUpperHexMd5(body);
        var lengthText = body.Length.ToString("D4");
        var block = Encoding.ASCII.GetBytes(md5Hex);

        for (var i = 0; i < BlockSize; i++)
            block[i] += (byte)(lengthText[i % 4] - '0');

        return block;
    }

    public static bool TryValidateBlock(ReadOnlySpan<byte> body, ReadOnlySpan<byte> checksumBlock)
    {
        if (checksumBlock.Length != BlockSize)
            return false;

        var expected = ComputeBlock(body);
        return checksumBlock.SequenceEqual(expected);
    }

    private static string ToUpperHexMd5(ReadOnlySpan<byte> body)
    {
        Span<byte> digest = stackalloc byte[16];
        MD5.HashData(body, digest);

        var hex = new char[32];
        for (var i = 0; i < 16; i++)
        {
            var b = digest[i];
            hex[i * 2] = GetHexChar(b >> 4);
            hex[i * 2 + 1] = GetHexChar(b & 0x0F);
        }

        return new string(hex);
    }

    private static char GetHexChar(int nibble) =>
        (char)(nibble < 10 ? '0' + nibble : 'A' + (nibble - 10));
}
