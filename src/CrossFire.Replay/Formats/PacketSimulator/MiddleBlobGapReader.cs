using CrossFire.Replay.Protocol.Lt;

namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Parses u32-length LT payload containers found in middle-blob gap/tail regions.
/// </summary>
public static class MiddleBlobGapReader
{
    private const int MaxContainerPayload = 500_000;
    private const int MaxResyncSteps = 64 * 1024;

    public static IReadOnlyList<MiddleBlobGapContainer> ParseContainers(ReadOnlySpan<byte> region, int regionOffset = 0)
    {
        if (region.IsEmpty)
            return Array.Empty<MiddleBlobGapContainer>();

        var containers = new List<MiddleBlobGapContainer>();
        var cursor = 0;
        var resyncSteps = 0;

        while (cursor + 8 <= region.Length && resyncSteps < MaxResyncSteps)
        {
            cursor = SkipPadding(region, cursor);
            if (cursor + 8 > region.Length)
                break;

            if (!TryReadContainer(region, cursor, regionOffset, out var container))
            {
                cursor++;
                resyncSteps++;
                continue;
            }

            containers.Add(container);
            cursor += 8 + container.PayloadSize;
            resyncSteps = 0;
        }

        return containers;
    }

    public static MiddleBlobCoverage ComputeCoverage(
        ReadOnlySpan<byte> middle,
        IReadOnlyList<MiddleBlobSegmentRange> segments,
        IReadOnlyList<MiddleBlobGapContainer> gapContainers)
    {
        if (middle.IsEmpty)
            return new MiddleBlobCoverage();

        var segmentBytes = segments.Sum(static s => s.EndOffset - s.StartOffset);
        var layout = PacketSimulatorModernLayoutBuilder.DecomposeMiddle(middle, segments);
        var gapBytes = layout.Where(static p => p.Kind == MiddleBlobLayoutPieceKind.Gap).Sum(static p => p.Data.Length);
        var tailBytes = layout.Where(static p => p.Kind == MiddleBlobLayoutPieceKind.Tail).Sum(static p => p.Data.Length);
        var containerBytes = gapContainers.Sum(static c => 8 + c.PayloadSize);
        var accounted = segmentBytes + containerBytes;
        var unparsed = Math.Max(0, middle.Length - accounted);

        return new MiddleBlobCoverage
        {
            TotalBytes = middle.Length,
            SegmentBytes = segmentBytes,
            GapBytes = gapBytes,
            TailBytes = tailBytes,
            GapContainerCount = gapContainers.Count,
            GapContainerBytes = containerBytes,
            UnparsedBytes = unparsed,
        };
    }

    private static bool TryReadContainer(
        ReadOnlySpan<byte> region,
        int cursor,
        int regionOffset,
        out MiddleBlobGapContainer container)
    {
        container = null!;
        var size = ReadUInt32(region, cursor);
        if (size is 0 or > MaxContainerPayload || cursor + 8 + (int)size > region.Length)
            return false;

        var payload = region.Slice(cursor + 8, (int)size);
        if (!LtMessageReader.TryPeekMessageId(payload, out var messageId))
            return false;

        container = new MiddleBlobGapContainer
        {
            Offset = regionOffset + cursor,
            PayloadSize = (int)size,
            MessageId = messageId,
            Payload = payload.ToArray(),
            Decoded = LtMessageReader.TryDecode(payload),
        };
        return true;
    }

    private static int SkipPadding(ReadOnlySpan<byte> region, int cursor)
    {
        while (cursor < region.Length && region[cursor] == 0)
            cursor++;

        return cursor;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) =>
        data[offset]
        | ((uint)data[offset + 1] << 8)
        | ((uint)data[offset + 2] << 16)
        | ((uint)data[offset + 3] << 24);
}

public sealed class MiddleBlobGapContainer
{
    public int Offset { get; init; }
    public int PayloadSize { get; init; }
    public Protocol.Lt.EMessageId MessageId { get; init; }
    public byte[] Payload { get; init; } = Array.Empty<byte>();
    public Protocol.Lt.LtDecodedMessage? Decoded { get; init; }
}

public sealed class MiddleBlobCoverage
{
    public int TotalBytes { get; init; }
    public int SegmentBytes { get; init; }
    public int GapBytes { get; init; }
    public int TailBytes { get; init; }
    public int GapContainerCount { get; init; }
    public int GapContainerBytes { get; init; }
    public int UnparsedBytes { get; init; }

    public double ParsedRatio =>
        TotalBytes > 0 ? (TotalBytes - UnparsedBytes) / (double)TotalBytes : 1.0;
}
