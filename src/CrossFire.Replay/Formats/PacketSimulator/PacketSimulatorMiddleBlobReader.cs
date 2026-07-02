using System.Text;

namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Parses the middle blob between the modern descriptor and EOF raw archives.
/// Layout: header, optional padding, then one or more ILT segments separated by zero padding.
/// </summary>
public static class PacketSimulatorMiddleBlobReader
{
    private const int DefaultStreamOffset = 64;
    private const int ScanStart = 40;
    private const int ScanEnd = 256;
    private const int SegmentSearchWindow = 512 * 1024;
    private const int MaxSegments = 32;

    public static MiddleBlobParseResult Parse(ReadOnlySpan<byte> middle)
    {
        if (middle.IsEmpty)
            return new MiddleBlobParseResult();

        var header = TryParseHeader(middle);
        var streamOffset = FindStreamOffset(middle);
        var segments = EnumerateSegmentRanges(middle, streamOffset);
        var (packets, bytesConsumed) = ParseSegments(middle, segments);
        var expanded = IltPacketArchiveReader.ExpandNestedPayloads(packets);
        var layout = PacketSimulatorModernLayoutBuilder.DecomposeMiddle(middle, segments);
        var gapContainers = ParseGapContainers(middle, layout);
        var coverage = MiddleBlobGapReader.ComputeCoverage(middle, segments, gapContainers);

        return new MiddleBlobParseResult
        {
            Header = header,
            StreamOffset = streamOffset,
            SegmentCount = segments.Count,
            Segments = segments,
            Timeline = new PacketSection<TimestampPacketRecord>
            {
                Packets = expanded,
                BytesConsumed = bytesConsumed,
            },
            GapContainers = gapContainers,
            Coverage = coverage,
        };
    }

    private static IReadOnlyList<MiddleBlobGapContainer> ParseGapContainers(
        ReadOnlySpan<byte> middle,
        IReadOnlyList<MiddleBlobLayoutPiece> layout)
    {
        var containers = new List<MiddleBlobGapContainer>();
        var cursor = 0;

        foreach (var piece in layout)
        {
            if (piece.Kind is not (MiddleBlobLayoutPieceKind.Gap or MiddleBlobLayoutPieceKind.Tail))
            {
                cursor += piece.Data.Length;
                continue;
            }

            if (piece.Data.Length >= 8)
                containers.AddRange(MiddleBlobGapReader.ParseContainers(piece.Data, cursor));

            cursor += piece.Data.Length;
        }

        return containers;
    }

    /// <summary>Identifies ILT segment byte ranges inside the middle blob.</summary>
    public static IReadOnlyList<MiddleBlobSegmentRange> EnumerateSegmentRanges(
        ReadOnlySpan<byte> middle,
        int startOffset)
    {
        if (middle.IsEmpty || startOffset >= middle.Length - 8)
            return Array.Empty<MiddleBlobSegmentRange>();

        var segments = new List<MiddleBlobSegmentRange>();
        var cursor = startOffset;

        while (cursor < middle.Length - 8 && segments.Count < MaxSegments)
        {
            cursor = SkipPadding(middle, cursor);
            if (cursor >= middle.Length - 8)
                break;

            if (!IltPacketArchiveReader.IsStrictTimestampPacketAtForTests(middle, cursor))
            {
                var searchEnd = Math.Min(cursor + SegmentSearchWindow, middle.Length);
                var next = IltPacketArchiveReader.FindNextStrictPacketOffset(middle, cursor, searchEnd);
                if (next < 0)
                    break;

                cursor = next;
            }

            var segmentStart = cursor;
            var section = IltPacketArchiveReader.ParseTimestampStream(middle, cursor);
            if (section.Packets.Count == 0)
            {
                cursor++;
                continue;
            }

            segments.Add(new MiddleBlobSegmentRange
            {
                StartOffset = segmentStart,
                EndOffset = section.BytesConsumed,
                PacketCount = section.Packets.Count,
            });
            cursor = section.BytesConsumed;
        }

        return segments;
    }

    internal static (List<TimestampPacketRecord> Packets, int BytesConsumed) ParseSegments(
        ReadOnlySpan<byte> middle,
        IReadOnlyList<MiddleBlobSegmentRange> segments)
    {
        var packets = new List<TimestampPacketRecord>();
        var lastConsumed = 0;

        foreach (var segment in segments)
        {
            var section = IltPacketArchiveReader.ParseTimestampStream(middle, segment.StartOffset);
            packets.AddRange(section.Packets);
            lastConsumed = Math.Max(lastConsumed, segment.EndOffset);
        }

        return (packets, lastConsumed);
    }

    internal static (List<TimestampPacketRecord> Packets, int SegmentCount, int BytesConsumed) ParseAllSegments(
        ReadOnlySpan<byte> middle,
        int startOffset)
    {
        var segments = EnumerateSegmentRanges(middle, startOffset);
        var (packets, bytesConsumed) = ParseSegments(middle, segments);
        return (packets, segments.Count, bytesConsumed);
    }

    public static int FindStreamOffset(ReadOnlySpan<byte> middle)
    {
        if (middle.Length <= ScanStart + 16)
            return Math.Min(DefaultStreamOffset, Math.Max(0, middle.Length - 1));

        if (DefaultStreamOffset + 9 <= middle.Length)
        {
            var canonical = IltPacketArchiveReader.ParseTimestampStream(middle, DefaultStreamOffset);
            if (canonical.Packets.Count >= IltPacketArchiveReader.MinNestedPacketCount)
                return DefaultStreamOffset;
        }

        var bestStart = DefaultStreamOffset;
        var bestScore = -1;
        var scanEnd = Math.Min(middle.Length - 16, ScanEnd);

        for (var start = ScanStart; start <= scanEnd; start++)
        {
            if (IsInsidePrintableRun(middle, start))
                continue;

            var section = IltPacketArchiveReader.ParseTimestampStream(middle, start);
            var withMessageId = 0;
            foreach (var packet in section.Packets)
            {
                if (packet.MessageId.HasValue)
                    withMessageId++;
            }

            var score = withMessageId * 1000 + section.Packets.Count;
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestStart = start;
        }

        return bestStart;
    }

    private static int SkipPadding(ReadOnlySpan<byte> middle, int cursor)
    {
        while (cursor < middle.Length && middle[cursor] == 0)
            cursor++;

        return cursor;
    }

    private static bool IsInsidePrintableRun(ReadOnlySpan<byte> middle, int offset)
    {
        if (offset < 40 || offset >= middle.Length)
            return false;

        var runStart = 40;
        var runEnd = runStart;
        while (runEnd < middle.Length && middle[runEnd] is >= 0x20 and <= 0x7E)
            runEnd++;

        return offset >= runStart && offset < runEnd;
    }

    internal static MiddleBlobHeader? TryParseHeader(ReadOnlySpan<byte> middle)
    {
        if (middle.Length < 44)
            return null;

        return new MiddleBlobHeader
        {
            Field0 = ReadUInt32(middle, 0),
            Field4 = ReadUInt32(middle, 4),
            Field8 = ReadUInt32(middle, 8),
            Field12 = ReadUInt32(middle, 12),
            MapTypeId = ReadUInt32(middle, 16),
            SpectatingArchiveSize = ReadUInt32(middle, 20),
            GameArchiveSize = ReadUInt32(middle, 24),
            SpecialEffectArchiveSize = ReadUInt32(middle, 28),
            PlayTimeMs = ReadUInt32(middle, 32),
            MapLabel = ReadNullTerminatedAscii(middle, 40, 64),
        };
    }

    private static string ReadNullTerminatedAscii(ReadOnlySpan<byte> data, int offset, int maxLen)
    {
        if (offset >= data.Length)
            return string.Empty;

        var len = Math.Min(maxLen, data.Length - offset);
        var slice = data.Slice(offset, len);
        var zero = slice.IndexOf((byte)0);
        if (zero >= 0)
            slice = slice[..zero];

        return Encoding.ASCII.GetString(slice).TrimEnd('\0', ' ', '0');
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) =>
        data[offset]
        | ((uint)data[offset + 1] << 8)
        | ((uint)data[offset + 2] << 16)
        | ((uint)data[offset + 3] << 24);
}

public sealed class MiddleBlobHeader
{
    public uint Field0 { get; init; }
    public uint Field4 { get; init; }
    public uint Field8 { get; init; }
    public uint Field12 { get; init; }
    public uint MapTypeId { get; init; }
    public uint SpectatingArchiveSize { get; init; }
    public uint GameArchiveSize { get; init; }
    public uint SpecialEffectArchiveSize { get; init; }
    public uint PlayTimeMs { get; init; }
    public string MapLabel { get; init; } = string.Empty;
}

public sealed class MiddleBlobSegmentRange
{
    public int StartOffset { get; init; }
    public int EndOffset { get; init; }
    public int PacketCount { get; init; }
}

public sealed class MiddleBlobParseResult
{
    public MiddleBlobHeader? Header { get; init; }
    public int StreamOffset { get; init; }
    public int SegmentCount { get; init; }
    public IReadOnlyList<MiddleBlobSegmentRange> Segments { get; init; } = Array.Empty<MiddleBlobSegmentRange>();
    public PacketSection<TimestampPacketRecord> Timeline { get; init; } = new();
    public IReadOnlyList<MiddleBlobGapContainer> GapContainers { get; init; } = Array.Empty<MiddleBlobGapContainer>();
    public MiddleBlobCoverage Coverage { get; init; } = new();
}
