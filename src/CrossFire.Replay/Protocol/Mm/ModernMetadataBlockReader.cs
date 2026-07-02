using System.Runtime.InteropServices;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Interop;
using CrossFire.Replay.Protocol.Game;

namespace CrossFire.Replay.Protocol.Mm;

/// <summary>
/// Fields of the room-info metadata block immediately after <see cref="ProtoMmRoomInfoPrefix"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct ProtoMmRoomInfoExtension
{
    public ushort FirstHalfResultGr;
    public ushort FirstHalfResultBl;
    public ushort SecondHalfResultGr;
    public ushort SecondHalfResultBl;
    public ushort ExtraFirstHalfResultGr;
    public ushort ExtraFirstHalfResultBl;
    public ushort ExtraSecondHalfResultGr;
    public ushort ExtraSecondHalfResultBl;
    public TourRoomState TourRoomState;
    public byte Round;
    public uint TeamEffectCount;
    public uint VvipUserCount;
    public int LeagueGameRoom;
    public int MatchType;
    public Win32SystemTime CurrentTime;
    public Win32SystemTime StartingTime;

    public static int Size => Marshal.SizeOf<ProtoMmRoomInfoExtension>();
}

/// <summary>
/// Parsed view of the modern 1024-byte replay metadata block.
/// </summary>
public sealed record ModernMetadataBlock
{
    public PacketSimulatorHeaderBlock Header { get; init; } = null!;
    public byte[] TailBytes { get; init; } = Array.Empty<byte>();
    public ProtocolMmRoomInfoHeader? RoomInfoPrefix { get; init; }
    public ProtoMmRoomInfoExtension? RoomInfoExtension { get; init; }
    public int? RoomInfoPrefixOffset { get; init; }

    /// <summary>Non-metadata sources that replace the legacy room-info block in modern .cfn.</summary>
    public IReadOnlyList<ModernReplayRoomInfoSource> RoomInfoSources { get; init; } =
        Array.Empty<ModernReplayRoomInfoSource>();
}

public static class ModernMetadataBlockReader
{
    public static ModernMetadataBlock? TryRead(ReadOnlySpan<byte> block)
    {
        if (block.Length < PacketSimulatorLayout.HeaderBlockSize)
            return null;

        var headerSpan = block[..PacketSimulatorLayout.HeaderBlockSize];
        var header = ParseHeaderBlock(headerSpan);
        var tail = block.Length > PacketSimulatorLayout.HeaderBlockSize
            ? block[PacketSimulatorLayout.HeaderBlockSize..].ToArray()
            : Array.Empty<byte>();

        ProtocolMmRoomInfoHeader? roomPrefix = null;
        ProtoMmRoomInfoExtension? roomExtension = null;
        int? prefixOffset = null;

        if (TryFindRoomInfoPrefix(block, out var offset, out var prefix))
        {
            prefixOffset = offset;
            roomPrefix = prefix;
            var extensionStart = offset + ProtoMmRoomInfoPrefix.Size;
            if (extensionStart + ProtoMmRoomInfoExtension.Size <= block.Length
                && StructReader.TryRead(block[extensionStart..], out ProtoMmRoomInfoExtension extension))
            {
                roomExtension = extension;
            }
        }

        return new ModernMetadataBlock
        {
            Header = header,
            TailBytes = tail,
            RoomInfoPrefix = roomPrefix,
            RoomInfoExtension = roomExtension,
            RoomInfoPrefixOffset = prefixOffset,
            RoomInfoSources = BuildMetadataRoomInfoSources(header, roomPrefix),
        };
    }

    public static IReadOnlyList<ModernReplayRoomInfoSource> MergeRoomInfoSources(
        IReadOnlyList<ModernReplayRoomInfoSource> metadataSources,
        PacketSimulatorModernDescriptor? descriptor,
        MiddleBlobHeader? middleHeader)
    {
        var sources = metadataSources.ToList();
        if (descriptor is { MapLabel.Length: > 0 } or { HostLabel.Length: > 0 })
            sources.Add(ModernReplayRoomInfoSource.ModernDescriptor);
        if (middleHeader is { MapLabel.Length: > 0 } or { MapTypeId: > 0 })
            sources.Add(ModernReplayRoomInfoSource.MiddleBlobHeader);
        return sources;
    }

    public static IReadOnlyList<ModernReplayRoomInfoSource> BuildMetadataRoomInfoSources(
        PacketSimulatorHeaderBlock header,
        ProtocolMmRoomInfoHeader? roomPrefix)
    {
        var sources = new List<ModernReplayRoomInfoSource>(2);
        if (header.MapId != 0 || header.GameGoal != 0 || header.IsClanGame)
            sources.Add(ModernReplayRoomInfoSource.MetadataHeaderBlock);
        if (roomPrefix is not null)
            sources.Add(ModernReplayRoomInfoSource.MetadataTailPrefix);
        return sources;
    }

    private static bool TryFindRoomInfoPrefix(
        ReadOnlySpan<byte> block,
        out int offset,
        out ProtocolMmRoomInfoHeader prefix)
    {
        prefix = null!;
        offset = -1;

        var max = Math.Min(block.Length - ProtoMmRoomInfoPrefix.Size, PacketSimulatorLayout.ModernMetadataBlockSize);
        for (var i = PacketSimulatorLayout.HeaderBlockSize; i <= max; i++)
        {
            var candidate = ProtocolMmRoomInfoReader.TryReadPrefix(block[i..]);
            if (candidate is null || !LooksPlausible(candidate))
                continue;

            offset = i;
            prefix = candidate;
            return true;
        }

        // Legacy synthesis layout may place prefix at 0 under the header block.
        var atZero = ProtocolMmRoomInfoReader.TryReadPrefix(block);
        if (atZero is not null && LooksPlausible(atZero))
        {
            offset = 0;
            prefix = atZero;
            return true;
        }

        return false;
    }

    private static bool LooksPlausible(ProtocolMmRoomInfoHeader header) =>
        header.RoomMaxUser is > 0 and <= 16
        || header.RoomObserverMaxUser is > 0 and <= 8
        || header.MapType is > 0 and < 500
            && header.WinGoal is > 0 and < 1_000_000
            && header.RoomMaxUser is >= 0 and <= 16;

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

        return System.Text.Encoding.ASCII.GetString(slice);
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset) =>
        (ushort)(data[offset] | (data[offset + 1] << 8));

    private static int ReadInt32(ReadOnlySpan<byte> data, int offset) => (int)ReadUInt32(data, offset);

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) =>
        data[offset]
        | ((uint)data[offset + 1] << 8)
        | ((uint)data[offset + 2] << 16)
        | ((uint)data[offset + 3] << 24);
}
