namespace CrossFire.Replay.Formats.PacketSimulator;

public sealed class PacketSimulatorHeaderBlock
{
    public uint DeathMatchTypeRaw { get; init; }
    public uint GameGoal { get; init; }
    public Protocol.Game.DeathMatchType DeathMatchType => (Protocol.Game.DeathMatchType)DeathMatchTypeRaw;
    public short MapId { get; init; }
    public bool IsClanGame { get; init; }
    public string ClanNameGr { get; init; } = string.Empty;
    public string ClanNameBl { get; init; } = string.Empty;
    public bool IsClanHalfTime { get; init; }
    public int HalfTimeScoreGr { get; init; }
    public int HalfTimeScoreBl { get; init; }
    public byte[] RawBlock { get; init; } = Array.Empty<byte>();
}

public sealed class TimestampPacketRecord
{
    public uint PayloadSize { get; init; }
    public uint Timestamp { get; init; }
    public Protocol.Lt.EMessageId? MessageId { get; init; }
    public Protocol.Lt.LtDecodedMessage? Decoded { get; init; }
    public byte[] Payload { get; init; } = Array.Empty<byte>();
    public PacketTimelineSource? Source { get; init; }
    public TimestampPayloadKind PayloadKind { get; init; } = TimestampPayloadKind.Standard;

    /// <summary>Byte offset of [u32 size][u32 ts] in the source archive, when known.</summary>
    public int SourceOffset { get; init; } = -1;
}

public sealed class SpecialEffectPacketRecord
{
    public uint PayloadSize { get; init; }
    public uint Timestamp { get; init; }
    public ushort ObjectId { get; init; }
    public Protocol.Lt.EMessageId? MessageId { get; init; }
    public Protocol.Lt.LtDecodedMessage? Decoded { get; init; }
    public byte[] Payload { get; init; } = Array.Empty<byte>();

    /// <summary>Byte offset of [u32 size][u32 ts][u16 objectId] in the source archive, when known.</summary>
    public int SourceOffset { get; init; } = -1;
}

public sealed class SpecialEffectAssetRecord
{
    public int Offset { get; init; }
    public string AssetPath { get; init; } = string.Empty;
}

public sealed class PacketSection<T>
{
    public IReadOnlyList<T> Packets { get; init; } = Array.Empty<T>();
    public int BytesConsumed { get; init; }
}
