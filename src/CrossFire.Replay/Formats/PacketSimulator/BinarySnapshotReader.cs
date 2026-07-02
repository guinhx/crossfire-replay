namespace CrossFire.Replay.Formats.PacketSimulator;

public sealed class BinarySnapshotHeader
{
    /// <summary>Chunk size for the body (typically 65536).</summary>
    public uint Field0 { get; init; }

    /// <summary>Duplicate of <see cref="Field0"/> in observed replays.</summary>
    public uint Field1 { get; init; }

    public uint Field2 { get; init; }
    public int BodyOffset { get; init; } = 12;
}

public sealed class BinarySnapshotRecord
{
    public uint Timestamp { get; init; }
    public int PayloadSize { get; init; }
    public PacketTimelineSource Source { get; init; }
    public BinarySnapshotHeader Header { get; init; } = new();
    public byte[] Body { get; init; } = Array.Empty<byte>();
    public IReadOnlyList<BinarySnapshotChunk> Chunks { get; init; } = Array.Empty<BinarySnapshotChunk>();
    public IReadOnlyList<TimestampPacketRecord> EmbeddedPackets { get; init; } = Array.Empty<TimestampPacketRecord>();
    public Protocol.Lt.LtWorldPropsDecoded? WorldProps { get; init; }
}

public static class BinarySnapshotReader
{
    public const int HeaderSize = 12;

    public static IReadOnlyList<BinarySnapshotRecord> ExtractFromTimeline(
        IReadOnlyList<TimestampPacketRecord> timeline)
    {
        if (timeline.Count == 0)
            return Array.Empty<BinarySnapshotRecord>();

        var snapshots = new List<BinarySnapshotRecord>();
        foreach (var packet in timeline)
        {
            if (packet.PayloadKind != TimestampPayloadKind.BinarySnapshot)
                continue;

            if (TryParse(packet, out var snapshot))
                snapshots.Add(snapshot);
        }

        return snapshots;
    }

    public static bool TryParse(TimestampPacketRecord packet, out BinarySnapshotRecord snapshot)
    {
        snapshot = null!;
        var payload = packet.Payload;
        if (payload.Length == 0 && packet.PayloadSize > 0)
            return false;

        if (payload.Length < HeaderSize && packet.PayloadSize < HeaderSize)
            return false;

        var header = ReadHeader(payload);
        var body = payload.Length > HeaderSize
            ? payload[HeaderSize..].ToArray()
            : Array.Empty<byte>();

        Protocol.Lt.LtMessageReader.TryDecodeWorldProps(payload, out var worldProps);

        var bodySnapshot = new BinarySnapshotRecord
        {
            Timestamp = packet.Timestamp,
            PayloadSize = Math.Max(payload.Length, (int)packet.PayloadSize),
            Source = packet.Source ?? PacketTimelineSource.MiddleBlob,
            Header = header,
            Body = body,
            WorldProps = worldProps,
        };

        var embedded = BinarySnapshotBodyExpander.Expand(bodySnapshot);
        var chunks = BinarySnapshotBodyExpander.AnnotateChunks(
            embedded,
            BinarySnapshotBodyReader.SplitChunks(bodySnapshot));

        snapshot = new BinarySnapshotRecord
        {
            Timestamp = bodySnapshot.Timestamp,
            PayloadSize = bodySnapshot.PayloadSize,
            Source = bodySnapshot.Source,
            Header = bodySnapshot.Header,
            Body = bodySnapshot.Body,
            WorldProps = bodySnapshot.WorldProps,
            EmbeddedPackets = embedded,
            Chunks = chunks,
        };
        return true;
    }

    internal static BinarySnapshotHeader ReadHeader(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < HeaderSize)
            return new BinarySnapshotHeader();

        return new BinarySnapshotHeader
        {
            Field0 = ReadUInt32(payload, 0),
            Field1 = ReadUInt32(payload, 4),
            Field2 = ReadUInt32(payload, 8),
        };
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) =>
        data[offset]
        | ((uint)data[offset + 1] << 8)
        | ((uint)data[offset + 2] << 16)
        | ((uint)data[offset + 3] << 24);
}
