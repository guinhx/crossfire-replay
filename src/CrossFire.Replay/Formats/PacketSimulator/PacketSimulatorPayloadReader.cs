using System.Text;
using CrossFire.Replay.Abstractions;
using CrossFire.Replay.IO;

namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Parses PacketSimulator inner payload (legacy and modern layouts).
/// </summary>
public sealed class PacketSimulatorPayloadReader : IReplayPayloadReader
{
    public ReplayFormatKind FormatKind => ReplayFormatKind.PacketSimulator;

    public bool CanRead(ReadOnlySpan<byte> payload) =>
        payload.Length >= 12 && !payload.StartsWith("cfrversion"u8);

    public IReplayDocument Read(ReadOnlySpan<byte> payload, string? sourcePath = null) =>
        Read(payload, ReplayContainerKind.None, sourcePath);

    public PacketSimulatorReplayDocument Read(
        ReadOnlySpan<byte> payload,
        ReplayContainerKind containerKind,
        string? sourcePath = null)
    {
        var innerFormat = DetectInnerFormat(payload);
        if (innerFormat == PacketSimulatorInnerFormat.LegacyV2022)
            return ReadLegacy(payload, containerKind, sourcePath);

        if (innerFormat == PacketSimulatorInnerFormat.ModernV2026)
            return PacketSimulatorModernReader.Read(payload, containerKind, sourcePath);

        return new PacketSimulatorReplayDocument
        {
            SourcePath = sourcePath ?? string.Empty,
            ContainerKind = containerKind,
            InnerFormat = PacketSimulatorInnerFormat.Unknown,
            InnerPayload = payload.ToArray(),
        };
    }

    internal static PacketSimulatorInnerFormat DetectInnerFormat(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 12)
            return PacketSimulatorInnerFormat.Unknown;

        var version = ReadUInt32(payload, 0);
        var magic = ReadUInt32(payload, 4);
        if (version == PacketSimulatorLayout.ContainerVersion
            && magic == PacketSimulatorLayout.ContainerMagic)
        {
            return PacketSimulatorInnerFormat.LegacyV2022;
        }

        if (PacketSimulatorModernReader.IsModern(payload))
            return PacketSimulatorInnerFormat.ModernV2026;

        return PacketSimulatorInnerFormat.Unknown;
    }

    private static PacketSimulatorReplayDocument ReadLegacy(
        ReadOnlySpan<byte> payload,
        ReplayContainerKind containerKind,
        string? sourcePath)
    {
        var cursor = 0;
        var version = ReadUInt32(payload, cursor);
        cursor += 4;
        var magic = ReadUInt32(payload, cursor);
        cursor += 4;
        var headerLen = (int)ReadUInt32(payload, cursor);
        cursor += 4;

        if (headerLen <= 0 || cursor + headerLen > payload.Length)
            throw new ReplayParseException("Invalid PacketSimulator header length.", cursor - 4);

        var headerBlock = payload.Slice(cursor, headerLen);
        cursor += headerLen;

        var header = ParseHeaderBlock(headerBlock);
        if (cursor + 4 > payload.Length)
            throw new ReplayParseException("Unexpected end before spectating room info.", cursor);

        var roomSize = (int)ReadUInt32(payload, cursor);
        cursor += 4;
        var copySize = Math.Min(roomSize, PacketSimulatorLayout.MaxSpectatingRoomInfoSize);
        if (cursor + copySize > payload.Length)
            throw new ReplayParseException("Spectating room info exceeds payload.", cursor);

        var roomInfo = payload.Slice(cursor, copySize).ToArray();
        cursor += roomSize;

        var spectating = ReadTimestampSection(payload, ref cursor);
        var game = ReadTimestampSection(payload, ref cursor);
        var special = ReadSpecialEffectSection(payload, ref cursor);
        var (_, spectatingTagged, gameTagged, unified, deduplicated) =
            PacketSimulatorPacketEnricher.BuildLegacyTimelines(spectating, game);
        var timing = PacketSimulatorTimestampRemapper.InferFromWireTimestamps(
            spectatingTagged.Packets,
            gameTagged.Packets);
        var roomInfoHeader = Protocol.Mm.ProtocolMmRoomInfoReader.TryReadPrefix(roomInfo);
        var modernMetadata = header is not null
            ? new Protocol.Mm.ModernMetadataBlock
            {
                Header = header,
                TailBytes = Array.Empty<byte>(),
                RoomInfoPrefix = roomInfoHeader,
                RoomInfoPrefixOffset = roomInfoHeader is not null ? 0 : null,
                RoomInfoSources = Protocol.Mm.ModernMetadataBlockReader.BuildMetadataRoomInfoSources(
                    header,
                    roomInfoHeader),
            }
            : null;

        return new PacketSimulatorReplayDocument
        {
            SourcePath = sourcePath ?? string.Empty,
            ContainerKind = containerKind,
            InnerFormat = PacketSimulatorInnerFormat.LegacyV2022,
            ContainerVersion = version,
            ContainerMagic = magic,
            InnerPayload = payload.ToArray(),
            Header = header,
            SpectatingRoomInfo = roomInfo,
            SpectatingRoomInfoDeclaredSize = roomSize,
            RoomInfoHeader = roomInfoHeader,
            ModernMetadata = modernMetadata,
            SpectatingPackets = spectatingTagged,
            GamePackets = gameTagged,
            SpecialEffectPackets = special,
            UnifiedTimeline = unified,
            DeduplicatedTimeline = deduplicated,
            ExpandedUnifiedTimeline = unified,
            ExpandedDeduplicatedTimeline = deduplicated,
            TimingState = timing,
        };
    }

    private static PacketSimulatorHeaderBlock ParseHeaderBlock(ReadOnlySpan<byte> block)
    {
        if (block.Length < 12)
            throw new ReplayParseException("PacketSimulator header block is too small.", 0);

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

    private static PacketSection<TimestampPacketRecord> ReadTimestampSection(ReadOnlySpan<byte> payload, ref int cursor)
    {
        var start = cursor;
        if (cursor + 4 > payload.Length)
            throw new ReplayParseException("Unexpected end before packet section.", cursor);

        var count = (int)ReadUInt32(payload, cursor);
        cursor += 4;
        var packets = new List<TimestampPacketRecord>(count);

        for (var i = 0; i < count; i++)
        {
            if (cursor + 8 > payload.Length)
                throw new ReplayParseException("Unexpected end in timestamp packet header.", cursor);

            var size = ReadUInt32(payload, cursor);
            var timestamp = ReadUInt32(payload, cursor + 4);
            cursor += 8;

            if (cursor + size > payload.Length)
                throw new ReplayParseException("Timestamp packet payload exceeds buffer.", cursor);

            packets.Add(new TimestampPacketRecord
            {
                PayloadSize = size,
                Timestamp = timestamp,
                Payload = payload.Slice(cursor, (int)size).ToArray(),
                MessageId = TryReadMessageId(payload.Slice(cursor, (int)size)),
                SourceOffset = cursor - 8,
            });
            cursor += (int)size;
        }

        return new PacketSection<TimestampPacketRecord>
        {
            Packets = packets,
            BytesConsumed = cursor - start,
        };
    }

    private static PacketSection<SpecialEffectPacketRecord> ReadSpecialEffectSection(ReadOnlySpan<byte> payload, ref int cursor)
    {
        var start = cursor;
        if (cursor + 4 > payload.Length)
            return new PacketSection<SpecialEffectPacketRecord> { BytesConsumed = 0 };

        var count = (int)ReadUInt32(payload, cursor);
        cursor += 4;
        var packets = new List<SpecialEffectPacketRecord>(count);

        for (var i = 0; i < count; i++)
        {
            if (cursor + 10 > payload.Length)
                throw new ReplayParseException("Unexpected end in SF packet header.", cursor);

            var size = ReadUInt32(payload, cursor);
            var timestamp = ReadUInt32(payload, cursor + 4);
            var objectId = ReadUInt16(payload, cursor + 8);
            cursor += 10;

            if (cursor + size > payload.Length)
                throw new ReplayParseException("SF packet payload exceeds buffer.", cursor);

            packets.Add(new SpecialEffectPacketRecord
            {
                PayloadSize = size,
                Timestamp = timestamp,
                ObjectId = objectId,
                Payload = payload.Slice(cursor, (int)size).ToArray(),
                MessageId = TryReadMessageId(payload.Slice(cursor, (int)size)),
            });
            cursor += (int)size;
        }

        return new PacketSection<SpecialEffectPacketRecord>
        {
            Packets = packets,
            BytesConsumed = cursor - start,
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

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) =>
        data[offset]
        | ((uint)data[offset + 1] << 8)
        | ((uint)data[offset + 2] << 16)
        | ((uint)data[offset + 3] << 24);

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset) =>
        (ushort)(data[offset] | (data[offset + 1] << 8));

    private static int ReadInt32(ReadOnlySpan<byte> data, int offset) => (int)ReadUInt32(data, offset);

    private static Protocol.Lt.EMessageId? TryReadMessageId(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 2)
            return null;

        try
        {
            return (Protocol.Lt.EMessageId)new Protocol.Lt.LtBitstreamReader(payload).ReadMessageId();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
