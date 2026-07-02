using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CrossFire.Replay.Interop;

/// <summary>
/// Blittable struct read/write using pinned spans.
/// </summary>
public static class StructReader
{
    public static T Read<T>(ReadOnlySpan<byte> data) where T : unmanaged
    {
        if (data.Length < Unsafe.SizeOf<T>())
            throw new ArgumentException($"Need at least {Unsafe.SizeOf<T>()} bytes for {typeof(T).Name}.");

        return MemoryMarshal.Read<T>(data);
    }

    public static bool TryRead<T>(ReadOnlySpan<byte> data, out T value) where T : unmanaged
    {
        if (data.Length < Unsafe.SizeOf<T>())
        {
            value = default;
            return false;
        }

        value = MemoryMarshal.Read<T>(data);
        return true;
    }

    public static byte[] Write<T>(T value) where T : unmanaged
    {
        var bytes = new byte[Unsafe.SizeOf<T>()];
        MemoryMarshal.Write(bytes, in value);
        return bytes;
    }
}
