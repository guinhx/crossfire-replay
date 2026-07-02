using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class ModernSynthesisTests
{
    private const string ReplayFolder = @"D:\Dinho\Documents\Cross Fire\Replay";

    public static IEnumerable<object[]> UserCfnFiles()
    {
        if (!Directory.Exists(ReplayFolder))
            yield break;

        foreach (var file in Directory.GetFiles(ReplayFolder, "*.cfn").OrderBy(static p => p, StringComparer.OrdinalIgnoreCase))
            yield return new object[] { file };
    }

    [Theory]
    [MemberData(nameof(UserCfnFiles))]
    public void UserCfn_SynthesizeFromLayoutHints_RoundTripsInner(string path)
    {
        var original = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        var synthesized = CreateSynthesisDocument(original);

        var inner = PacketSimulatorModernWriter.BuildInnerPayload(synthesized);
        Assert.Equal(original.InnerPayload, inner);
    }

    [Theory]
    [MemberData(nameof(UserCfnFiles))]
    public void UserCfn_SynthesizeFromLayoutHints_ReadsBackWithSamePacketCounts(string path)
    {
        var original = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        var synthesized = CreateSynthesisDocument(original);
        var inner = PacketSimulatorModernWriter.BuildInnerPayload(synthesized);

        var reader = new PacketSimulatorPayloadReader();
        var originalDecoded = Assert.IsType<PacketSimulatorReplayDocument>(
            reader.Read(inner, original.ContainerKind, path));

        Assert.Equal(original.SpectatingPackets.Packets.Count, originalDecoded.SpectatingPackets.Packets.Count);
        Assert.Equal(original.GamePackets.Packets.Count, originalDecoded.GamePackets.Packets.Count);
        Assert.Equal(original.MiddleBlobPackets.Packets.Count, originalDecoded.MiddleBlobPackets.Packets.Count);
        Assert.Equal(original.SpecialEffectAssets.Count, originalDecoded.SpecialEffectAssets.Count);
    }

    [Fact]
    public void SynthesizeFromModels_BuildsTaggedArchives()
    {
        var packets = new[]
        {
            new TimestampPacketRecord
            {
                PayloadSize = 2,
                Timestamp = 11,
                Payload = new byte[] { 0x00, 0x00 },
            },
        };

        var layout = new ModernArchiveLayout
        {
            TaggedHeader = IltPacketArchiveWriter.CreateDefaultTaggedHeader(IltPacketArchiveTags.Spectating, 1),
            TopLevelPackets = new PacketSection<TimestampPacketRecord> { Packets = packets },
        };

        var archive = IltPacketArchiveWriter.BuildTimestampArchiveFromLayout(layout, IltPacketArchiveTags.Spectating);
        Assert.NotEmpty(archive);
        Assert.Equal(IltPacketArchiveTags.Spectating, BitConverter.ToUInt32(archive, 0));

        var parsed = IltPacketArchiveReader.ParseTimestampArchive(archive, lenient: false);
        Assert.Single(parsed.Packets);
    }

    private static PacketSimulatorReplayDocument CreateSynthesisDocument(PacketSimulatorReplayDocument original) =>
        new()
        {
            SourcePath = original.SourcePath,
            ContainerKind = original.ContainerKind,
            InnerFormat = original.InnerFormat,
            MetadataBlock = original.MetadataBlock,
            ExtraBlock = original.ExtraBlock,
            ModernDescriptor = original.ModernDescriptor,
            MiddleBlobHeader = original.MiddleBlobHeader,
            MiddleBlobStreamOffset = original.MiddleBlobStreamOffset,
            MiddleBlobSegmentCount = original.MiddleBlobSegmentCount,
            MiddleBlobLayout = original.MiddleBlobLayout,
            MiddleBlobSegments = original.MiddleBlobSegments,
            SpectatingArchiveLayout = CloneArchiveLayout(original.SpectatingArchiveLayout),
            GameArchiveLayout = CloneArchiveLayout(original.GameArchiveLayout),
            SpecialEffectArchiveBytes = original.SpecialEffectArchiveBytes,
            SpectatingPackets = original.SpectatingPackets,
            GamePackets = original.GamePackets,
            MiddleBlobPackets = original.MiddleBlobPackets,
            SpecialEffectPackets = original.SpecialEffectPackets,
            SpecialEffectAssets = original.SpecialEffectAssets,
        };

    private static ModernArchiveLayout? CloneArchiveLayout(ModernArchiveLayout? layout)
    {
        if (layout is null)
            return null;

        return new ModernArchiveLayout
        {
            TaggedHeader = layout.TaggedHeader,
            TopLevelPackets = layout.TopLevelPackets,
            WirePieces = layout.WirePieces,
        };
    }
}
