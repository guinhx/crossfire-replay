using CrossFire.Replay.Compression;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Tests.Support;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class IltArchiveReaderTests
{
    [Fact]
    public void UserCfn_GameArchive_ParsesPackets()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var payload = new ReplayContainerDecoder().Decode(File.ReadAllBytes(path)).Payload;
        var tail = 12 + 1024 + 4;
        var s1 = BitConverter.ToUInt32(payload, tail + 20);
        var s2 = BitConverter.ToUInt32(payload, tail + 24);
        var s3 = BitConverter.ToUInt32(payload, tail + 28);
        var dataStart = payload.Length - (int)(s1 + s2 + s3);
        var game = payload.AsSpan((int)(dataStart + s1), (int)s2);

        var section = IltPacketArchiveReader.ParseTimestampArchive(game);
        Assert.True(section.Packets.Count >= 1);
        Assert.Contains(section.Packets, p => p.MessageId.HasValue);
        Assert.Contains(section.Packets, p => p.Decoded is not null);

        var expanded = IltPacketArchiveReader.ExpandNestedPayloads(section.Packets);
        Assert.True(expanded.Count >= section.Packets.Count);
    }

    [Fact]
    public void UserCfn_MiddleBlob_ParsesTimeline()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var payload = new ReplayContainerDecoder().Decode(File.ReadAllBytes(path)).Payload;
        var tail = 12 + 1024 + 4;
        var s1 = BitConverter.ToUInt32(payload, tail + 20);
        var s2 = BitConverter.ToUInt32(payload, tail + 24);
        var s3 = BitConverter.ToUInt32(payload, tail + 28);
        var dataStart = payload.Length - (int)(s1 + s2 + s3);
        var middle = payload.AsSpan(tail, dataStart - tail);

        var result = PacketSimulatorMiddleBlobReader.Parse(middle);
        Assert.Equal(80, result.StreamOffset);
        Assert.NotNull(result.Header);
        Assert.Equal("BRASILEIRAO", result.Header!.MapLabel);
        Assert.True(result.Timeline.Packets.Count >= 50);
        Assert.True(result.SegmentCount >= 3);
        Assert.Contains(result.Timeline.Packets, p => p.Decoded is not null);
    }
}
