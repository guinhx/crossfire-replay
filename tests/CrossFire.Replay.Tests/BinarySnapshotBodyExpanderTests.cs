using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Lt;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class BinarySnapshotBodyExpanderTests
{
    private const string UserCfn = @"D:\Dinho\Documents\Cross Fire\Replay\CFReplay20260701_0000.cfn";

    [Fact]
    public void UserCfn_BinarySnapshot_ExtractsEmbeddedIltPackets()
    {
        if (!File.Exists(UserCfn))
            return;

        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(UserCfn));
        var snapshot = Assert.Single(doc.BinarySnapshots);

        Assert.Equal(65_536u, snapshot.Header.Field0);
        Assert.Equal(65_536u, snapshot.Header.Field1);
        Assert.True(snapshot.EmbeddedPackets.Count >= 20);
        Assert.Equal(snapshot.EmbeddedPackets.Count, doc.BinarySnapshotEmbeddedPackets.Count);
        Assert.All(snapshot.EmbeddedPackets, p => Assert.Equal(PacketTimelineSource.BinarySnapshotBody, p.Source));

        var decodedIds = snapshot.EmbeddedPackets
            .Where(p => p.MessageId.HasValue)
            .Select(p => p.MessageId!.Value)
            .ToHashSet();

        Assert.Contains(EMessageId.MsgScBombSites, decodedIds);
        Assert.True(decodedIds.Count >= 3);
        Assert.True(snapshot.Chunks.Sum(c => c.IltPacketCount) >= snapshot.EmbeddedPackets.Count / 2);
    }

    [Fact]
    public void UserReplayFolder_AllBinarySnapshots_ExposeEmbeddedPackets()
    {
        const string folder = @"D:\Dinho\Documents\Cross Fire\Replay";
        if (!Directory.Exists(folder))
            return;

        var sawSnapshot = false;
        foreach (var file in Directory.GetFiles(folder, "*.cfn"))
        {
            var doc = ReplayService.Default.Read(file);
            if (doc is not PacketSimulatorReplayDocument ps || ps.BinarySnapshots.Count == 0)
                continue;

            sawSnapshot = true;
            Assert.All(ps.BinarySnapshots, s => Assert.NotEmpty(s.EmbeddedPackets));
            Assert.Equal(
                ps.BinarySnapshots.Sum(s => s.EmbeddedPackets.Count),
                ps.BinarySnapshotEmbeddedPackets.Count);
        }

        if (!sawSnapshot)
            return;

        Assert.True(sawSnapshot);
    }
}
