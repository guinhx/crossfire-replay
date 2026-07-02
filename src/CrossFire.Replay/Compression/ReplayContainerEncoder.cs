using CrossFire.Replay.Abstractions;

namespace CrossFire.Replay.Compression;

/// <summary>
/// Outer replay container encoder (Brotli-only and AES-wrapped variants).
/// </summary>
public sealed class ReplayContainerEncoder : IReplayContainerEncoder
{
    private static ReadOnlySpan<byte> CompressSignature => "CF_ConDeV"u8;

    public byte[] EncodeBrotliWrapper(ReadOnlySpan<byte> payload)
    {
        var compressed = NativeBrotliCompressBuffer.Compress(payload);
        var file = new byte[8 + compressed.Length];

        // Kind byte 0: header word 256 (0x00000100 LE).
        WriteUInt32(file, 0, 256);
        WriteUInt32(file, 4, (uint)payload.Length);
        compressed.CopyTo(file.AsSpan(8));

        return file;
    }

    public byte[] EncodeEncryptedWrapper(ReadOnlySpan<byte> payload, int keyIndex, int ivIndex)
    {
        if (keyIndex < 0 || keyIndex >= CfDictionary.Entries.Length)
            throw new ArgumentOutOfRangeException(nameof(keyIndex));
        if (ivIndex < 0 || ivIndex >= CfDictionary.Entries.Length)
            throw new ArgumentOutOfRangeException(nameof(ivIndex));

        var compressed = NativeBrotliCompressBuffer.Compress(payload);
        var innerLength = 9 + 4 + compressed.Length;
        var paddedLength = (innerLength + 15) & ~15;
        var innerPlain = new byte[paddedLength];

        CompressSignature.CopyTo(innerPlain);
        WriteUInt32(innerPlain, 9, (uint)payload.Length);
        compressed.CopyTo(innerPlain.AsSpan(13));

        var cipherText = AesCbcEncryptor.Encrypt(
            innerPlain,
            CfDictionary.Entries[keyIndex],
            CfDictionary.Entries[ivIndex]);

        var file = new byte[8 + cipherText.Length];
        file[0] = 1;
        file[2] = (byte)keyIndex;
        file[3] = (byte)ivIndex;
        WriteUInt32(file, 4, (uint)payload.Length);
        cipherText.CopyTo(file.AsSpan(8));

        return file;
    }

    private static void WriteUInt32(Span<byte> buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
