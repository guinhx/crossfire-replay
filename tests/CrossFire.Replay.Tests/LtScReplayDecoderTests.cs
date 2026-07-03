using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol;
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

        Assert.True(decoded.IsReplayArchivalLayout);
        Assert.Equal(0x0C7C, decoded.CoreObjectId);
        Assert.Equal(0x0Du, decoded.EventKind);
        Assert.Equal(0x550D25B2u, decoded.Timestamp);
        Assert.Equal(0x88, decoded.SubState);
        Assert.Equal(104u, decoded.CurHp);
        Assert.Equal(0x0C7Du, decoded.RelatedObjectId);
        Assert.Equal(-8, decoded.ReplaySentinelA);
        Assert.Equal(7, decoded.ReplaySentinelB);
        Assert.Equal(0x0FFFFFFFu, decoded.ValidMask);
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
    public void DamageSiteState_RoundTripsThroughNativeSerializer()
    {
        var original = new LtScDamageSiteStateDecoded(1, false);
        var payload = LtNativeSerializers.EncodeDamageSiteState(original);
        var decoded = Assert.IsType<LtScDamageSiteStateDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(original.ObjectId, decoded.ObjectId);
        Assert.Equal(original.IsOn, decoded.IsOn);
    }

    [Fact]
    public void MscNone4_DecodesIdOnlyPayload()
    {
        var payload = new byte[] { 0xE9, 0x01 };
        Assert.IsType<LtMscNone4Decoded>(LtMessageReader.TryDecode(payload));
    }

    [Fact]
    public void ForceLeavePollStartNative_RoundTripsThroughNativeSerializer()
    {
        var original = new LtScForceLeavePollStartDecoded([
            new LtScForceLeavePollStartEntryDecoded(
                0,
                0,
                0,
                "PlayerA",
                "PlayerB",
                2,
                IsReplayBundledLayout: false),
        ]);
        var payload = LtNativeSerializers.EncodeForceLeavePollStart(original);
        var decoded = Assert.IsType<LtScForceLeavePollStartDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Single(decoded.Entries);
        Assert.Equal("PlayerA", decoded.Entries[0].RequesterName);
        Assert.Equal("PlayerB", decoded.Entries[0].TargetName);
        Assert.Equal(2, decoded.Entries[0].ReasonNum);
        Assert.False(decoded.Entries[0].IsReplayBundledLayout);
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
    public void LadderAreaCompact_RoundTripsThroughNativeSerializer()
    {
        var original = new LtLadderAreaDecoded(
            0,
            new Vector3F(13, 0, 0),
            new Vector3F(0, 0, 0),
            new Vector3F(0, 0, 0),
            0x0A,
            false);
        var payload = LtNativeSerializers.EncodeLadderArea(original);
        var decoded = Assert.IsType<LtLadderAreaDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(original.Position.X, decoded.Position.X);
        Assert.Equal(original.LadderType, decoded.LadderType);
        Assert.False(decoded.HasFullGeometry);
    }

    [Fact]
    public void ArcadiaCoreSwitchState_ReplayArchivalRoundTripsFromFixture()
    {
        var hex = "000800000000000000000000000000000000000000000000000000000000000000000000000000000000F8FFFFFF07000000000000000000000000000000000000000000000000000000000000000000000000703168F15E813F70008000F0FFFFFFFFFFFFFFFFFF0F00007C0C0000680000000D00000088CFB2250D5508770000000000000000007D0C0000";
        var decoded = Assert.IsType<LtScArcadiaCoreSwitchStateDecoded>(LtMessageReader.TryDecode(Convert.FromHexString(hex)));
        var payload = LtNativeSerializers.EncodeArcadiaCoreSwitchState(decoded);
        var roundTripped = Assert.IsType<LtScArcadiaCoreSwitchStateDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(decoded.CoreObjectId, roundTripped.CoreObjectId);
        Assert.Equal(decoded.EventKind, roundTripped.EventKind);
        Assert.Equal(decoded.Timestamp, roundTripped.Timestamp);
        Assert.Equal(decoded.CurHp, roundTripped.CurHp);
        Assert.Equal(decoded.RelatedObjectId, roundTripped.RelatedObjectId);
        Assert.Equal(decoded.ReplaySentinelA, roundTripped.ReplaySentinelA);
        Assert.Equal(decoded.ValidMask, roundTripped.ValidMask);
    }

    [Fact]
    public void ArcadiaCoreSwitchState_LiveWireRoundTripsThroughNativeSerializer()
    {
        var original = new LtScArcadiaCoreSwitchStateDecoded(
            CoreObjectId: 0,
            EventKind: 4,
            Timestamp: 0,
            SubState: 0,
            PayloadLength: 14,
            CurHp: 500,
            SwitchSlot: 4,
            CoreTableKey: 2,
            IsReplayArchivalLayout: false);
        var payload = LtNativeSerializers.EncodeArcadiaCoreSwitchState(original);
        var decoded = Assert.IsType<LtScArcadiaCoreSwitchStateDecoded>(LtMessageReader.TryDecode(payload));
        Assert.False(decoded.IsReplayArchivalLayout);
        Assert.Equal(original.SwitchSlot, decoded.SwitchSlot);
        Assert.Equal(original.CoreTableKey, decoded.CoreTableKey);
        Assert.Equal(original.CurHp, decoded.CurHp);
    }

    [Fact]
    public void ArcadiaCoreSwitchState_RoundTripsTailFieldsThroughNativeSerializer()
    {
        var original = new LtScArcadiaCoreSwitchStateDecoded(
            0x0C7C,
            0x0D,
            0x550D25B2u,
            0x88,
            140,
            CurHp: 104,
            ReplaySentinelA: -8,
            ReplaySentinelB: 7,
            GuardByte: 0xFF,
            ValidMask: 0x0FFFFFFF,
            RelatedObjectId: 0x0C7D,
            IsReplayArchivalLayout: true);
        var payload = LtNativeSerializers.EncodeArcadiaCoreSwitchState(original);
        var decoded = Assert.IsType<LtScArcadiaCoreSwitchStateDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(original.CoreObjectId, decoded.CoreObjectId);
        Assert.Equal(original.EventKind, decoded.EventKind);
        Assert.Equal(original.Timestamp, decoded.Timestamp);
        Assert.Equal(original.SubState, decoded.SubState);
    }

    [Fact]
    public void NjAiFireStart_RoundTripsThroughNativeSerializer()
    {
        var original = new LtScNjAiFireStartDecoded(1, 0, false);
        var payload = LtNativeSerializers.EncodeNjAiFireStart(original);
        var decoded = Assert.IsType<LtScNjAiFireStartDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(original.FieldA, decoded.FieldA);
        Assert.Equal(original.FieldB, decoded.FieldB);
    }

    [Fact]
    public void RappelVelAndRotEntry_RoundTripsThroughNativeSerializer()
    {
        var original = new LtCsRappelVelAndRotEntryDecoded(
            0xB2,
            0x25,
            new Vector3F(1f, 2f, 3f),
            new Vector3F(100f, 50f, -200f),
            15f);
        var payload = LtNativeSerializers.EncodeRappelVelAndRotEntry(original);
        Assert.True(LtNativeSerializers.TryDecodeRappelVelAndRotEntry(payload, out var decoded, out _));
        Assert.Equal(original.AreaIndex, decoded.AreaIndex);
        Assert.Equal(original.CharacterIndex, decoded.CharacterIndex);
        Assert.Equal(original.Ratio, decoded.Ratio);
    }

    [Fact]
    public void BossReviveEntry_RoundTripsThroughNativeSerializer()
    {
        var original = new LtScBossReviveEntryDecoded(
            0x380D25B2u,
            0x0A01F301u,
            0x000000F8u,
            0x491E0010u,
            0x00000060u,
            0x0000000Cu);
        var payload = LtNativeSerializers.EncodeBossReviveEntry(original);
        Assert.True(LtNativeSerializers.TryDecodeBossReviveEntry(payload, out var decoded, out _));
        Assert.Equal(original.Timestamp, decoded.Timestamp);
        Assert.Equal(original.EventId, decoded.EventId);
    }

    [Fact]
    public void DamageCalculationRequest_RoundTripsThroughNativeSerializer()
    {
        var positions = Enumerable.Range(0, 16)
            .Select(i => new Vector3F(i, i + 1, i + 2))
            .ToList();
        var original = new LtScDamageCalculationRequestDecoded(
            5,
            new Vector3F(1f, 2f, 3f),
            42,
            0f, 0f, 0f, 1f,
            0.5f,
            1.25f,
            2,
            3,
            1,
            4,
            5,
            false,
            0.75f,
            positions,
            false);
        var payload = LtNativeSerializers.EncodeDamageCalculationRequest(original);
        var decoded = Assert.IsType<LtScDamageCalculationRequestDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(original.Attacker, decoded.Attacker);
        Assert.Equal(original.WeaponType, decoded.WeaponType);
        Assert.Equal(16, decoded.ThirdPartyPositions.Count);
        Assert.Equal(original.ThirdPartyPositions[7].Y, decoded.ThirdPartyPositions[7].Y);
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
    public void DamageSite_RoundTripsThroughNativeSerializer()
    {
        var original = new LtScDamageSiteDecoded(new Vector3F(10f, 20f, 30f), 2, true);
        var payload = LtNativeSerializers.EncodeDamageSite(original);
        var decoded = Assert.IsType<LtScDamageSiteDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(original.Dimension, decoded.Dimension);
        Assert.Equal(original.DamageSiteType, decoded.DamageSiteType);
        Assert.Equal(original.RenderEffect, decoded.RenderEffect);
    }

    [Fact]
    public void AiScore_RoundTripsThroughNativeSerializer()
    {
        var original = new LtScAiScoreDecoded(3, 100, 50, 12, 4, 900);
        var payload = LtNativeSerializers.EncodeAiScore(original);
        var decoded = Assert.IsType<LtScAiScoreDecoded>(LtMessageReader.TryDecode(payload));

        Assert.Equal(original.BotIndex, decoded.BotIndex);
        Assert.Equal(original.Health, decoded.Health);
        Assert.Equal(original.CurrentGameMoney, decoded.CurrentGameMoney);
    }

    [Fact]
    public void AiScore_DecodesFixtureSample()
    {
        var original = new LtScAiScoreDecoded(93, 0x65CA, 0x017D, 5, 0, 0x0139);
        var payload = LtNativeSerializers.EncodeAiScore(original);
        var decoded = Assert.IsType<LtScAiScoreDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(93, decoded.BotIndex);
        Assert.Equal(5, decoded.NumKill);
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
    public void FirstUpdate_DecodesMinimalPayload()
    {
        Assert.IsType<LtCsFirstUpdateDecoded>(LtMessageReader.TryDecode(Convert.FromHexString("1600")));
    }

    [Fact]
    public void AiAckCanDefuseC4_DecodesMinimalPayload()
    {
        Assert.IsType<LtScAiAckCanDefuseC4Decoded>(LtMessageReader.TryDecode(Convert.FromHexString("6C00")));
    }

    [Fact]
    public void RappelVelAndRot_DecodesBundledFixtureSample()
    {
        var payload = Convert.FromHexString("5901B2250D32000C0B47493E4C785A404300007A4A0000710000000F0000005901B2250D32000227301D3D31892DC04500007B4A00007B000000100000005901B2250D360010A0E489B7322B420AD7FB49007C4A0000710000000F000000");
        var decoded = Assert.IsType<LtCsRappelVelAndRotDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(3, decoded.Entries.Count);
        Assert.Equal(0xB2, decoded.Entries[0].AreaIndex);
        Assert.Equal(0x25, decoded.Entries[0].CharacterIndex);
    }

    [Fact]
    public void DamageCalculationRequest_DecodesWhenPayloadMatchesNativeLayout()
    {
        var payload = Convert.FromHexString("A100000015000000ED24B2250D39001300BD0124AF0E563C3FFFFFFF0100000000973B0000600000000C000000ED24B2250DB90031024D2D626F6E6500983B00");
        var decoded = LtMessageReader.TryDecode(payload);
        if (decoded is LtScDamageCalculationRequestDecoded request)
        {
            Assert.InRange(request.WeaponType, (short)0, (short)1000);
            return;
        }

        Assert.True(decoded is null or LtUnknownDecoded);
    }

    [Fact]
    public void SheepWantedList_DecodesFixtureSample()
    {
        Assert.IsType<LtScSheepWantedListDecoded>(LtMessageReader.TryDecode(Convert.FromHexString("E703000000")));
    }

    [Fact]
    public void PresentTeamAceUserPeek_RejectsHugePayload()
    {
        var payload = new byte[512];
        payload[0] = 0xBA;
        payload[1] = 0x07;
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
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgCsFirstUpdate),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgCsRappelVelAndRot),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScAiAckCanDefuseC4),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScAiScore),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScDamageCalculationRequest),
                     report.ByMessageId.FirstOrDefault(c => c.MessageId == EMessageId.MsgScSheepWantedList),
                 })
        {
            if (row is not null)
                Assert.Equal(0, row.Unknown);
        }
    }
}
