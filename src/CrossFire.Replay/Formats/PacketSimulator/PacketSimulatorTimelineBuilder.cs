namespace CrossFire.Replay.Formats.PacketSimulator;

public static class PacketSimulatorTimelineBuilder
{
    public const int BinarySnapshotThreshold = 65_536;

    public static IReadOnlyList<TimestampPacketRecord> BuildUnifiedTimeline(
        PacketSection<TimestampPacketRecord> middle,
        PacketSection<TimestampPacketRecord> spectating,
        PacketSection<TimestampPacketRecord> game)
    {
        var capacity = middle.Packets.Count + spectating.Packets.Count + game.Packets.Count;
        if (capacity == 0)
            return Array.Empty<TimestampPacketRecord>();

        var timeline = new List<TimestampPacketRecord>(capacity);
        timeline.AddRange(middle.Packets);
        timeline.AddRange(spectating.Packets);
        timeline.AddRange(game.Packets);

        timeline.Sort(static (left, right) =>
        {
            var byTimestamp = left.Timestamp.CompareTo(right.Timestamp);
            if (byTimestamp != 0)
                return byTimestamp;

            var leftSource = left.Source ?? PacketTimelineSource.MiddleBlob;
            var rightSource = right.Source ?? PacketTimelineSource.MiddleBlob;
            return leftSource.CompareTo(rightSource);
        });

        return timeline;
    }

    public static TimestampPacketRecord Tag(
        TimestampPacketRecord packet,
        PacketTimelineSource source,
        TimestampPayloadKind payloadKind = TimestampPayloadKind.Standard) =>
        new()
        {
            PayloadSize = packet.PayloadSize,
            Timestamp = packet.Timestamp,
            MessageId = packet.MessageId,
            Decoded = packet.Decoded,
            Payload = packet.Payload,
            Source = source,
            PayloadKind = payloadKind,
        };

    public static TimestampPayloadKind ClassifyPayload(TimestampPacketRecord packet)
    {
        var payloadLength = Math.Max(packet.Payload.Length, (int)packet.PayloadSize);
        if (payloadLength >= BinarySnapshotThreshold)
            return TimestampPayloadKind.BinarySnapshot;

        if (payloadLength >= IltPacketArchiveReader.NestedPayloadThreshold)
        {
            var inner = IltPacketArchiveReader.ParseTimestampStream(packet.Payload);
            var withMessageId = inner.Packets.Count(p => p.MessageId.HasValue);
            if (inner.Packets.Count >= IltPacketArchiveReader.MinNestedPacketCount
                && withMessageId >= IltPacketArchiveReader.MinNestedPacketCount)
                return TimestampPayloadKind.NestedArchive;
        }

        return TimestampPayloadKind.Standard;
    }

    public static IReadOnlyList<TimestampPacketRecord> BuildDeduplicatedTimeline(
        IReadOnlyList<TimestampPacketRecord> timeline)
    {
        if (timeline.Count == 0)
            return timeline;

        var seen = new HashSet<TimelinePacketKey>();
        var deduplicated = new List<TimestampPacketRecord>(timeline.Count);
        foreach (var packet in timeline)
        {
            if (!seen.Add(TimelinePacketKey.From(packet)))
                continue;

            deduplicated.Add(packet);
        }

        return deduplicated;
    }

    public static IReadOnlyList<TimestampPacketRecord> MergeEmbeddedSnapshots(
        IReadOnlyList<TimestampPacketRecord> timeline,
        IReadOnlyList<TimestampPacketRecord> embedded)
    {
        if (embedded.Count == 0)
            return timeline;

        var merged = new List<TimestampPacketRecord>(timeline.Count + embedded.Count);
        merged.AddRange(timeline);
        foreach (var packet in embedded)
            merged.Add(Tag(packet, PacketTimelineSource.BinarySnapshotBody));

        merged.Sort(static (left, right) =>
        {
            var byTimestamp = left.Timestamp.CompareTo(right.Timestamp);
            if (byTimestamp != 0)
                return byTimestamp;

            var leftSource = left.Source ?? PacketTimelineSource.MiddleBlob;
            var rightSource = right.Source ?? PacketTimelineSource.MiddleBlob;
            return leftSource.CompareTo(rightSource);
        });

        return merged;
    }

    private readonly record struct TimelinePacketKey(
        uint Timestamp,
        ushort? MessageId,
        PacketTimelineSource Source,
        int PayloadLength,
        ulong PayloadFingerprint)
    {
        public static TimelinePacketKey From(TimestampPacketRecord packet)
        {
            var payload = packet.Payload;
            return new TimelinePacketKey(
                packet.Timestamp,
                packet.MessageId.HasValue ? (ushort)packet.MessageId.Value : null,
                packet.Source ?? PacketTimelineSource.MiddleBlob,
                Math.Max(payload.Length, (int)packet.PayloadSize),
                Fingerprint(payload));
        }

        private static ulong Fingerprint(ReadOnlySpan<byte> payload)
        {
            if (payload.IsEmpty)
                return 0;

            const int sampleLen = 16;
            var len = Math.Min(sampleLen, payload.Length);
            ulong hash = (ulong)payload.Length;
            for (var i = 0; i < len; i++)
                hash = hash * 31 + payload[i];

            return hash;
        }
    }

    public static PacketSection<TimestampPacketRecord> TagSection(
        PacketSection<TimestampPacketRecord> section,
        PacketTimelineSource source)
    {
        if (section.Packets.Count == 0)
            return section;

        var tagged = new List<TimestampPacketRecord>(section.Packets.Count);
        foreach (var packet in section.Packets)
        {
            var kind = ClassifyPayload(packet);
            tagged.Add(Tag(packet, source, kind));
        }

        return new PacketSection<TimestampPacketRecord>
        {
            Packets = tagged,
            BytesConsumed = section.BytesConsumed,
        };
    }
}
