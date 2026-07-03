using CrossFire.Replay.Formats.PacketSimulator;

namespace CrossFire.Replay.Protocol.Lt;

public sealed class IltMessageIdCoverage
{
    public required EMessageId MessageId { get; init; }
    public required string Name { get; init; }
    public int Total { get; init; }
    public int Semantic { get; init; }
    public int Unknown { get; init; }
    public int NoDecode { get; init; }

    public bool InCatalog => EMessageIdCatalog.IsKnown((ushort)MessageId);
}

public sealed class IltDecodeCoverageReport
{
    public int TotalPackets { get; init; }
    public int WithMessageId { get; init; }
    public int SemanticallyDecoded { get; init; }
    public int UnknownDecoded { get; init; }
    public int Undecoded { get; init; }
    public int DistinctMessageIds { get; init; }
    public int DistinctSemanticIds { get; init; }
    public int DistinctUnknownIds { get; init; }
    public IReadOnlyList<IltMessageIdCoverage> ByMessageId { get; init; } = Array.Empty<IltMessageIdCoverage>();

    public double SemanticPacketRatio =>
        WithMessageId == 0 ? 0 : (double)SemanticallyDecoded / WithMessageId;

    public double SemanticIdRatio =>
        DistinctMessageIds == 0 ? 0 : (double)DistinctSemanticIds / DistinctMessageIds;
}

public static class IltDecodeCoverage
{
    public static bool IsSemanticDecode(LtDecodedMessage? decoded) =>
        decoded is not null and not LtUnknownDecoded;

    public static IltDecodeCoverageReport Analyze(PacketSimulatorReplayDocument document) =>
        Analyze(CollectPackets(document));

    public static IltDecodeCoverageReport Analyze(IEnumerable<TimestampPacketRecord> packets) =>
        Analyze(packets.Select(p => (p.MessageId, p.Decoded, p.Payload.Length)));

    public static IltDecodeCoverageReport Analyze(
        IEnumerable<(EMessageId? MessageId, LtDecodedMessage? Decoded, int PayloadSize)> packets)
    {
        var stats = new Dictionary<ushort, MutableStat>();
        var total = 0;
        var withId = 0;
        var semantic = 0;
        var unknown = 0;
        var undecoded = 0;

        foreach (var (messageId, decoded, _) in packets)
        {
            total++;
            if (messageId is null)
            {
                undecoded++;
                continue;
            }

            withId++;
            var key = (ushort)messageId.Value;
            if (!stats.TryGetValue(key, out var row))
            {
                row = new MutableStat(messageId.Value);
                stats[key] = row;
            }

            row.Total++;
            if (IsSemanticDecode(decoded))
            {
                semantic++;
                row.Semantic++;
            }
            else if (decoded is LtUnknownDecoded)
            {
                unknown++;
                row.Unknown++;
            }
            else
            {
                row.NoDecode++;
            }
        }

        var byId = stats.Values
            .Select(static s => s.ToCoverage())
            .OrderByDescending(static c => c.Unknown)
            .ThenByDescending(static c => c.Total)
            .ThenBy(static c => c.MessageId)
            .ToArray();

        var semanticIds = byId.Count(c => c.Semantic > 0);
        var unknownIds = byId.Count(c => c.Unknown > 0);

        return new IltDecodeCoverageReport
        {
            TotalPackets = total,
            WithMessageId = withId,
            SemanticallyDecoded = semantic,
            UnknownDecoded = unknown,
            Undecoded = undecoded,
            DistinctMessageIds = byId.Length,
            DistinctSemanticIds = semanticIds,
            DistinctUnknownIds = unknownIds,
            ByMessageId = byId,
        };
    }

    private static IEnumerable<(EMessageId? MessageId, LtDecodedMessage? Decoded, int PayloadSize)> CollectPackets(
        PacketSimulatorReplayDocument document)
    {
        foreach (var packet in document.UnifiedTimeline)
            yield return (packet.MessageId, packet.Decoded, packet.Payload.Length);
    }

    private sealed class MutableStat(EMessageId id)
    {
        public int Total { get; set; }
        public int Semantic { get; set; }
        public int Unknown { get; set; }
        public int NoDecode { get; set; }

        public IltMessageIdCoverage ToCoverage() => new()
        {
            MessageId = id,
            Name = EMessageIdCatalog.GetName((ushort)id),
            Total = Total,
            Semantic = Semantic,
            Unknown = Unknown,
            NoDecode = NoDecode,
        };
    }
}
