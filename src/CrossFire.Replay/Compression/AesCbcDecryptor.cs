using System.Security.Cryptography;

namespace CrossFire.Replay.Compression;

internal static class AesCbcDecryptor
{
    public static byte[] Decrypt(ReadOnlySpan<byte> cipherText, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipherText.ToArray(), 0, cipherText.Length);
    }
}
