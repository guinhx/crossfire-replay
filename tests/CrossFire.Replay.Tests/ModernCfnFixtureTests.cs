using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Tests.Support;
using Xunit;

public sealed class ModernCfnFixtureTests
{
    [Fact]
    public void UserCfn_ParsesModernInnerLayout()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var doc = ReplayService.Default.Read(path);
        var ps = Assert.IsType<PacketSimulatorReplayDocument>(doc);

        Assert.Equal(PacketSimulatorInnerFormat.ModernV2026, ps.InnerFormat);
        Assert.Equal(ReplayContainerKind.EncryptedBrotliWrapper, ps.ContainerKind);
        Assert.Equal(1024, ps.MetadataBlock.Length);
        Assert.Equal(4, ps.ExtraBlock.Length);

        Assert.NotNull(ps.ModernDescriptor);
        var modern = ps.ModernDescriptor!;
        Assert.Equal("BRASILEIRAO", modern.MapLabel);
        Assert.Equal("Allan.", modern.HostLabel);
        Assert.Equal(206950u, modern.SpectatingArchiveSize);
        Assert.Equal(195456u, modern.GameArchiveSize);
        Assert.Equal(4056u, modern.SpecialEffectArchiveSize);

        Assert.Equal(3, ps.RawArchives.Count);
        Assert.Equal(206950, ps.RawArchives[0].Data.Length);
        Assert.Equal(195456, ps.RawArchives[1].Data.Length);
        Assert.Equal(4056, ps.RawArchives[2].Data.Length);
        Assert.True(ps.GamePackets.Packets.Count >= 1);
        Assert.True(ps.SpectatingPackets.Packets.Count >= 2);
        Assert.Contains(ps.GamePackets.Packets, p => p.Decoded is not null);
        Assert.True(ps.MiddleBlobPackets.Packets.Count >= 50);
        Assert.True(ps.MiddleBlobSegmentCount >= 3);
        Assert.Equal(80, ps.MiddleBlobStreamOffset);
        Assert.NotNull(ps.MiddleBlobHeader);
        Assert.Equal("BRASILEIRAO", ps.MiddleBlobHeader!.MapLabel);
        Assert.Equal(modern.MapTypeId, ps.MiddleBlobHeader.MapTypeId);
        Assert.True(ps.UnifiedTimeline.Count >= 100);
        Assert.Contains(ps.UnifiedTimeline, p => p.Source == PacketTimelineSource.MiddleBlob);
        Assert.Contains(ps.UnifiedTimeline, p => p.Source == PacketTimelineSource.GameArchive);
        Assert.True(ps.UnifiedTimeline.SequenceEqual(
            ps.UnifiedTimeline.OrderBy(p => p.Timestamp).ThenBy(p => p.Source)));
        Assert.True(ps.SpecialEffectAssets.Count >= 1);
        Assert.Contains(ps.SpecialEffectAssets, a => a.AssetPath.Contains(".DTX", StringComparison.OrdinalIgnoreCase));
    }
}
