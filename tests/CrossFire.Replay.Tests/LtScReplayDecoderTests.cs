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
    public void SetWeaponSlot_DecodesCompactBundledPayload()
    {
        var hex = "10000000E517B3250D3600066071CCF21A4C050000FCE90064000000600000000C000000E417B3250D38002500090000F833000065000000600000000C000000";
        var payload = Convert.FromHexString(hex);
        var decoded = Assert.IsType<LtSetWeaponSlotDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(0, decoded.SelectedSlotIndex);
        Assert.Empty(decoded.CustomSetInfo[0]);
        Assert.True(decoded.HasExtendedTrailingData);
    }

    [Fact]
    public void BossRevive_DecodesConcatenatedFixtureSample()
    {
        var hex = "0C03B2250D3800F3010A0000F8C310001E490000600000000C0000000C03B2250D3800F3010A0000F84310001F490000710000000F000000";
        var payload = Convert.FromHexString(hex);
        var decoded = Assert.IsType<LtScBossReviveDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(2, decoded.Entries.Count);
        Assert.Equal(0x380D25B2u, decoded.Entries[0].Timestamp);
        Assert.Equal(0x491E0010u, decoded.Entries[0].EventId);
    }

    [Fact]
    public void ArcadiaCoreSwitchState_DecodesFixtureSample()
    {
        var hex = "000800000000000000000000000000000000000000000000000000000000000000000000000000000000F8FFFFFF07000000000000000000000000000000000000000000000000000000000000000000000000703168F15E813F70008000F0FFFFFFFFFFFFFFFFFF0F00007C0C0000680000000D00000088CFB2250D5508770000000000000000007D0C0000";
        var payload = Convert.FromHexString(hex);
        var decoded = Assert.IsType<LtScArcadiaCoreSwitchStateDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(0x0C7C, decoded.CoreObjectId);
        Assert.Equal(0x0Du, decoded.EventKind);
        Assert.Equal(0x550D25B2u, decoded.Timestamp);
        Assert.Equal(0x88, decoded.SubState);
        Assert.Equal(payload.Length, decoded.PayloadLength);
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

        var weaponSlot = report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScSetWeaponSlot);
        Assert.NotNull(weaponSlot);
        Assert.Equal(0, weaponSlot!.Unknown);

        var bossRevive = report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScBossRevive);
        Assert.NotNull(bossRevive);
        Assert.Equal(0, bossRevive!.Unknown);

        var arcadia = report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScArcadiaCoreSwitchState);
        Assert.NotNull(arcadia);
        Assert.Equal(0, arcadia!.Unknown);
    }
}
