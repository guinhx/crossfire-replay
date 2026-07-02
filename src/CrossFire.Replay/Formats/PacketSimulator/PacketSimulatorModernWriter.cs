using System.Text;
using CrossFire.Replay.Protocol.Mm;

namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Writes modern PacketSimulator inner layout (ModernV2026) observed in Jul/2026 .cfn files.
/// </summary>
public static class PacketSimulatorModernWriter
{
    public static byte[] BuildInnerPayload(PacketSimulatorReplayDocument document)
    {
        if (document.InnerFormat != PacketSimulatorInnerFormat.ModernV2026)
            throw new InvalidOperationException("Document is not ModernV2026.");

        if (document.InnerPayload.Length > 0)
            return document.InnerPayload;

        if (HasRawWireParts(document))
            return RebuildFromParts(document);

        return SynthesizeFromModels(document);
    }

    internal static byte[] RebuildFromParts(PacketSimulatorReplayDocument document) =>
        AssembleInner(
            document,
            PacketSimulatorMiddleBlobWriter.Build(document),
            BuildArchivesFromRaw(document));

    internal static byte[] SynthesizeFromModels(PacketSimulatorReplayDocument document)
    {
        var archives = BuildArchivesFromModels(document);
        var middle = BuildMiddleBlob(document, archives.Spectating.Length, archives.Game.Length, archives.SpecialEffect.Length);
        return AssembleInner(document, middle, archives);
    }

    private static bool HasRawWireParts(PacketSimulatorReplayDocument document) =>
        document.MiddleBlob.Length > 0
        || document.RawArchives.Any(static archive => archive.Data.Length > 0);

    private static byte[] AssembleInner(
        PacketSimulatorReplayDocument document,
        byte[] middle,
        (byte[] Spectating, byte[] Game, byte[] SpecialEffect) archives)
    {
        var metadata = ResolveMetadataBlock(document);
        var extra = document.ExtraBlock.Length > 0
            ? document.ExtraBlock
            : new byte[PacketSimulatorLayout.ModernExtraBlockSize];

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        WriteUInt32(writer, PacketSimulatorLayout.ModernSignature);
        WriteUInt32(writer, (uint)metadata.Length);
        WriteUInt32(writer, (uint)extra.Length);
        writer.Write(metadata);
        writer.Write(extra);
        writer.Write(middle);
        writer.Write(archives.Spectating);
        writer.Write(archives.Game);
        writer.Write(archives.SpecialEffect);

        return stream.ToArray();
    }

    private static byte[] ResolveMetadataBlock(PacketSimulatorReplayDocument document)
    {
        if (document.MetadataBlock.Length >= PacketSimulatorLayout.ModernMetadataBlockSize)
            return document.MetadataBlock;

        return ProtocolMmRoomInfoWriter.WriteMetadataBlock(
            document.MetadataBlock.Length > 0 ? document.MetadataBlock : null,
            document.RoomInfoHeader,
            document.Header,
            PacketSimulatorLayout.ModernMetadataBlockSize);
    }

    private static byte[] BuildMiddleBlob(
        PacketSimulatorReplayDocument document,
        int spectatingArchiveSize,
        int gameArchiveSize,
        int specialEffectArchiveSize)
    {
        if (document.MiddleBlob.Length > 0)
            return PacketSimulatorMiddleBlobWriter.RebuildPreservingLayout(document.MiddleBlob);

        if (document.MiddleBlobLayout.Count > 0)
        {
            return PacketSimulatorMiddleBlobWriter.BuildFromLayout(
                document.MiddleBlobLayout,
                (uint)spectatingArchiveSize,
                (uint)gameArchiveSize,
                (uint)specialEffectArchiveSize);
        }

        return PacketSimulatorMiddleBlobWriter.BuildFromParts(document);
    }

    private static (byte[] Spectating, byte[] Game, byte[] SpecialEffect) BuildArchivesFromRaw(
        PacketSimulatorReplayDocument document)
    {
        if (document.RawArchives.Count == 0)
            return BuildArchivesFromModels(document);

        byte[] spectating = Array.Empty<byte>();
        byte[] game = Array.Empty<byte>();
        byte[] specialEffect = Array.Empty<byte>();

        foreach (var archive in document.RawArchives)
        {
            switch (archive.Name)
            {
                case "spectating":
                    spectating = IltPacketArchiveWriter.RebuildPreservingLayout(archive.Data);
                    break;
                case "game":
                    game = IltPacketArchiveWriter.RebuildPreservingLayout(archive.Data);
                    break;
                case "specialFx":
                    specialEffect = IltPacketArchiveWriter.RebuildSpecialEffectPreservingLayout(archive.Data);
                    break;
            }
        }

        return (spectating, game, specialEffect);
    }

    private static (byte[] Spectating, byte[] Game, byte[] SpecialEffect) BuildArchivesFromModels(
        PacketSimulatorReplayDocument document)
    {
        var spectating = IltPacketArchiveWriter.BuildTimestampArchiveFromLayout(
            ResolveArchiveLayout(document.SpectatingArchiveLayout, document.SpectatingPackets),
            IltPacketArchiveTags.Spectating);

        var game = IltPacketArchiveWriter.BuildTimestampArchiveFromLayout(
            ResolveArchiveLayout(document.GameArchiveLayout, document.GamePackets),
            IltPacketArchiveTags.Game);

        var specialEffect = BuildSpecialEffectArchive(document);
        return (spectating, game, specialEffect);
    }

    private static ModernArchiveLayout? ResolveArchiveLayout(
        ModernArchiveLayout? layout,
        PacketSection<TimestampPacketRecord> fallbackPackets)
    {
        if (layout is not null)
            return layout;

        if (fallbackPackets.Packets.Count == 0)
            return null;

        return new ModernArchiveLayout
        {
            TopLevelPackets = fallbackPackets,
        };
    }

    private static byte[] BuildSpecialEffectArchive(PacketSimulatorReplayDocument document)
    {
        if (document.SpecialEffectArchiveBytes.Length > 0)
            return document.SpecialEffectArchiveBytes;

        if (document.SpecialEffectPackets.Packets.Count > 0)
            return IltPacketArchiveWriter.WriteLegacySpecialEffectArchive(document.SpecialEffectPackets.Packets, document.TimingState);

        if (document.SpecialEffectAssets.Count > 0)
            return IltPacketArchiveWriter.WriteSpecialEffectAssetBlob(document.SpecialEffectAssets);

        return Array.Empty<byte>();
    }

    private static void WriteUInt32(BinaryWriter writer, uint value)
    {
        writer.Write((byte)value);
        writer.Write((byte)(value >> 8));
        writer.Write((byte)(value >> 16));
        writer.Write((byte)(value >> 24));
    }
}
