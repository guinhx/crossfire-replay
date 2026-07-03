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
    public void CheatScaleDown_DecodesFixtureSample()
    {
        var hex = "880200000000000000000000F8FFFFFF0700000000000000000000000000000000000000000000000080010000000000";
        var payload = Convert.FromHexString(hex);
        var decoded = Assert.IsType<LtScCheatScaleDownDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(0f, decoded.Scale);
        Assert.Equal(0, decoded.SendIndex);
        Assert.True(decoded.HasTrailingData);
    }

    [Fact]
    public void DefenceTowerChangeState_DecodesFixtureSample()
    {
        var hex = "7D03B2250DD7040C64000000640000000200000005000000010000000000000000000000000000000000000000000000";
        var payload = Convert.FromHexString(hex);
        var decoded = Assert.IsType<LtScDefenceTowerChangeStateDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(0xD70D25B2u, decoded.Timestamp);
        Assert.Equal(0u, decoded.TowerIndex);
        Assert.Equal(100u, decoded.StateA);
        Assert.Equal(2u, decoded.StateB);
    }

    [Fact]
    public void AddTimeItem_DecodesIdOnlyPayload()
    {
        var payload = new byte[] { 0x9A, 0x00 };
        var decoded = Assert.IsType<LtScAddTimeItemDecoded>(LtMessageReader.TryDecode(payload));
        Assert.False(decoded.HasBody);
    }

    [Fact]
    public void DamageSiteState_DecodesFixtureSample()
    {
        var hex = "01010100";
        var payload = Convert.FromHexString(hex);
        var decoded = Assert.IsType<LtScDamageSiteStateDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(1u, decoded.ObjectHandle);
        Assert.False(decoded.IsOn);
    }

    [Fact]
    public void DamageSiteState_DecodesExtendedFixtureSample()
    {
        var hex = "01010100FF";
        var payload = Convert.FromHexString(hex);
        var decoded = Assert.IsType<LtScDamageSiteStateDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(1u, decoded.ObjectHandle);
        Assert.True(decoded.IsOn);
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

        Assert.True(report.SemanticPacketRatio >= 0.85);

        foreach (var row in new[]
                 {
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScBossRevive),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScArcadiaCoreSwitchState),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScCheatScaleDown),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScAi2ModeDefenceTowerChangeState),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScAddTimeItem),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScDamageSiteState),
                 })
        {
            if (row is not null)
                Assert.Equal(0, row.Unknown);
        }
    }
}
