using System.Text;
using CrossFire.Replay.Abstractions;

namespace CrossFire.Replay.Formats.PacketSimulator;

internal static class PacketSimulatorModernReader
{
    public static bool IsModern(ReadOnlySpan<byte> payload) =>
        payload.Length >= 12 && ReadUInt32(payload, 0) == PacketSimulatorLayout.ModernSignature;

    public static PacketSimulatorReplayDocument Read(
        ReadOnlySpan<byte> payload,
        ReplayContainerKind containerKind,
        string? sourcePath)
    {
        var metadataSize = (int)ReadUInt32(payload, 4);
        var extraSize = (int)ReadUInt32(payload, 8);
        if (metadataSize <= 0 || extraSize < 0 || payload.Length < 12 + metadataSize + extraSize)
            throw new ReplayParseException("Invalid modern PacketSimulator metadata sizes.", 4);

        var cursor = 12;
        var metadata = payload.Slice(cursor, metadataSize).ToArray();
        cursor += metadataSize;

        var extra = extraSize > 0
            ? payload.Slice(cursor, extraSize).ToArray()
            : Array.Empty<byte>();
        cursor += extraSize;

        var descriptor = ParseDescriptor(payload, cursor);
        var archiveTotal = (int)(descriptor.SpectatingArchiveSize
                                 + descriptor.GameArchiveSize
                                 + descriptor.SpecialEffectArchiveSize);
        if (archiveTotal <= 0 || archiveTotal > payload.Length)
            throw new ReplayParseException("Invalid modern PacketSimulator archive sizes.", cursor + 20);

        var archiveStart = payload.Length - archiveTotal;
        if (archiveStart <= cursor)
            throw new ReplayParseException("Modern PacketSimulator archive layout underflow.", archiveStart);

        var middle = payload.Slice(cursor, archiveStart - cursor).ToArray();
        var archiveCursor = archiveStart;
        var spectatingArchive = SliceArchive(payload, ref archiveCursor, descriptor.SpectatingArchiveSize, "spectating");
        var gameArchive = SliceArchive(payload, ref archiveCursor, descriptor.GameArchiveSize, "game");
        var sfxArchive = SliceArchive(payload, ref archiveCursor, descriptor.SpecialEffectArchiveSize, "specialFx");

        var roomInfo = Protocol.Mm.ProtocolMmRoomInfoReader.TryReadPrefix(metadata);
        var modernMetadata = Protocol.Mm.ModernMetadataBlockReader.TryRead(metadata);
        PacketSimulatorHeaderBlock? header = modernMetadata?.Header
            ?? (metadata.Length >= PacketSimulatorLayout.HeaderBlockSize
                ? ParseHeaderBlock(metadata[..PacketSimulatorLayout.HeaderBlockSize])
                : null);
        if (modernMetadata?.RoomInfoPrefix is { } locatedRoom)
            roomInfo = locatedRoom;

        var spectatingRaw = PacketSimulatorTimelineBuilder.TagSection(
            ParseArchiveSection(spectatingArchive.Data),
            PacketTimelineSource.SpectatingArchive);
        var gameRaw = PacketSimulatorTimelineBuilder.TagSection(
            ParseArchiveSection(gameArchive.Data),
            PacketTimelineSource.GameArchive);
        var special = IltPacketArchiveReader.ParseSpecialEffectArchive(sfxArchive.Data);
        var sfxAssets = SpecialEffectAssetParser.Parse(sfxArchive.Data);
        var middleBlob = PacketSimulatorMiddleBlobReader.Parse(middle);
        if (modernMetadata is not null)
        {
            modernMetadata = modernMetadata with
            {
                RoomInfoSources = Protocol.Mm.ModernMetadataBlockReader.MergeRoomInfoSources(
                    modernMetadata.RoomInfoSources,
                    descriptor,
                    middleBlob.Header),
            };
        }
        var middleLayout = PacketSimulatorModernLayoutBuilder.DecomposeMiddle(middle, middleBlob.Segments);
        var middleSegments = PacketSimulatorModernLayoutBuilder.BuildSegmentData(middle, middleBlob.Segments);
        var spectatingLayout = PacketSimulatorModernLayoutBuilder.BuildArchiveLayout(spectatingArchive.Data);
        var gameLayout = PacketSimulatorModernLayoutBuilder.BuildArchiveLayout(gameArchive.Data);
        var middleTagged = PacketSimulatorTimelineBuilder.TagSection(
            middleBlob.Timeline,
            PacketTimelineSource.MiddleBlob);
        var unifiedTimeline = PacketSimulatorTimelineBuilder.BuildUnifiedTimeline(
            middleTagged,
            spectatingRaw,
            gameRaw);
        var deduplicatedTimeline = PacketSimulatorTimelineBuilder.BuildDeduplicatedTimeline(unifiedTimeline);
        var binarySnapshots = BinarySnapshotReader.ExtractFromTimeline(unifiedTimeline);
        var binarySnapshotEmbedded = binarySnapshots
            .SelectMany(static s => s.EmbeddedPackets)
            .ToArray();
        var expandedUnifiedTimeline = PacketSimulatorTimelineBuilder.MergeEmbeddedSnapshots(
            unifiedTimeline,
            binarySnapshotEmbedded);
        var expandedDeduplicatedTimeline = PacketSimulatorTimelineBuilder.BuildDeduplicatedTimeline(
            expandedUnifiedTimeline);
        var parsedExtra = ModernExtraBlockReader.TryRead(extra);
        var timing = PacketSimulatorTimestampRemapper.InferFromWireTimestamps(
            spectatingRaw.Packets,
            gameRaw.Packets);

        return new PacketSimulatorReplayDocument
        {
            SourcePath = sourcePath ?? string.Empty,
            ContainerKind = containerKind,
            InnerFormat = PacketSimulatorInnerFormat.ModernV2026,
            InnerPayload = payload.ToArray(),
            MetadataBlock = metadata,
            ExtraBlock = extra,
            ParsedExtraBlock = parsedExtra,
            ModernDescriptor = descriptor,
            MiddleBlob = middle,
            MiddleBlobHeader = middleBlob.Header,
            MiddleBlobStreamOffset = middleBlob.StreamOffset,
            MiddleBlobSegmentCount = middleBlob.SegmentCount,
            MiddleBlobLayout = middleLayout,
            MiddleBlobSegments = middleSegments,
            MiddleBlobGapContainers = middleBlob.GapContainers,
            MiddleBlobCoverage = middleBlob.Coverage,
            SpectatingArchiveLayout = spectatingLayout,
            GameArchiveLayout = gameLayout,
            SpecialEffectArchiveBytes = sfxArchive.Data,
            RoomInfoHeader = roomInfo,
            ModernMetadata = modernMetadata,
            Header = header,
            RawArchives =
            [
                spectatingArchive,
                gameArchive,
                sfxArchive,
            ],
            SpectatingPackets = spectatingRaw,
            GamePackets = gameRaw,
            MiddleBlobPackets = middleTagged,
            SpecialEffectPackets = special,
            SpecialEffectAssets = sfxAssets,
            UnifiedTimeline = unifiedTimeline,
            DeduplicatedTimeline = deduplicatedTimeline,
            ExpandedUnifiedTimeline = expandedUnifiedTimeline,
            ExpandedDeduplicatedTimeline = expandedDeduplicatedTimeline,
            BinarySnapshots = binarySnapshots,
            BinarySnapshotEmbeddedPackets = binarySnapshotEmbedded,
            TimingState = timing,
        };
    }

    private static PacketSimulatorHeaderBlock ParseHeaderBlock(ReadOnlySpan<byte> block)
    {
        var isClan = block.Length > 10 && block[10] != 0;
        return new PacketSimulatorHeaderBlock
        {
            DeathMatchTypeRaw = ReadUInt32(block, 0),
            GameGoal = ReadUInt32(block, 4),
            MapId = (short)ReadUInt16(block, 8),
            IsClanGame = isClan,
            ClanNameGr = ReadFixedString(block, 11, 36),
            ClanNameBl = ReadFixedString(block, 47, 36),
            IsClanHalfTime = block.Length > 83 && block[83] != 0,
            HalfTimeScoreGr = block.Length >= 88 ? ReadInt32(block, 84) : 0,
            HalfTimeScoreBl = block.Length >= 92 ? ReadInt32(block, 88) : 0,
            RawBlock = block.ToArray(),
        };
    }

    private static string ReadFixedString(ReadOnlySpan<byte> data, int offset, int maxLen)
    {
        if (offset >= data.Length)
            return string.Empty;

        var len = Math.Min(maxLen, data.Length - offset);
        var slice = data.Slice(offset, len);
        var zero = slice.IndexOf((byte)0);
        if (zero >= 0)
            slice = slice[..zero];

        return Encoding.ASCII.GetString(slice);
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset) =>
        (ushort)(data[offset] | (data[offset + 1] << 8));

    private static int ReadInt32(ReadOnlySpan<byte> data, int offset) => (int)ReadUInt32(data, offset);

    private static PacketSimulatorModernDescriptor ParseDescriptor(ReadOnlySpan<byte> payload, int offset)
    {
        if (offset + 40 > payload.Length)
            throw new ReplayParseException("Modern PacketSimulator descriptor truncated.", offset);

        var mapLabel = string.Empty;
        var hostLabel = string.Empty;
        ReadLabels(payload, offset, ref mapLabel, ref hostLabel);

        return new PacketSimulatorModernDescriptor
        {
            Flags = ReadUInt32(payload, offset + 4),
            FieldA = ReadUInt32(payload, offset + 8),
            FieldB = ReadUInt32(payload, offset + 12),
            MapTypeId = ReadUInt32(payload, offset + 16),
            SpectatingArchiveSize = ReadUInt32(payload, offset + 20),
            GameArchiveSize = ReadUInt32(payload, offset + 24),
            SpecialEffectArchiveSize = ReadUInt32(payload, offset + 28),
            UnknownWord = ReadUInt32(payload, offset + 32),
            MapLabel = mapLabel,
            HostLabel = hostLabel,
        };
    }

    private static void ReadLabels(ReadOnlySpan<byte> payload, int offset, ref string mapLabel, ref string hostLabel)
    {
        var cursor = offset + 40;
        var end = Math.Min(payload.Length, offset + 256);
        var labels = new List<string>(2);
        while (cursor < end && labels.Count < 2)
        {
            while (cursor < end && payload[cursor] == 0)
                cursor++;

            if (cursor >= end)
                break;

            var start = cursor;
            while (cursor < end && payload[cursor] != 0)
                cursor++;

            if (cursor > start)
                labels.Add(Encoding.ASCII.GetString(payload.Slice(start, cursor - start)));
        }

        if (labels.Count > 0)
            mapLabel = labels[0].TrimEnd('0', ' ');
        if (labels.Count > 1)
            hostLabel = labels[1];
    }

    private static RawPacketArchive SliceArchive(ReadOnlySpan<byte> payload, ref int cursor, uint size, string name)
    {
        if (size == 0)
            return new RawPacketArchive { Name = name, Data = Array.Empty<byte>() };

        if (cursor + (int)size > payload.Length)
            throw new ReplayParseException($"Modern archive '{name}' exceeds payload.", cursor);

        var data = payload.Slice(cursor, (int)size).ToArray();
        cursor += (int)size;
        return new RawPacketArchive { Name = name, Data = data };
    }

    private static PacketSection<TimestampPacketRecord> ParseArchiveSection(ReadOnlySpan<byte> archive)
    {
        var section = IltPacketArchiveReader.ParseTimestampArchive(archive);
        return new PacketSection<TimestampPacketRecord>
        {
            Packets = IltPacketArchiveReader.ExpandNestedPayloads(section.Packets),
            BytesConsumed = section.BytesConsumed,
        };
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) =>
        data[offset]
        | ((uint)data[offset + 1] << 8)
        | ((uint)data[offset + 2] << 16)
        | ((uint)data[offset + 3] << 24);
}
