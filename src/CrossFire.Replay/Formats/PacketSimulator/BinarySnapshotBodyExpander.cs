using CrossFire.Replay.Protocol.Lt;

namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Expands ILT timestamp packets embedded in binary snapshot bodies (middle blob ~300 KB blocks).
/// On-disk layout: 12-byte header (chunk size repeated) + body split into 64 KiB segments with a continuous ILT stream.
/// </summary>
public static class BinarySnapshotBodyExpander
{
    public static IReadOnlyList<TimestampPacketRecord> Expand(BinarySnapshotRecord snapshot)
    {
        if (snapshot.Body.Length == 0)
            return Array.Empty<TimestampPacketRecord>();

        var section = IltPacketArchiveReader.ExtractAllTimestampPackets(snapshot.Body);
        if (section.Packets.Count == 0)
            return Array.Empty<TimestampPacketRecord>();

        var expanded = IltPacketArchiveReader.ExpandNestedPayloads(section.Packets);
        var tagged = new List<TimestampPacketRecord>(expanded.Count);
        foreach (var packet in expanded)
        {
            tagged.Add(new TimestampPacketRecord
            {
                PayloadSize = packet.PayloadSize,
                Timestamp = packet.Timestamp != 0 ? packet.Timestamp : snapshot.Timestamp,
                MessageId = packet.MessageId,
                Decoded = packet.Decoded ?? TryDecode(packet.Payload),
                Payload = packet.Payload,
                Source = PacketTimelineSource.BinarySnapshotBody,
                PayloadKind = TimestampPayloadKind.Standard,
                SourceOffset = packet.SourceOffset,
            });
        }

        return tagged;
    }

    public static IReadOnlyList<BinarySnapshotChunk> AnnotateChunks(
        IReadOnlyList<TimestampPacketRecord> embeddedPackets,
        IReadOnlyList<BinarySnapshotChunk> chunks)
    {
        if (chunks.Count == 0)
            return chunks;

        var annotated = new List<BinarySnapshotChunk>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var count = 0;
            var consumed = 0;
            foreach (var packet in embeddedPackets)
            {
                if (packet.SourceOffset < chunk.Offset)
                    continue;
                if (packet.SourceOffset >= chunk.Offset + chunk.Size)
                    continue;

                count++;
                consumed = Math.Max(consumed, packet.SourceOffset + (int)packet.PayloadSize + 8 - chunk.Offset);
            }

            annotated.Add(new BinarySnapshotChunk
            {
                Offset = chunk.Offset,
                Size = chunk.Size,
                IltPacketCount = count,
                IltBytesConsumed = consumed,
                NonZeroRatio = chunk.NonZeroRatio,
            });
        }

        return annotated;
    }

    private static LtDecodedMessage? TryDecode(ReadOnlySpan<byte> payload)
    {
        try
        {
            return LtMessageReader.TryDecode(payload);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
