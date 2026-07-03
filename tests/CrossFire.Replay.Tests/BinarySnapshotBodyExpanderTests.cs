using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Lt;
using CrossFire.Replay.Tests.Support;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class BinarySnapshotBodyExpanderTests
{
    [Fact]
    public void UserCfn_BinarySnapshot_ExtractsEmbeddedIltPackets()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        if (doc.BinarySnapshots.Count == 0)
            return;

        var snapshot = doc.BinarySnapshots[0];

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
        var folder = ReplayFixturePaths.GetReplayFolder();
        if (folder is null)
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
