namespace CrossFire.Replay.Formats.PacketSimulator;

internal static class PacketSimulatorModernLayoutBuilder
{
    public static IReadOnlyList<MiddleBlobLayoutPiece> DecomposeMiddle(
        ReadOnlySpan<byte> middle,
        IReadOnlyList<MiddleBlobSegmentRange> segments)
    {
        if (middle.IsEmpty)
            return Array.Empty<MiddleBlobLayoutPiece>();

        if (segments.Count == 0)
            return new[] { new MiddleBlobLayoutPiece { Kind = MiddleBlobLayoutPieceKind.Tail, Data = middle.ToArray() } };

        var pieces = new List<MiddleBlobLayoutPiece>(segments.Count * 2 + 1);
        var writtenEnd = 0;

        foreach (var segment in segments)
        {
            if (segment.StartOffset > writtenEnd)
            {
                pieces.Add(new MiddleBlobLayoutPiece
                {
                    Kind = MiddleBlobLayoutPieceKind.Gap,
                    Data = middle[writtenEnd..segment.StartOffset].ToArray(),
                });
            }

            pieces.Add(new MiddleBlobLayoutPiece
            {
                Kind = MiddleBlobLayoutPieceKind.Segment,
                Data = middle[segment.StartOffset..segment.EndOffset].ToArray(),
            });
            writtenEnd = segment.EndOffset;
        }

        if (writtenEnd < middle.Length)
        {
            pieces.Add(new MiddleBlobLayoutPiece
            {
                Kind = MiddleBlobLayoutPieceKind.Tail,
                Data = middle[writtenEnd..].ToArray(),
            });
        }

        return pieces;
    }

    public static IReadOnlyList<MiddleBlobSegmentData> BuildSegmentData(
        ReadOnlySpan<byte> middle,
        IReadOnlyList<MiddleBlobSegmentRange> segments)
    {
        if (segments.Count == 0)
            return Array.Empty<MiddleBlobSegmentData>();

        var segmentData = new List<MiddleBlobSegmentData>(segments.Count);
        foreach (var segment in segments)
        {
            var section = IltPacketArchiveReader.ParseTimestampStream(middle, segment.StartOffset);
            segmentData.Add(new MiddleBlobSegmentData
            {
                Range = segment,
                Packets = section.Packets,
                RawBytes = middle[segment.StartOffset..segment.EndOffset].ToArray(),
            });
        }

        return segmentData;
    }

    public static ModernArchiveLayout BuildArchiveLayout(ReadOnlySpan<byte> archive)
    {
        if (archive.IsEmpty)
            return new ModernArchiveLayout();

        var taggedHeader = IltPacketArchiveWriter.ExtractTaggedHeader(archive);
        var hasTag = taggedHeader.Tag is IltPacketArchiveTags.Spectating or IltPacketArchiveTags.Game;
        var topLevel = IltPacketArchiveReader.ParseTimestampArchive(archive);

        return new ModernArchiveLayout
        {
            TaggedHeader = hasTag ? taggedHeader : null,
            TopLevelPackets = topLevel,
            WirePieces = DecomposeArchive(archive, topLevel),
            RawBytes = archive.ToArray(),
        };
    }

    public static IReadOnlyList<byte[]> DecomposeArchive(
        ReadOnlySpan<byte> archive,
        PacketSection<TimestampPacketRecord> section)
    {
        if (archive.IsEmpty)
            return Array.Empty<byte[]>();

        if (section.Packets.Count == 0)
            return new[] { archive.ToArray() };

        var (packetStart, _) = IltPacketArchiveReader.ResolveHeaderForTests(archive);
        packetStart = Math.Clamp(packetStart, 0, archive.Length);

        var pieces = new List<byte[]>();
        if (packetStart > 0)
            pieces.Add(archive[..packetStart].ToArray());

        var writtenEnd = packetStart;
        foreach (var packet in section.Packets.OrderBy(static packet => packet.SourceOffset))
        {
            if (packet.SourceOffset < 0)
                return Array.Empty<byte[]>();

            if (packet.SourceOffset > writtenEnd)
                pieces.Add(archive[writtenEnd..packet.SourceOffset].ToArray());

            var packetLength = 8 + packet.Payload.Length;
            if (packet.SourceOffset + packetLength > archive.Length)
                return Array.Empty<byte[]>();

            pieces.Add(archive.Slice(packet.SourceOffset, packetLength).ToArray());
            writtenEnd = packet.SourceOffset + packetLength;
        }

        if (writtenEnd < section.BytesConsumed)
            pieces.Add(archive[writtenEnd..section.BytesConsumed].ToArray());

        if (section.BytesConsumed < archive.Length)
            pieces.Add(archive[section.BytesConsumed..].ToArray());

        return pieces;
    }

    public static byte[] ComposeWirePieces(IReadOnlyList<byte[]> wirePieces)
    {
        if (wirePieces.Count == 0)
            return Array.Empty<byte>();

        if (wirePieces.Count == 1)
            return wirePieces[0];

        using var stream = new MemoryStream();
        foreach (var piece in wirePieces)
            stream.Write(piece);

        return stream.ToArray();
    }

    public static byte[] PatchArchiveSizes(ReadOnlySpan<byte> headerRegion, uint spectatingSize, uint gameSize, uint sfxSize)
    {
        if (headerRegion.IsEmpty)
            return Array.Empty<byte>();

        var buffer = headerRegion.ToArray();
        if (buffer.Length >= 32)
        {
            WriteUInt32(buffer, 20, spectatingSize);
            WriteUInt32(buffer, 24, gameSize);
            WriteUInt32(buffer, 28, sfxSize);
        }

        return buffer;
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
