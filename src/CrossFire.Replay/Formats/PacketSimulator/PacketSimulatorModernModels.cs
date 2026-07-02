namespace CrossFire.Replay.Formats.PacketSimulator;

public sealed class PacketSimulatorModernDescriptor
{
    public uint Flags { get; init; }
    public uint FieldA { get; init; }
    public uint FieldB { get; init; }
    public uint MapTypeId { get; init; }
    public uint SpectatingArchiveSize { get; init; }
    public uint GameArchiveSize { get; init; }
    public uint SpecialEffectArchiveSize { get; init; }
    public uint UnknownWord { get; init; }
    public string MapLabel { get; init; } = string.Empty;
    public string HostLabel { get; init; } = string.Empty;
}

public sealed class RawPacketArchive
{
    public required string Name { get; init; }
    public required byte[] Data { get; init; }
}
