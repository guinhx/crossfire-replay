using CrossFire.Replay.Protocol.Lt;

namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Applies LT decode, nested expansion, and timeline tagging to timestamp packet sections.
/// </summary>
public static class PacketSimulatorPacketEnricher
{
    public static PacketSection<TimestampPacketRecord> EnrichSection(
        PacketSection<TimestampPacketRecord> section,
        PacketTimelineSource source)
    {
        if (section.Packets.Count == 0)
            return section;

        var enriched = section.Packets.Select(EnrichPacket).ToList();
        var expanded = IltPacketArchiveReader.ExpandNestedPayloads(enriched);
        var tagged = expanded
            .Select(packet => PacketSimulatorTimelineBuilder.Tag(
                packet,
                source,
                PacketSimulatorTimelineBuilder.ClassifyPayload(packet)))
            .ToList();

        return new PacketSection<TimestampPacketRecord>
        {
            Packets = tagged,
            BytesConsumed = section.BytesConsumed,
        };
    }

    public static (PacketSection<TimestampPacketRecord> Middle, PacketSection<TimestampPacketRecord> Spectating, PacketSection<TimestampPacketRecord> Game, IReadOnlyList<TimestampPacketRecord> Unified, IReadOnlyList<TimestampPacketRecord> Deduplicated) BuildLegacyTimelines(
        PacketSection<TimestampPacketRecord> spectating,
        PacketSection<TimestampPacketRecord> game)
    {
        var emptyMiddle = new PacketSection<TimestampPacketRecord>();
        var spectatingTagged = EnrichSection(spectating, PacketTimelineSource.SpectatingArchive);
        var gameTagged = EnrichSection(game, PacketTimelineSource.GameArchive);
        var unified = PacketSimulatorTimelineBuilder.BuildUnifiedTimeline(emptyMiddle, spectatingTagged, gameTagged);
        var deduplicated = PacketSimulatorTimelineBuilder.BuildDeduplicatedTimeline(unified);
        return (emptyMiddle, spectatingTagged, gameTagged, unified, deduplicated);
    }

    private static TimestampPacketRecord EnrichPacket(TimestampPacketRecord packet)
    {
        EMessageId? messageId = null;
        if (LtMessageReader.TryPeekMessageId(packet.Payload, out var peeked))
            messageId = peeked;

        var decoded = messageId.HasValue ? LtMessageReader.TryDecode(packet.Payload) : null;
        return new TimestampPacketRecord
        {
            PayloadSize = packet.PayloadSize,
            Timestamp = packet.Timestamp,
            MessageId = messageId,
            Decoded = decoded,
            Payload = packet.Payload,
            Source = packet.Source,
            PayloadKind = packet.PayloadKind,
            SourceOffset = packet.SourceOffset,
        };
    }
}
