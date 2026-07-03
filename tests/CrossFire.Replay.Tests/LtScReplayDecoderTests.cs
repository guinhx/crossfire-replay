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
    public void MscNone4_DecodesIdOnlyPayload()
    {
        var payload = new byte[] { 0xE9, 0x01 };
        Assert.IsType<LtMscNone4Decoded>(LtMessageReader.TryDecode(payload));
    }

    [Fact]
    public void ForceLeavePollStart_DecodesConcatenatedFixtureSample()
    {
        var hex = "9300000013000000568BB2250D36001F00FE1168548DB7726DD2CCCCF8F900F3170000710000000F000000368BB2250D320000FFCFA33D7C391EC04A0000F417";
        var payload = Convert.FromHexString(hex);
        var decoded = Assert.IsType<LtScForceLeavePollStartDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Single(decoded.Entries);
        Assert.Equal(0x360D25B2u, decoded.Entries[0].Timestamp);
    }

    [Fact]
    public void BombSitesPeek_RejectsHugePayload()
    {
        var payload = new byte[1024];
        payload[0] = 0x00;
        payload[1] = 0x00;
        payload[2] = 0x01;
        Assert.False(LtMessageReader.TryPeekMessageId(payload, out _));
    }

    [Fact]
    public void ReqDropWeapon_DecodesMinimalPayloads()
    {
        Assert.IsType<LtCsReqDropWeaponDecoded>(LtMessageReader.TryDecode(new byte[] { 0x0F, 0x00 }));
        var padded = Assert.IsType<LtCsReqDropWeaponDecoded>(LtMessageReader.TryDecode(Convert.FromHexString("0F000000")));
        Assert.True(padded.IsMinimalPayload);
    }

    [Fact]
    public void LadderArea_DecodesCompactFixtureSample()
    {
        var payload = Convert.FromHexString("0100");
        var minimal = Assert.IsType<LtLadderAreaDecoded>(LtMessageReader.TryDecode(payload));
        Assert.False(minimal.HasFullGeometry);

        var compact = Assert.IsType<LtLadderAreaDecoded>(
            LtMessageReader.TryDecode(Convert.FromHexString("01000000000000000D0000005A3000000A0A0A001F00300000")));
        Assert.False(compact.HasFullGeometry);
        Assert.Equal(13f, compact.Position.X);
        Assert.Equal(0x0A, compact.LadderType);
    }

    [Fact]
    public void ActObjectDestroyPeek_RejectsHugePayload()
    {
        var payload = new byte[512];
        payload[0] = 0x00;
        payload[1] = 0x03;
        Assert.False(LtMessageReader.TryPeekMessageId(payload, out _));
    }

    [Fact]
    public void NjAiFireStart_DecodesFixtureSample()
    {
        var payload = Convert.FromHexString("0002000000010000");
        var decoded = Assert.IsType<LtScNjAiFireStartDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(1, decoded.FieldA);
        Assert.Equal(0, decoded.FieldB);
    }

    [Fact]
    public void PlayerLevelUp_DecodesFixtureSample()
    {
        var payload = Convert.FromHexString("55005D00030A00006B11000005000000");
        var decoded = Assert.IsType<LtScPlayerLevelUpDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(93, decoded.LevelOrValue);
        Assert.Equal(3, decoded.CharacterIndex);
        Assert.Equal(10, decoded.Amount);
    }

    [Fact]
    public void DamageSite_DecodesFixtureSample()
    {
        var payload = Convert.FromHexString("000100000000000000000000008CC44735000000004336393732003000000000000000000000452B703900000000433838333200300000000000000000006883F63400000000433230333600300000000000000000009C50931500000000433132");
        var decoded = Assert.IsType<LtScDamageSiteDecoded>(LtMessageReader.TryDecode(payload));
        Assert.NotEmpty(decoded.Entries);
    }

    [Fact]
    public void BombSitesPeek_RejectsInvalidAreaOnCompactPayload()
    {
        var payload = Convert.FromHexString("000004FE4E39000000004336343732003000000000000000000011A74039000000004336373633003000000000000000");
        Assert.False(LtMessageReader.TryPeekMessageId(payload, out _));
    }

    [Fact]
    public void AllScoresPeek_RejectsInvalidTeamCount()
    {
        var payload = Convert.FromHexString("20003000000000000000000000000000992D000001010100FFFF300000000000");
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
        if (itemDropped is not null)
            Assert.True(itemDropped.Semantic >= itemDropped.Unknown, $"item dropped semantic={itemDropped.Semantic} unknown={itemDropped.Unknown}");

        var towerFire = report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScAi2ModeDefenceTowerFire);
        Assert.NotNull(towerFire);
        Assert.Equal(0, towerFire!.Unknown);

        var weaponSlot = report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScSetWeaponSlot);
        if (weaponSlot is not null)
            Assert.True(weaponSlot.Semantic > weaponSlot.Unknown);

        Assert.True(report.SemanticPacketRatio >= 0.85);

        foreach (var row in new[]
                 {
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScBossRevive),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScArcadiaCoreSwitchState),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScCheatScaleDown),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScAi2ModeDefenceTowerChangeState),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScAddTimeItem),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScDamageSiteState),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScForceLeavePollStart),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgMscNone4),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScNjAiFireStart),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScPlayerLevelUp),
                 })
        {
            if (row is not null)
                Assert.Equal(0, row.Unknown);
        }
    }
}
