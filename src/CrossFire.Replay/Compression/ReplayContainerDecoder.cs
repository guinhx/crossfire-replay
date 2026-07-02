using System.Security.Cryptography;
using CrossFire.Replay.Abstractions;

namespace CrossFire.Replay.Compression;

/// <summary>
/// Outer replay container decoder (Brotli-only and AES-wrapped variants).
/// </summary>
public sealed class ReplayContainerDecoder : IReplayContainerDecoder
{
    private static ReadOnlySpan<byte> CompressSignature => "CF_ConDeV"u8;

    public bool CanDecode(ReadOnlySpan<byte> fileBytes) =>
        fileBytes.Length >= 8 && fileBytes[0] is 0 or 1;

    public ReplayContainerInfo Decode(ReadOnlySpan<byte> fileBytes)
    {
        if (fileBytes.Length < 8)
            throw new ReplayParseException("Replay container is too small.", 0);

        var kindByte = fileBytes[0];
        var headerWord = ReadUInt32(fileBytes, 0);
        var declaredSize = ReadUInt32(fileBytes, 4);

        if (declaredSize == 0)
            throw new ReplayParseException("Replay container declares zero output size.", 4);

        var payload = kindByte switch
        {
            0 => DecodeBrotliWrapper(fileBytes, (int)declaredSize),
            1 => DecodeEncryptedWrapper(fileBytes, (int)declaredSize),
            _ => throw new ReplayParseException($"Unknown replay container kind byte {kindByte}.", 0),
        };

        return new ReplayContainerInfo
        {
            Kind = kindByte switch
            {
                0 => ReplayContainerKind.BrotliWrapper,
                1 => ReplayContainerKind.EncryptedBrotliWrapper,
                _ => ReplayContainerKind.None,
            },
            Payload = payload,
            DeclaredOutputSize = declaredSize,
            HeaderWord = headerWord,
        };
    }

    private static byte[] DecodeBrotliWrapper(ReadOnlySpan<byte> fileBytes, int declaredSize)
    {
        // Type-0 container: Brotli payload follows the 8-byte header.
        var encoded = fileBytes[8..];
        return DecompressToCapacity(encoded, encodedSizeBudget: fileBytes.Length, declaredSize);
    }

    private static byte[] DecodeEncryptedWrapper(ReadOnlySpan<byte> fileBytes, int declaredSize)
    {
        // Type-1 container: AES-CBC decrypt, then inner CF_ConDeV + Brotli.
        var headerWord = ReadUInt32(fileBytes, 0);
        var keyIndex = (int)((headerWord >> 16) & 0xFF);
        var ivIndex = (int)((headerWord >> 24) & 0xFF);

        if (keyIndex >= CfDictionary.Entries.Length || ivIndex >= CfDictionary.Entries.Length)
            throw new ReplayParseException("Replay AES dictionary index out of range.", 0);

        var cipherText = fileBytes[8..];
        var plain = AesCbcDecryptor.Decrypt(cipherText, CfDictionary.Entries[keyIndex], CfDictionary.Entries[ivIndex]);

        if (plain.Length < 13 || !plain.AsSpan(0, 9).SequenceEqual(CompressSignature))
            throw new ReplayParseException("Replay AES payload signature mismatch.", 0);

        var innerDeclared = (int)ReadUInt32(plain, 9);
        if (innerDeclared != declaredSize)
            throw new ReplayParseException(
                $"Replay AES inner size mismatch (wrapper {declaredSize}, payload {innerDeclared}).",
                9);

        var brotliInput = plain[13..];
        var cipherLength = fileBytes.Length - 8;
        return DecompressToCapacity(brotliInput, encodedSizeBudget: cipherLength, declaredSize);
    }

    private static byte[] DecompressToCapacity(ReadOnlySpan<byte> encoded, int encodedSizeBudget, int declaredSize)
    {
        var output = new byte[declaredSize];
        if (!NativeBrotliDecompressBuffer.TryDecompress(
                encoded,
                encodedSizeBudget,
                declaredSize,
                output,
                out var written))
        {
            throw new ReplayParseException("Brotli decompression failed.", 0);
        }

        if (written != declaredSize)
            throw new ReplayParseException(
                $"Brotli output size mismatch (expected {declaredSize}, got {written}).",
                0);

        return output;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) =>
        data[offset]
        | ((uint)data[offset + 1] << 8)
        | ((uint)data[offset + 2] << 16)
        | ((uint)data[offset + 3] << 24);
}
