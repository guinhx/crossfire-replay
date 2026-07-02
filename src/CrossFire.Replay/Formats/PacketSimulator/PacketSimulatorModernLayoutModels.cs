namespace CrossFire.Replay.Formats.PacketSimulator;

public enum MiddleBlobLayoutPieceKind
{
    Gap = 0,
    Segment = 1,
    Tail = 2,
}

public sealed class MiddleBlobLayoutPiece
{
    public MiddleBlobLayoutPieceKind Kind { get; init; }
    public byte[] Data { get; init; } = Array.Empty<byte>();
}

public sealed class MiddleBlobSegmentData
{
    public MiddleBlobSegmentRange Range { get; init; } = new();
    public IReadOnlyList<TimestampPacketRecord> Packets { get; init; } = Array.Empty<TimestampPacketRecord>();
    public byte[] RawBytes { get; init; } = Array.Empty<byte>();
}

public sealed class ModernArchiveLayout
{
    public IltPacketArchiveWriter.TaggedHeaderOptions? TaggedHeader { get; init; }
    public PacketSection<TimestampPacketRecord> TopLevelPackets { get; init; } = new();
    public IReadOnlyList<byte[]> WirePieces { get; init; } = Array.Empty<byte[]>();
    public byte[] RawBytes { get; init; } = Array.Empty<byte>();
}
