using System.Buffers.Binary;

namespace CrossFire.Replay.Formats.PacketSimulator;

public sealed record ModernExtraBlock
{
    public uint Value { get; init; }
    public bool IsZero => Value == 0;
}

public static class ModernExtraBlockReader
{
    public static ModernExtraBlock? TryRead(ReadOnlySpan<byte> extra)
    {
        if (extra.Length != PacketSimulatorLayout.ModernExtraBlockSize)
            return null;

        return new ModernExtraBlock
        {
            Value = BinaryPrimitives.ReadUInt32LittleEndian(extra),
        };
    }
}
