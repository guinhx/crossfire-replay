using System.Security.Cryptography;

namespace CrossFire.Replay.Compression;

internal static class AesCbcEncryptor
{
    public static byte[] Encrypt(ReadOnlySpan<byte> plainText, byte[] key, byte[] iv)
    {
        if (plainText.Length % 16 != 0)
            throw new ArgumentException("AES-CBC plaintext must be block-aligned.", nameof(plainText));

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plainText.ToArray(), 0, plainText.Length);
    }
}
