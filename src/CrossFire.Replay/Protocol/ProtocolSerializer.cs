using System.Text;
using CrossFire.Replay.IO;
using CrossFire.Replay.Protocol.Messages;

namespace CrossFire.Replay.Protocol;

public static class ProtocolSerializer
{
    public static void WritePayload(BinaryWriter writer, ReplayMessage message, CfrReadContext ctx)
    {
        if (message.Payload.Length > 0)
        {
            writer.Write(message.Payload);
            return;
        }

        switch (message)
        {
            case MapInfoMessage map:
                WriteMapInfo(writer, map, ctx);
                break;
            case PlayerInMessage playerIn:
                WritePlayerIn(writer, playerIn, ctx);
                break;
            case FireMessage fire:
                WriteFire(writer, fire, ctx);
                break;
            case AnimMessage anim:
                WriteAnim(writer, anim);
                break;
            case RoundStartMessage roundStart:
                WriteRoundStart(writer, roundStart, ctx);
                break;
            case RoundEndMessage roundEnd:
                WriteRoundEnd(writer, roundEnd);
                break;
            case PlayerDieMessage playerDie:
                WritePlayerDie(writer, playerDie, ctx);
                break;
            case VelocityMessage velocity:
                WriteVelocity(writer, velocity);
                break;
            case GunDirRotMessage gunDirRot:
                WriteGunDirRot(writer, gunDirRot);
                break;
            case SendC4ObjectMessage sendC4:
                WriteSendC4Object(writer, sendC4);
                break;
            case PlayerOutMessage playerOut:
                WritePlayerOut(writer, playerOut);
                break;
            case DelayTriggerMessage delayTrigger:
                WriteDelayTrigger(writer, delayTrigger);
                break;
            case TriggerOnOffReplayMessage triggerOnOff:
                WriteTriggerOnOffReplay(writer, triggerOnOff);
                break;
            case AllScoresMessage allScores:
                WriteAllScores(writer, allScores, ctx);
                break;
            default:
                throw new InvalidOperationException(
                    $"Message {message.MessageId} has no payload bytes and no serializer implementation.");
        }
    }

    private static void WriteMapInfo(BinaryWriter writer, MapInfoMessage msg, CfrReadContext ctx)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.MapIndex);
        writer.Write(msg.RoundType);
        writer.Write(msg.GameRule);
        writer.Write(msg.WinGoal);
        writer.Write(msg.UseVvipSubMode);

        if (ctx.HasFeature(ReplayFeatureFlags.UseClanSystemRefine2nd))
        {
            writer.Write(msg.ClanServerMatch);
            WriteLengthPrefixedAscii(writer, msg.ClanNameBl);
            WriteLengthPrefixedAscii(writer, msg.ClanNameGr);
        }
        else
        {
            CfrBinaryWriterExtensions.WriteFixedString(writer, msg.ClanNameBl, 0x22);
            CfrBinaryWriterExtensions.WriteFixedString(writer, msg.ClanNameGr, 0x22);
        }

        writer.Write(msg.IsNano4ExpansionMode);
        if (ctx.FileVersion >= 18)
            writer.Write(msg.IsAutoSideChange);
    }

    private static void WritePlayerIn(BinaryWriter writer, PlayerInMessage msg, CfrReadContext ctx)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.CharacterIndex);
        writer.Write(msg.TeamIndex);
        CfrBinaryWriterExtensions.WriteFixedString(writer, msg.UserName, 0xE);
        foreach (var dress in msg.DressItemInfoIndex)
            writer.Write(dress);
        writer.Write(msg.CharItemInfoIndex);
        writer.Write(msg.IsDead);
        writer.Write(msg.ZoomStep);
        if (ctx.FileVersion >= 30)
            writer.Write(msg.UserId);
        else
            writer.Write((int)msg.UserId);
        writer.Write(msg.IsObserver);
        foreach (var mark in msg.ClanMark)
            writer.Write(mark);
        writer.Write(msg.SpecialKickWeaponIndex);
        CfrBinaryWriterExtensions.WriteFixedString(writer, msg.ClanName, 0x1A);
        writer.Write(msg.MaxAccessory);
        foreach (var accessory in msg.AccessoryItemIndex)
            writer.Write(accessory);
        writer.Write(msg.SocialMotionWeaponIndex);
        foreach (var item in msg.FuncItemIndex)
            writer.Write(item);
        foreach (var period in msg.FuncItemPeriodIndex)
            writer.Write(period);
        writer.Write(msg.AutoSideChangeState);
        writer.Write(msg.ThrowMoneyWeaponIndex);
        writer.Write(msg.RememberMePrecedeWeaponIndex);
        if (ctx.HasFeature(ReplayFeatureFlags.UseCharacterSkinChangeSystem))
            writer.Write(msg.CharacterSkinIndex);
    }

    private static void WriteFire(BinaryWriter writer, FireMessage msg, CfrReadContext ctx)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.CharacterIndex);
        CfrBinaryWriterExtensions.WriteVector3(writer, msg.GunRot);
        writer.Write(msg.ColorMuzzle);
        if (msg.ProtocolVersion >= 2)
            writer.Write(msg.ShapeMuzzle);
        writer.Write(msg.LeftShoot);
        if (msg.ProtocolVersion != 0)
            writer.Write(msg.KnifeAttack);
        if (msg.ProtocolVersion >= 3)
        {
            writer.Write(msg.ShotsPerAmmo);
            writer.Write(msg.ShootThroughNum);
            foreach (var row in msg.HitPos)
            foreach (var hit in row)
                CfrBinaryWriterExtensions.WriteVector3(writer, hit);
            foreach (var row in msg.HitNodeType)
            foreach (var node in row)
                writer.Write(node);
        }
        if (msg.ProtocolVersion >= 4)
        {
            CfrBinaryWriterExtensions.WriteVector3(writer, msg.ShotPos);
            CfrBinaryWriterExtensions.WriteVector3(writer, msg.ShotDir);
        }
        if (ctx.FileVersion >= 8)
            writer.Write(msg.ChargeShot);
    }

    private static void WriteAnim(BinaryWriter writer, AnimMessage msg)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.AnimIndex);
        writer.Write(msg.TrackerId);
        writer.Write(msg.Looping);
        writer.Write(msg.AnimRate);
        writer.Write(msg.CharacterIndex);
        writer.Write(msg.Idle);
        writer.Write(msg.AnimSubType);
    }

    private static void WriteRoundStart(BinaryWriter writer, RoundStartMessage msg, CfrReadContext ctx)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        if (ctx.FileVersion >= 18)
            writer.Write(msg.AutoSideChangeState);
    }

    private static void WriteRoundEnd(BinaryWriter writer, RoundEndMessage msg)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.WinTeamIndex);
        if (msg.ProtocolVersion != 0)
            writer.Write(msg.ClanWinTeamIndex);
    }

    private static void WritePlayerDie(BinaryWriter writer, PlayerDieMessage msg, CfrReadContext ctx)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.CharacterIndex);
        writer.Write(msg.AttackerIndex);
        writer.Write(msg.WeaponType);
        writer.Write(msg.HitNodeType);
        if (msg.ProtocolVersion != 0)
            writer.Write(msg.KnifeAttack);
        if (ctx.FileVersion >= 8)
            writer.Write(msg.ChargeShot);
        if (ctx.FileVersion >= 10)
            writer.Write(msg.DieDir);
        if (ctx.FileVersion >= 15)
            writer.Write(msg.AiObjectIndex);
    }

    private static void WriteVelocity(BinaryWriter writer, VelocityMessage msg)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.IncludeVelocity);
        CfrBinaryWriterExtensions.WriteVector3(writer, msg.Pos);
        CfrBinaryWriterExtensions.WriteVector3(writer, msg.Vel);
        writer.Write(msg.CharacterIndex);
        writer.Write(msg.Teleport);
    }

    private static void WriteGunDirRot(BinaryWriter writer, GunDirRotMessage msg)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.CharacterIndex);
        CfrBinaryWriterExtensions.WriteVector3(writer, msg.Rot);
    }

    private static void WriteSendC4Object(BinaryWriter writer, SendC4ObjectMessage msg)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.Planted);
        CfrBinaryWriterExtensions.WriteVector3(writer, msg.C4Pos);
        if (msg.ProtocolVersion != 0)
            writer.Write(msg.BombSiteIndex);
        if (msg.ProtocolVersion >= 2)
        {
            writer.Write(msg.ChangeC4Object);
            writer.Write(msg.C4WeaponType);
        }
    }

    private static void WritePlayerOut(BinaryWriter writer, PlayerOutMessage msg)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.CharacterIndex);
        if (msg.ProtocolVersion != 0)
        {
            writer.Write(msg.ExitReason);
            writer.Write(msg.FirstCode);
            writer.Write(msg.SecondCode);
        }
    }

    private static void WriteDelayTrigger(BinaryWriter writer, DelayTriggerMessage msg)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.TriggerId);
    }

    private static void WriteTriggerOnOffReplay(BinaryWriter writer, TriggerOnOffReplayMessage msg)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        var data = msg.Data.Length >= 0x80 ? msg.Data : msg.Data.Concat(new byte[0x80 - msg.Data.Length]).ToArray();
        writer.Write(data, 0, 0x80);
    }

    private static void WriteAllScores(BinaryWriter writer, AllScoresMessage msg, CfrReadContext ctx)
    {
        writer.Write(msg.ProtocolVersion);
        writer.Write(msg.Timestamp);
        writer.Write(msg.TeamScoreGr);
        writer.Write(msg.TeamScoreBl);
        writer.Write(msg.UserIds);
        writer.Write(msg.Kills);
        writer.Write(msg.Deaths);
        if (msg.ProtocolVersion >= 2 && msg.SoccerGoalCounts is not null)
            writer.Write(msg.SoccerGoalCounts);
        if (ctx.FileVersion >= 18)
        {
            writer.Write(msg.FirstHalfTeamScoreGr);
            writer.Write(msg.FirstHalfTeamScoreBl);
        }
    }

    private static void WriteLengthPrefixedAscii(BinaryWriter writer, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        writer.Write(bytes.Length + 1);
        writer.Write(bytes);
        writer.Write((byte)0);
    }
}
