using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class IltArchiveWriterTests
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
    public void UserCfn_EofArchives_RebuildPreservingLayout_RoundTrips(string path)
    {
        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        Assert.Equal(3, doc.RawArchives.Count);

        foreach (var archive in doc.RawArchives)
        {
            var rebuilt = archive.Name switch
            {
                "specialFx" => IltPacketArchiveWriter.RebuildSpecialEffectPreservingLayout(archive.Data),
                _ => IltPacketArchiveWriter.RebuildPreservingLayout(archive.Data),
            };

            Assert.Equal(archive.Data, rebuilt);
        }
    }

    [Theory]
    [MemberData(nameof(UserCfnFiles))]
    public void UserCfn_ModernInner_RebuildFromPartsWithoutPassthrough_RoundTrips(string path)
    {
        var original = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        var rebuiltDoc = new PacketSimulatorReplayDocument
        {
            SourcePath = original.SourcePath,
            ContainerKind = original.ContainerKind,
            InnerFormat = original.InnerFormat,
            MetadataBlock = original.MetadataBlock,
            ExtraBlock = original.ExtraBlock,
            ModernDescriptor = original.ModernDescriptor,
            MiddleBlob = original.MiddleBlob,
            RawArchives = original.RawArchives,
        };

        var inner = PacketSimulatorModernWriter.BuildInnerPayload(rebuiltDoc);
        Assert.Equal(original.InnerPayload, inner);
    }

    [Fact]
    public void LegacyTimestampArchive_WriteThenRead_RoundTrips()
    {
        var packets = new[]
        {
            new TimestampPacketRecord
            {
                PayloadSize = 3,
                Timestamp = 42,
                Payload = new byte[] { 0x01, 0x02, 0x03 },
            },
            new TimestampPacketRecord
            {
                PayloadSize = 2,
                Timestamp = 99,
                Payload = new byte[] { 0xAA, 0xBB },
            },
        };

        var archive = IltPacketArchiveWriter.WriteLegacyTimestampArchive(packets);
        var parsed = IltPacketArchiveReader.ParseTimestampArchive(archive, lenient: false);

        Assert.Equal(2, parsed.Packets.Count);
        Assert.Equal(packets[0].Timestamp, parsed.Packets[0].Timestamp);
        Assert.Equal(packets[1].Payload, parsed.Packets[1].Payload);
        Assert.Equal(archive, IltPacketArchiveWriter.RebuildPreservingLayout(archive));
    }

    [Fact]
    public void TaggedTimestampArchive_WriteThenRead_PreservesPackets()
    {
        var packets = new[]
        {
            new TimestampPacketRecord
            {
                PayloadSize = 6,
                Timestamp = 7,
                Payload = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
            },
        };

        var archive = IltPacketArchiveWriter.WriteTaggedTimestampArchive(
            new IltPacketArchiveWriter.TaggedHeaderOptions
            {
                Tag = IltPacketArchiveTags.Spectating,
                Count = 1,
                PrefixAfterCount = new byte[] { 0x2F, 0x00, 0x00 },
            },
            packets);

        var parsed = IltPacketArchiveReader.ParseTimestampArchive(archive, lenient: false);
        Assert.Single(parsed.Packets);
        Assert.Equal(packets[0].Payload, parsed.Packets[0].Payload);
        Assert.Equal(archive, IltPacketArchiveWriter.RebuildPreservingLayout(archive));
    }
}
