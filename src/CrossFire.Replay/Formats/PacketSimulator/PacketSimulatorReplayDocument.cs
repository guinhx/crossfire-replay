using System.Text;
using CrossFire.Replay.Abstractions;

namespace CrossFire.Replay.Formats.PacketSimulator;

public sealed class PacketSimulatorReplayDocument : IReplayDocument
{
    public ReplayFormatKind FormatKind => ReplayFormatKind.PacketSimulator;
    public string SourcePath { get; init; } = string.Empty;

    public ReplayContainerKind ContainerKind { get; init; }
    public PacketSimulatorInnerFormat InnerFormat { get; init; }
    public uint ContainerVersion { get; init; }
    public uint ContainerMagic { get; init; }
    public byte[] InnerPayload { get; init; } = Array.Empty<byte>();

    public PacketSimulatorHeaderBlock? Header { get; init; }
    public byte[] SpectatingRoomInfo { get; init; } = Array.Empty<byte>();

    /// <summary>Declared size prefix before spectating room bytes (observed max 47963).</summary>
    public int? SpectatingRoomInfoDeclaredSize { get; init; }

    /// <summary>1024-byte metadata block in modern .cfn inner layout.</summary>
    public byte[] MetadataBlock { get; init; } = Array.Empty<byte>();

    public byte[] ExtraBlock { get; init; } = Array.Empty<byte>();
    public byte[] MiddleBlob { get; init; } = Array.Empty<byte>();
    public MiddleBlobHeader? MiddleBlobHeader { get; init; }
    public int MiddleBlobStreamOffset { get; init; }
    public int MiddleBlobSegmentCount { get; init; }
    public PacketSimulatorModernDescriptor? ModernDescriptor { get; init; }
    public PacketSimulatorTimingState? TimingState { get; init; }
    public IReadOnlyList<RawPacketArchive> RawArchives { get; init; } = Array.Empty<RawPacketArchive>();

    /// <summary>Decomposed middle-blob layout for synthesis without raw middle bytes.</summary>
    public IReadOnlyList<MiddleBlobLayoutPiece> MiddleBlobLayout { get; init; } = Array.Empty<MiddleBlobLayoutPiece>();
    public IReadOnlyList<MiddleBlobSegmentData> MiddleBlobSegments { get; init; } = Array.Empty<MiddleBlobSegmentData>();

    public IReadOnlyList<MiddleBlobGapContainer> MiddleBlobGapContainers { get; init; } = Array.Empty<MiddleBlobGapContainer>();
    public MiddleBlobCoverage MiddleBlobCoverage { get; init; } = new();

    public ModernArchiveLayout? SpectatingArchiveLayout { get; init; }
    public ModernArchiveLayout? GameArchiveLayout { get; init; }
    public byte[] SpecialEffectArchiveBytes { get; init; } = Array.Empty<byte>();

    public Protocol.Mm.ProtocolMmRoomInfoHeader? RoomInfoHeader { get; init; }

    /// <summary>Parsed 1024-byte modern metadata (header + tail extension).</summary>
    public Protocol.Mm.ModernMetadataBlock? ModernMetadata { get; init; }

    public PacketSection<TimestampPacketRecord> SpectatingPackets { get; init; } = new();
    public PacketSection<TimestampPacketRecord> GamePackets { get; init; } = new();
    public PacketSection<TimestampPacketRecord> MiddleBlobPackets { get; init; } = new();
    public PacketSection<SpecialEffectPacketRecord> SpecialEffectPackets { get; init; } = new();
    public IReadOnlyList<SpecialEffectAssetRecord> SpecialEffectAssets { get; init; } = Array.Empty<SpecialEffectAssetRecord>();
    public IReadOnlyList<TimestampPacketRecord> UnifiedTimeline { get; init; } = Array.Empty<TimestampPacketRecord>();
    public IReadOnlyList<TimestampPacketRecord> DeduplicatedTimeline { get; init; } = Array.Empty<TimestampPacketRecord>();
    public IReadOnlyList<BinarySnapshotRecord> BinarySnapshots { get; init; } = Array.Empty<BinarySnapshotRecord>();

    /// <summary>ILT packets extracted from <see cref="BinarySnapshots"/> bodies.</summary>
    public IReadOnlyList<TimestampPacketRecord> BinarySnapshotEmbeddedPackets { get; init; } = Array.Empty<TimestampPacketRecord>();

    public ModernExtraBlock? ParsedExtraBlock { get; init; }

    /// <summary>Unified timeline with <see cref="BinarySnapshotEmbeddedPackets"/> merged in.</summary>
    public IReadOnlyList<TimestampPacketRecord> ExpandedUnifiedTimeline { get; init; } = Array.Empty<TimestampPacketRecord>();

    public IReadOnlyList<TimestampPacketRecord> ExpandedDeduplicatedTimeline { get; init; } = Array.Empty<TimestampPacketRecord>();
}
