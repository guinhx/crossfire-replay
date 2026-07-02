namespace CrossFire.Replay.Protocol.Lt;

public static class LtMessageWriter
{
    public static byte[] Encode(LtDecodedMessage message) =>
        message switch
        {
            LtScoreDecoded score => EncodeScore(score),
            LtRoundTimeLeftDecoded roundTime => EncodeRoundTimeLeft(roundTime),
            LtShotInfoDecoded shotInfo => EncodeShotInfo(shotInfo),
            LtSetCurWeaponDecoded setWeapon => EncodeSetCurWeapon(setWeapon),
            LtPlayerRespawnDecoded respawn => EncodePlayerRespawn(respawn),
            LtRoundStartDecoded roundStart => EncodeRoundStart(roundStart),
            LtRoundEndDecoded roundEnd => EncodeRoundEnd(roundEnd),
            LtPlayerScoreDecoded playerScore => EncodePlayerScore(playerScore),
            LtPlayerOutDecoded playerOut => EncodePlayerOut(playerOut),
            LtFireDecoded fire => EncodeFire(fire),
            LtGunDirRotDecoded gunDir => EncodeGunDirRot(gunDir),
            LtVelocityDecoded velocity => EncodeVelocity(velocity),
            LtAllScoresDecoded allScores => EncodeAllScores(allScores),
            LtDamageDecoded damage => EncodeDamage(damage),
            LtHitInfoDecoded hitInfo => EncodeHitInfo(hitInfo),
            _ => throw new NotSupportedException($"LT encode not implemented for {message.GetType().Name}."),
        };

    private static byte[] EncodeScore(LtScoreDecoded score)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScScore);
        writer.WriteUInt16(score.Health);
        writer.WriteUInt32(score.ArmorPoint);
        writer.WriteUInt16(score.NumKill);
        writer.WriteUInt16(score.NumDeath);
        writer.WriteUInt16(score.CurrentGameMoney);
        return writer.ToArray();
    }

    private static byte[] EncodeRoundTimeLeft(LtRoundTimeLeftDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScRoundTimeLeft);
        writer.WriteSingle(decoded.RoundTimeLeftSeconds);
        foreach (var health in decoded.TeamHealth)
            writer.WriteUInt16(health);
        return writer.ToArray();
    }

    private static byte[] EncodeShotInfo(LtShotInfoDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScShotInfo);
        writer.WriteUInt16(decoded.WeaponIndex);
        writer.WriteUInt16(decoded.CurrentAmmo);
        writer.WriteUInt16(decoded.MagazineAmmo);
        writer.WriteUInt16(decoded.FullAmmo);
        writer.WriteUInt16(decoded.MaxAmmo);
        writer.WriteUInt16(decoded.WeaponType);
        writer.WriteBoolean(decoded.AddAmmoByVvipBuff);
        writer.WriteBoolean(decoded.AmmoPlusByHeadShot);
        writer.WriteBoolean(decoded.OneAmmoInMagWeapon);
        writer.WriteBoolean(decoded.ProcessThrowMagazineAmmo);
        writer.WriteUInt16(decoded.ThrowMagazineAmmo);
        writer.WriteUInt16(decoded.VvipBuffAmmoCount);
        writer.WriteUInt16(decoded.LinkWeaponAmmoCount);
        return writer.ToArray();
    }

    private static byte[] EncodeSetCurWeapon(LtSetCurWeaponDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScSetCurWeapon);
        writer.WriteUInt8(decoded.CharacterIndex);
        writer.WriteUInt16(decoded.ObjectId);
        writer.WriteUInt16(decoded.WeaponObjectId);
        writer.WriteUInt16(decoded.WeaponType);
        writer.WriteUInt16(decoded.SelectedFuncItemIndex);
        return writer.ToArray();
    }

    private static byte[] EncodePlayerRespawn(LtPlayerRespawnDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScPlayerRespawn);
        writer.WriteUInt8(decoded.CharacterIndex);
        writer.WriteUInt8(decoded.CheckCharacterIndex);
        writer.WriteUInt16(decoded.ObjectId);
        writer.WriteVector3(decoded.Position);
        writer.WriteInt32(decoded.BaseLifeCount);
        writer.WriteUInt8(decoded.UserCharacterIndex);
        writer.WriteUInt8(decoded.TargetCharacterIndex);
        writer.WriteBoolean(decoded.EnableRageState);
        return writer.ToArray();
    }

    private static byte[] EncodeRoundStart(LtRoundStartDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScRoundStart);
        writer.WriteInt32(decoded.AutoSideChangeState);
        return writer.ToArray();
    }

    private static byte[] EncodeRoundEnd(LtRoundEndDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScRoundEnd);
        writer.WriteInt8(decoded.WinTeamIndex);
        writer.WriteInt8(decoded.ClanWinTeamIndex);
        return writer.ToArray();
    }

    private static byte[] EncodePlayerScore(LtPlayerScoreDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScPlayerScore);
        writer.WriteInt8(decoded.CharacterIndex);
        writer.WriteUInt16(decoded.Score);
        return writer.ToArray();
    }

    private static byte[] EncodePlayerOut(LtPlayerOutDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScPlayerOut);
        writer.WriteInt8(decoded.CharacterIndex);
        writer.WriteUInt8(decoded.ExitReason);
        return writer.ToArray();
    }

    private static byte[] EncodeFire(LtFireDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScFire);
        writer.WriteInt8(decoded.CharacterIndex);
        writer.WriteVector3(decoded.GunRot);
        writer.WriteUInt8(decoded.ColorMuzzle);
        writer.WriteBoolean(decoded.LeftShoot);
        return writer.ToArray();
    }

    private static byte[] EncodeGunDirRot(LtGunDirRotDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScGunDirRot);
        writer.WriteInt8(decoded.CharacterIndex);
        writer.WriteVector3(decoded.Rot);
        return writer.ToArray();
    }

    private static byte[] EncodeVelocity(LtVelocityDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScVelocity);
        writer.WriteBoolean(decoded.IncludeVelocity);
        writer.WriteVector3(decoded.Pos);
        writer.WriteVector3(decoded.Vel);
        writer.WriteInt8(decoded.CharacterIndex);
        writer.WriteBoolean(decoded.Teleport);
        return writer.ToArray();
    }

    private static byte[] EncodeAllScores(LtAllScoresDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScAllScores);
        writer.WriteUInt8(decoded.TeamCount);
        foreach (var teamScore in decoded.TeamScores)
            writer.WriteUInt16(teamScore);

        writer.WriteUInt8(decoded.PlayerCount);
        foreach (var player in decoded.Players)
        {
            writer.WriteUInt8(player.ClientIndex);
            writer.WriteUInt8(player.CharacterIndex);
            writer.WriteUInt16(player.Health);
            writer.WriteUInt16(player.Kills);
            writer.WriteUInt16(player.Deaths);
            writer.WriteUInt16((ushort)player.GoalInCount);
            writer.WriteUInt16(0);
            writer.WriteUInt32(player.TotalDamage);
            writer.WriteUInt16(player.Ping);
            writer.WriteUInt8(player.TeamIndex);
            writer.WriteUInt64(player.UserId);
            writer.WriteUInt16(player.EscapeCount);
            writer.WriteUInt32(player.AiKillScore);
            writer.WriteUInt16(0);
            writer.WriteUInt16(0);
            writer.WriteUInt16(0);
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);
            writer.WriteUInt16(0);
            writer.WriteUInt16(0);
            writer.WriteUInt32(0);
            writer.WriteUInt8(player.SoldierType);
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);
            writer.WriteUInt64(0);
            writer.WriteUInt32(0);
            writer.WriteUInt64(0);
        }

        writer.WriteUInt64(decoded.AceUserId);
        writer.WriteUInt64(decoded.TopEscapeUserId);
        writer.WriteInt32(decoded.TotalKills);
        writer.WriteInt32(decoded.KillCountsForWin);
        writer.WriteInt32(decoded.CurrentLevel);
        writer.WriteUInt16(decoded.ActiveBomberScore);
        writer.WriteUInt16(decoded.FirstHalfTeamScores[0]);
        writer.WriteUInt16(decoded.FirstHalfTeamScores[1]);
        return writer.ToArray();
    }

    private static byte[] EncodeDamage(LtDamageDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScDamage);
        writer.WriteInt8(decoded.AttackerIndex);
        writer.WriteVector3(decoded.From);
        writer.WriteUInt16(decoded.Damage);
        writer.WriteUInt8(decoded.DamageType);
        writer.WriteInt16(decoded.WeaponType);
        writer.WriteBoolean(decoded.IsAiSuperArmorActivated);
        writer.WriteUInt8(decoded.HitNodeType);
        return writer.ToArray();
    }

    private static byte[] EncodeHitInfo(LtHitInfoDecoded decoded)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScHitInfo);
        writer.WriteZeroBytes(LtSemanticDecoders.HitInfoBlobBytes);
        writer.WriteSingle(decoded.HitRates);
        writer.WriteSingle(decoded.HeadKillRates);
        writer.WriteInt32(decoded.SummingDamages);
        return writer.ToArray();
    }
}
