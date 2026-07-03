using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Tests.Support;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class MiddleBlobWriterTests
{
    public static IEnumerable<object[]> UserCfnFiles() => ReplayFixturePaths.CfnFileTheoryData();

    [Theory]
    [MemberData(nameof(UserCfnFiles))]
    public void UserCfn_MiddleBlob_RebuildPreservingLayout_RoundTrips(string path)
    {
        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        Assert.NotEmpty(doc.MiddleBlob);

        var rebuilt = PacketSimulatorMiddleBlobWriter.RebuildPreservingLayout(doc.MiddleBlob);
        Assert.Equal(doc.MiddleBlob, rebuilt);
    }

    [Theory]
    [MemberData(nameof(UserCfnFiles))]
    public void UserCfn_MiddleBlob_SegmentEnumeration_MatchesReader(string path)
    {
        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        var parse = PacketSimulatorMiddleBlobReader.Parse(doc.MiddleBlob);
        var segments = PacketSimulatorMiddleBlobReader.EnumerateSegmentRanges(doc.MiddleBlob, parse.StreamOffset);

        Assert.Equal(parse.SegmentCount, segments.Count);
        Assert.Equal(parse.Segments.Count, segments.Count);
        Assert.All(segments, segment => Assert.True(segment.EndOffset > segment.StartOffset));
        Assert.All(segments, segment => Assert.True(segment.PacketCount > 0));
    }

    [Fact]
    public void UserFixture_MiddleBlob_HasExpectedSegmentLayout()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        var parse = PacketSimulatorMiddleBlobReader.Parse(doc.MiddleBlob);

        Assert.Equal(80, parse.StreamOffset);
        Assert.True(parse.SegmentCount >= 3);
        Assert.Equal("BRASILEIRAO", parse.Header!.MapLabel);
        Assert.True(parse.Timeline.Packets.Count >= 50);
    }

    [Theory]
    [MemberData(nameof(UserCfnFiles))]
    public void UserCfn_FullModernInner_RebuildWithoutInnerPayload_RoundTrips(string path)
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
            MiddleBlobHeader = original.MiddleBlobHeader,
            MiddleBlobStreamOffset = original.MiddleBlobStreamOffset,
            RawArchives = original.RawArchives,
        };

        var inner = PacketSimulatorModernWriter.BuildInnerPayload(rebuiltDoc);
        Assert.Equal(original.InnerPayload, inner);
    }
}
