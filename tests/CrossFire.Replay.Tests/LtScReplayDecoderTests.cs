using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Lt;
using CrossFire.Replay.Tests.Support;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class LtScReplayDecoderTests
{
    [Fact]
    public void IngameItemDropped_DecodesFixtureSample()
    {
        var hex = "7500B2250D32000A0000000000000000490000AF4A00007B00000010000000";
        var payload = Convert.FromHexString(hex);
        var decoded = Assert.IsType<LtScIngameItemDroppedDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(0x320D25B2u, decoded.Timestamp);
        Assert.Equal(0x0A, decoded.DropKind);
        Assert.Equal(0x49, decoded.ItemId);
        Assert.Equal(123, decoded.FieldA);
        Assert.Equal(0x10, decoded.FieldB);
    }

    [Fact]
    public void DefenceTowerFire_DecodesFixtureSample()
    {
        var hex = "7A03B2250DD8040C14006400BA4800004000000008000000";
        var payload = Convert.FromHexString(hex);
        var decoded = Assert.IsType<LtScDefenceTowerFireDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(0xD80D25B2u, decoded.Timestamp);
        Assert.Equal(0x0C04, decoded.FieldA);
        Assert.Equal(0x0014, decoded.FieldB);
        Assert.Equal(100, decoded.TowerIndex);
        Assert.Equal(8u, decoded.State);
    }

    [Fact]
    public void BombSitesPeek_RejectsZeroCountOnLargePayload()
    {
        var payload = new byte[64];
        Assert.False(LtMessageReader.TryPeekMessageId(payload, out _));
    }

    [Fact]
    public void UserFixture_DecodesTopUnknownIds()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var ps = (PacketSimulatorReplayDocument)Core.ReplayService.Default.Read(path);
        var report = IltDecodeCoverage.Analyze(ps);

        var itemDropped = report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScIngameItemDropped);
        Assert.NotNull(itemDropped);
        Assert.True(itemDropped!.Semantic >= itemDropped.Unknown, $"item dropped semantic={itemDropped.Semantic} unknown={itemDropped.Unknown}");

        var towerFire = report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScAi2ModeDefenceTowerFire);
        Assert.NotNull(towerFire);
        Assert.Equal(0, towerFire!.Unknown);
    }
}
