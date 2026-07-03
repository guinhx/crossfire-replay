using CrossFire.Replay.Protocol;

namespace CrossFire.Replay.Protocol.Lt;

public static class LtMessageReader
{
    public static bool TryPeekMessageId(ReadOnlySpan<byte> payload, out EMessageId messageId)
    {
        messageId = default;
        if (payload.Length < 2)
            return false;

        try
        {
            var reader = new LtBitstreamReader(payload);
            var id = reader.ReadMessageId();
            if (!EMessageIdCatalog.IsPlausible(id))
                return false;

            if (!ValidateMessagePeek((EMessageId)id, reader, payload))
                return false;

            messageId = (EMessageId)id;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool ValidateMessagePeek(EMessageId id, LtBitstreamReader reader, ReadOnlySpan<byte> payload) =>
        id switch
        {
            EMessageId.MsgScBombSites => ValidateBombSitesPeek(reader, payload),
            _ => ValidateGenericPeek(id, payload),
        };

    private static bool ValidateGenericPeek(EMessageId id, ReadOnlySpan<byte> payload)
    {
        if (payload.Length > GetMaxPlausiblePayloadBytes(id))
            return false;

        if (payload.Length <= 512)
            return true;

        return id switch
        {
            EMessageId.MsgCsAiReqCanDefuseC4 => false,
            EMessageId.MsgScAiDamage => false,
            EMessageId.MsgScDamageSite => false,
            _ => true,
        };
    }

    private static int GetMaxPlausiblePayloadBytes(EMessageId id) => id switch
    {
        EMessageId.MsgCsReqLuckyBoom => 24,
        EMessageId.MsgScAllScores => 1024,
        EMessageId.MsgCsBoomGrenade => 64,
        EMessageId.MsgCsReqForceChangeWeapon => 64,
        EMessageId.MsgCsAmmoReload => 64,
        EMessageId.MsgCsScoreInfoOnOff => 64,
        EMessageId.MsgCsHackParam => 128,
        EMessageId.MsgCsAiReqCanDefuseC4 => 64,
        EMessageId.MsgScGetMyWeapon => 128,
        EMessageId.MsgScAddTimeItem => 48,
        EMessageId.MsgScAmmoSupplySite => 32,
        EMessageId.MsgScDamageSiteState => 16,
        EMessageId.MsgScDamageSite => 128,
        EMessageId.MsgScAiDamage => 128,
        EMessageId.MsgScNanoNanopoint => 128,
        EMessageId.MsgScHpInfoAi => 128,
        EMessageId.MsgScAi2ModeDefenceWeaponCrossbowFire => 128,
        EMessageId.MsgScKingsObjectInitRockPaperScissors => 128,
        EMessageId.MsgScAiBossTowerChangeSkillStateUsing => 128,
        EMessageId.MsgSc3rdExplodeDestroyGeneratorFire => 128,
        EMessageId.MsgCsRappelVelAndRot => 128,
        EMessageId.MsgCsWireC4Defuse => 128,
        EMessageId.MsgScAiCraterRenewalEnergyBallDestroy => 256,
        _ => int.MaxValue,
    };

    private static bool ValidateBombSitesPeek(LtBitstreamReader reader, ReadOnlySpan<byte> payload)
    {
        if (payload.Length is < 3 or > 256)
            return false;

        var count = reader.ReadUInt8();
        return count is >= 1 and <= 5;
    }

    public static LtDecodedMessage? TryDecode(ReadOnlySpan<byte> payload)
    {
        if (!TryPeekMessageId(payload, out var id))
            return null;

        try
        {
            return id switch
            {
                EMessageId.MsgCsAnim => DecodeAnim(payload),
                EMessageId.MsgScAnim => DecodeScAnim(payload),
                EMessageId.MsgCsVelAndRot => LtCsSemanticDecoders.TryDecodeVelAndRot(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScVelocity => DecodeVelocity(payload),
                EMessageId.MsgCsGunDirRot or EMessageId.MsgScGunDirRot => DecodeGunDirRot(payload),
                EMessageId.MsgScRoundStart => DecodeRoundStart(payload),
                EMessageId.MsgScRoundEnd => DecodeRoundEnd(payload),
                EMessageId.MsgScPlayerDie => DecodePlayerDie(payload),
                EMessageId.MsgScFire => DecodeFire(payload),
                EMessageId.MsgScPlayerScore => DecodePlayerScore(payload),
                EMessageId.MsgScSendC4Object => DecodeSendC4Object(payload),
                EMessageId.MsgScPlayerOut => DecodePlayerOut(payload),
                EMessageId.MsgScBombSites => DecodeBombSites(payload),
                EMessageId.MsgScWorldProps => DecodeWorldProps(payload),
                EMessageId.MsgScScore => LtSemanticDecoders.TryDecodeScore(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScRoundTimeLeft => LtSemanticDecoders.TryDecodeRoundTimeLeft(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScAllScores => LtSemanticDecoders.TryDecodeAllScores(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScThrowGrenade => LtSemanticDecoders.TryDecodeThrowGrenade(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScShotInfo => LtSemanticDecoders.TryDecodeShotInfo(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScSetCurWeapon => LtSemanticDecoders.TryDecodeSetCurWeapon(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScLadderArea => LtSemanticDecoders.TryDecodeLadderArea(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScPlayerRespawn => LtSemanticDecoders.TryDecodePlayerRespawn(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScPlayerIn => LtSemanticDecoders.TryDecodePlayerIn(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgCs1SecondPassed => Decode1SecondPassed(payload),
                EMessageId.MsgScHiddenTeamIndex => DecodeHiddenTeamIndex(payload),
                EMessageId.MsgScDamage => LtSemanticDecoders.TryDecodeDamage(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScSetWeaponSlot => LtSemanticDecoders.TryDecodeSetWeaponSlot(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScHitInfo => LtSemanticDecoders.TryDecodeHitInfo(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgCsReqDropWeapon => LtCsSemanticDecoders.TryDecodeReqDropWeapon(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgCsReqChangeWeapon or EMessageId.MsgScAckForceChangeWeapon or EMessageId.MsgScAckSublinkChangeWeapon
                    => LtCsSemanticDecoders.TryDecodeChangeWeapon(payload, id) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgCsReqLinkWeapon or EMessageId.MsgScAckLinkWeapon
                    => LtCsSemanticDecoders.TryDecodeLinkWeapon(payload, id) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgCsFrogJump => LtCsSemanticDecoders.TryDecodeFrogJump(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgCsLandingState => LtCsSemanticDecoders.TryDecodeLandingState(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScIngameItemDropped => LtScReplayDecoders.TryDecodeIngameItemDropped(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScAi2ModeDefenceTowerFire => LtScReplayDecoders.TryDecodeDefenceTowerFire(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScBossRevive => LtScReplayDecoders.TryDecodeBossRevive(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScArcadiaCoreSwitchState => LtScReplayDecoders.TryDecodeArcadiaCoreSwitchState(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScAi2ModeDefenceTowerChangeState => LtScReplayDecoders.TryDecodeDefenceTowerChangeState(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScCheatScaleDown => LtScReplayDecoders.TryDecodeCheatScaleDown(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScAddTimeItem => LtScReplayDecoders.TryDecodeAddTimeItem(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScDamageSiteState => LtScReplayDecoders.TryDecodeDamageSiteState(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgScForceLeavePollStart => LtScReplayDecoders.TryDecodeForceLeavePollStart(payload) ?? new LtUnknownDecoded(id, payload.Length),
                EMessageId.MsgMscNone4 => LtScReplayDecoders.TryDecodeMscNone4(payload) ?? new LtUnknownDecoded(id, payload.Length),
                _ => new LtUnknownDecoded(id, payload.Length),
            };
        }
        catch (InvalidOperationException)
        {
            return new LtUnknownDecoded(id, payload.Length);
        }
    }

    private static LtAnimDecoded DecodeAnim(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        return new LtAnimDecoded(
            reader.ReadUInt16(),
            reader.ReadUInt8(),
            reader.ReadBoolean(),
            reader.ReadSingle(),
            reader.ReadInt8(),
            reader.ReadBoolean(),
            reader.ReadUInt8());
    }

    private static LtScAnimDecoded DecodeScAnim(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        return new LtScAnimDecoded(
            reader.ReadUInt16(),
            reader.ReadUInt8(),
            reader.ReadBoolean(),
            reader.ReadSingle(),
            reader.ReadInt8(),
            reader.ReadBoolean(),
            reader.ReadUInt8());
    }

    private static LtVelocityDecoded DecodeVelocity(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        return new LtVelocityDecoded(
            reader.ReadBoolean(),
            reader.ReadVector3(),
            reader.ReadVector3(),
            reader.ReadInt8(),
            reader.ReadBoolean());
    }

    private static LtGunDirRotDecoded DecodeGunDirRot(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        return new LtGunDirRotDecoded(reader.ReadInt8(), reader.ReadVector3());
    }

    private static LtCs1SecondPassedDecoded Decode1SecondPassed(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        return new LtCs1SecondPassedDecoded();
    }

    private static LtHiddenTeamIndexDecoded DecodeHiddenTeamIndex(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        return new LtHiddenTeamIndexDecoded(reader.ReadInt8());
    }

    private static LtRoundStartDecoded DecodeRoundStart(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        var autoSide = reader.IsEmpty ? 0 : reader.ReadInt32();
        return new LtRoundStartDecoded(autoSide);
    }

    private static LtRoundEndDecoded DecodeRoundEnd(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        var win = reader.ReadInt8();
        var clanWin = reader.IsEmpty ? (sbyte)0 : reader.ReadInt8();
        return new LtRoundEndDecoded(win, clanWin);
    }

    private static LtPlayerDieDecoded DecodePlayerDie(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        return new LtPlayerDieDecoded(
            reader.ReadInt8(),
            reader.ReadInt8(),
            reader.ReadInt16(),
            reader.ReadInt32(),
            !reader.IsEmpty && reader.ReadBoolean());
    }

    private static LtFireDecoded DecodeFire(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        return new LtFireDecoded(
            reader.ReadInt8(),
            reader.ReadVector3(),
            reader.ReadUInt8(),
            reader.ReadBoolean());
    }

    private static LtPlayerScoreDecoded DecodePlayerScore(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        return new LtPlayerScoreDecoded(reader.ReadInt8(), reader.ReadUInt16());
    }

    private static LtSendC4ObjectDecoded DecodeSendC4Object(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        var planted = reader.ReadBoolean();
        var pos = reader.ReadVector3();
        var site = reader.IsEmpty ? (short)0 : reader.ReadInt16();
        return new LtSendC4ObjectDecoded(planted, pos, site);
    }

    private static LtPlayerOutDecoded DecodePlayerOut(ReadOnlySpan<byte> payload)
    {
        var reader = new LtBitstreamReader(payload);
        reader.ReadMessageId();
        var character = reader.ReadInt8();
        var reason = reader.IsEmpty ? (byte)0 : reader.ReadUInt8();
        return new LtPlayerOutDecoded(character, reason);
    }

    private static LtDecodedMessage? DecodeBombSites(ReadOnlySpan<byte> payload)
    {
        if (!TryDecodeBombSites(payload, out var decoded))
            return new LtUnknownDecoded(EMessageId.MsgScBombSites, payload.Length);

        return decoded;
    }

    public static bool TryDecodeBombSites(ReadOnlySpan<byte> payload, out LtBombSitesDecoded? decoded)
    {
        decoded = null;
        if (payload.Length < 3)
            return false;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScBombSites)
                return false;

            var count = reader.ReadUInt8();
            if (count is 0 or > 5)
                return false;

            var sites = new List<LtBombSiteInfo>(count);
            for (var i = 0; i < count; i++)
            {
                var area = reader.ReadUInt8();
                var pos = reader.ReadVector3();
                var dim = reader.ReadVector3();
                sites.Add(new LtBombSiteInfo(area, pos, dim));
            }

            if (payload.Length > 512 && sites.All(IsZeroSite))
                return false;

            decoded = new LtBombSitesDecoded(sites);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsZeroSite(LtBombSiteInfo site) =>
        site.AreaNumber == 0
        && site.Position.X == 0 && site.Position.Y == 0 && site.Position.Z == 0
        && site.Dimensions.X == 0 && site.Dimensions.Y == 0 && site.Dimensions.Z == 0;

    private static LtDecodedMessage? DecodeWorldProps(ReadOnlySpan<byte> payload) =>
        TryDecodeWorldProps(payload, out var decoded) ? decoded : new LtUnknownDecoded(EMessageId.MsgScWorldProps, payload.Length);

    public static bool TryDecodeWorldProps(ReadOnlySpan<byte> payload, out LtWorldPropsDecoded? decoded)
    {
        decoded = null;
        foreach (var skip in new[] { 0, 2, 12 })
        {
            if (skip >= payload.Length)
                continue;

            if (TryDecodeWorldPropsAt(payload[skip..], out decoded))
                return true;
        }

        return false;
    }

    private static bool TryDecodeWorldPropsAt(ReadOnlySpan<byte> payload, out LtWorldPropsDecoded? decoded)
    {
        decoded = null;
        if (payload.Length < 32)
            return false;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScWorldProps)
                return false;

            var farZ = reader.ReadUInt32();
            if (farZ == LtBitstreamReader.InvalidSentinel)
                return false;

            var background = reader.ReadVector3();
            var fogEnable = reader.ReadBoolean();
            var fogColor = reader.ReadVector3();
            var fogNearZ = reader.ReadUInt32();
            var fogFarZ = reader.ReadUInt32();
            if (fogNearZ == LtBitstreamReader.InvalidSentinel || fogFarZ == LtBitstreamReader.InvalidSentinel)
                return false;

            var skyFogEnable = reader.ReadBoolean();
            var skyFogNearZ = reader.ReadUInt32();
            var skyFogFarZ = reader.ReadUInt32();
            var skyScale = reader.ReadSingle();
            if (skyFogNearZ == LtBitstreamReader.InvalidSentinel || skyFogFarZ == LtBitstreamReader.InvalidSentinel)
                return false;

            decoded = new LtWorldPropsDecoded(
                farZ,
                background,
                fogEnable,
                fogColor,
                fogNearZ,
                fogFarZ,
                skyFogEnable,
                skyFogNearZ,
                skyFogFarZ,
                skyScale);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
