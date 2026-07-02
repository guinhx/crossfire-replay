using CrossFire.Replay.IO;
using CrossFire.Replay.Protocol.Messages;

namespace CrossFire.Replay.Protocol;

public static partial class ProtocolDeserializer
{
    private static Messages.ReplayMessage ReadMessageCore(SimpleProtocolId id, CfrBinaryReader reader, CfrReadContext ctx)
    {
        return id switch
        {
            SimpleProtocolId.MapInfo => ReadMapInfo(reader, ctx),
                       SimpleProtocolId.PlayerIn => ReadPlayerIn(reader, ctx),
            SimpleProtocolId.Fire => ReadFire(reader, ctx),
            SimpleProtocolId.Anim => ReadAnim(reader, ctx),
            SimpleProtocolId.RoundStart => ReadRoundStart(reader, ctx),
            SimpleProtocolId.RoundEnd => ReadRoundEnd(reader, ctx),
            SimpleProtocolId.PlayerOut => ReadPlayerOut(reader, ctx),
            SimpleProtocolId.SendC4Object => ReadSendC4Object(reader, ctx),
            SimpleProtocolId.GunDirRot => ReadGunDirRot(reader, ctx),
            SimpleProtocolId.Velocity => ReadVelocity(reader, ctx),
            SimpleProtocolId.PlayerDie => ReadPlayerDie(reader, ctx),
            SimpleProtocolId.PlayerRespawn => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            reader.ReadByte(); // reserved
            msg.Fields["Pos"] = reader.ReadVector3();
            if (msg.ProtocolVersion != 0) msg.Fields["Health"] = reader.ReadUInt16();
            if (msg.ProtocolVersion >= 2) msg.Fields["RageState"] = reader.ReadBoolean();
            }),
            SimpleProtocolId.ThrowGrenade => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["WeaponType"] = reader.ReadInt16();
            msg.Fields["ThrowPos"] = reader.ReadVector3();
            msg.Fields["Velocity"] = reader.ReadVector3();
            msg.Fields["KeyIndex"] = reader.ReadInt32();
            if (msg.ProtocolVersion != 0) msg.Fields["ElapsedTime"] = reader.ReadSingle();
            }),
            SimpleProtocolId.SetCurWeapon => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["WeaponType"] = reader.ReadInt16();
            msg.Fields["Ammo"] = reader.ReadInt32();
            msg.Fields["ExtraAmmo"] = reader.ReadInt32();
            if (msg.ProtocolVersion != 0) {
              msg.Fields["DualMag"] = reader.ReadBoolean();
              msg.Fields["DualMagAmmo"] = reader.ReadInt32();
            }
            }),
            SimpleProtocolId.PickupWeapon => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["WeaponType"] = reader.ReadInt16();
            msg.Fields["KeyIndex"] = reader.ReadInt32();
            }),
            SimpleProtocolId.SniperZoomIn => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["ZoomStep"] = reader.ReadByte();
            }),
            SimpleProtocolId.AllScores => ReadAllScores(reader, ctx),
            SimpleProtocolId.NanoGhostIndex => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["GhostIndex"] = reader.ReadSByte();
            msg.Fields["GhostType"] = reader.ReadByte();
            if (msg.ProtocolVersion != 0) msg.Fields["NanoMaxHP"] = reader.ReadUInt16();
            }),
            SimpleProtocolId.TeamChange => ReadTeamChange(reader, ctx),
            SimpleProtocolId.TeamChangeEnd => ReadTeamChangeEnd(reader, ctx),
            SimpleProtocolId.CountDownNoDeath => ReadCountDownNoDeath(reader, ctx),
            SimpleProtocolId.SoccerFadeStart => ReadSoccerFadeStart(reader, ctx),
            SimpleProtocolId.AckSwapSubWeapon => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.UseMountedWeapon => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.ReleaseMountedWeapon => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.TwistNeck => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.ExplosivePlant => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.C4PlantCountdown => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["Time"] = reader.ReadInt32();
            }),
            SimpleProtocolId.ChangeBullet => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["Ammo"] = reader.ReadInt32();
            }),
            SimpleProtocolId.ChangeDualMag => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.SetRageState => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["RageState"] = reader.ReadBoolean();
            }),
            SimpleProtocolId.GpsActivate => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.CancelSetupC4 => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.ClimbWall => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.ThrowMoneyMotionChangeNotify => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.RememberMeEscortSkill => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.RememberMePrecedeSkillMotion => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.RememberMeCeremonyEnd => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.ReplayDuplicationAnimId => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["PlayerIndex"] = reader.ReadByte();
            msg.Fields["DuplicationAnimId"] = reader.ReadInt32();
            }),
            SimpleProtocolId.RemoveWeaponProjectile => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["KeyIndex"] = reader.ReadInt32();
            }),
            SimpleProtocolId.FinishFakeNano => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.CreateWeaponProjectile => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["KeyIndex"] = reader.ReadInt32();
            msg.Fields["WeaponType"] = reader.ReadInt16();
            msg.Fields["ThrowPos"] = reader.ReadVector3();
            msg.Fields["Velocity"] = reader.ReadVector3();
            msg.Fields["ElapsedTime"] = reader.ReadSingle();
            }),
            SimpleProtocolId.SpecialAttackMotionNotify => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["MotionIndex"] = reader.ReadInt32();
            msg.Fields["TrackerId"] = reader.ReadByte();
            }),
            SimpleProtocolId.DropWeapon => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["WeaponType"] = reader.ReadInt16();
            msg.Fields["KeyIndex"] = reader.ReadInt32();
            msg.Fields["Pos"] = reader.ReadVector3();
            msg.Fields["Rot"] = reader.ReadVector3();
            msg.Fields["Ammo"] = reader.ReadInt32();
            msg.Fields["ExtraAmmo"] = reader.ReadInt32();
            if (msg.ProtocolVersion >= 2) msg.Fields["DualMagAmmo"] = reader.ReadInt32();
            }),
            SimpleProtocolId.C4BoomEffect => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["Pos"] = reader.ReadVector3();
            msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.C4CarriedBy => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.NanoGhostEveOfDeath => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.NanoTransform => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            if (msg.ProtocolVersion != 0) msg.Fields["NanoMaxHP"] = reader.ReadUInt16();
            }),
            SimpleProtocolId.DestroyDistributionBox => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["KeyIndex"] = reader.ReadInt32();
            }),
            SimpleProtocolId.NanoSpecialWeapon => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.Nano3NanoStep => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.NanoMaxHp => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            if (msg.ProtocolVersion != 0) msg.Fields["NanoMaxHP"] = reader.ReadUInt16();
            }),
            SimpleProtocolId.NanoChangeWeapon => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["WeaponType"] = reader.ReadInt16();
            }),
            SimpleProtocolId.HumanBossIndex => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["BossType"] = reader.ReadByte();
            }),
            SimpleProtocolId.EscapeSuccess => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["SuccessType"] = reader.ReadInt32();
            }),
            SimpleProtocolId.Spray => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["SprayIndex"] = reader.ReadInt32();
            msg.Fields["Pos"] = reader.ReadVector3();
            msg.Fields["Normal"] = reader.ReadVector3();
            }),
            SimpleProtocolId.HeroIndex => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["HeroType"] = reader.ReadInt16();
            }),
            SimpleProtocolId.FuryGhostIndex => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.SpecialKnifeWarheadBoom => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["Pos"] = reader.ReadVector3();
            }),
            SimpleProtocolId.DamageCampingRegionViewWarning => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["WarningLevel"] = reader.ReadInt32();
            }),
            SimpleProtocolId.DamageCampingRegionViewDamage => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.DamageCampingRegionViewWarningPause => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["WarningLevel"] = reader.ReadInt32();
            }),
            SimpleProtocolId.StartSetupC4 => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.SocialMotionChangeNotify => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["IsSocialMotion"] = reader.ReadBoolean();
            msg.Fields["MotionIndex"] = reader.ReadUInt16();
            msg.Fields["MotionWeaponIndex"] = reader.ReadInt16();
            if (msg.ProtocolVersion != 0) msg.Fields["InputCancel"] = reader.ReadBoolean();
            }),
            SimpleProtocolId.EscapeTeamMatchAiVipRemove => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["KeyIndex"] = reader.ReadInt32();
            }),
            SimpleProtocolId.EscapeTeamMatchAiVipHpInfo => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["KeyIndex"] = reader.ReadInt32();
            msg.Fields["Hp"] = reader.ReadInt32();
            }),
            SimpleProtocolId.EscapeTeamMatchAiVipAnimation => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["KeyIndex"] = reader.ReadInt32();
            msg.Fields["AnimIndex"] = reader.ReadUInt16();
            }),
            SimpleProtocolId.DelayTriggerPlaySound => ReadDelayTrigger(reader, ctx, SimpleProtocolId.DelayTriggerPlaySound),
            SimpleProtocolId.DelayTriggerShowMessage => ReadDelayTrigger(reader, ctx, SimpleProtocolId.DelayTriggerShowMessage),
            SimpleProtocolId.DelayTriggerPlayEffect => ReadDelayTrigger(reader, ctx, SimpleProtocolId.DelayTriggerPlayEffect),
            SimpleProtocolId.TriggerOnOffReplay => ReadTriggerOnOffReplay(reader, ctx),
            SimpleProtocolId.ToggleFxState => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["Data"] = reader.ReadBytesExact(0x7F);
            }),
            SimpleProtocolId.CharacterFunc => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["FuncType"] = reader.ReadInt32();
            }),
            SimpleProtocolId.PvAnim => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["AnimIndex"] = reader.ReadUInt16();
            msg.Fields["AnimRate"] = reader.ReadSingle();
            }),
            SimpleProtocolId.StanceChangeActivate => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            msg.Fields["Activated"] = reader.ReadBoolean();
            }),
            SimpleProtocolId.MountedWeaponSite => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["SiteIndex"] = reader.ReadInt32();
            msg.Fields["Pos"] = reader.ReadVector3();
            }),
            SimpleProtocolId.SoccerStartCount => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["Count"] = reader.ReadInt32();
            }),
            SimpleProtocolId.SoccerGoalIn => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["TeamIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.SoccerPickupBall => ReadGeneric(id, reader, ctx, msg =>
            {
                msg.Fields["CharacterIndex"] = reader.ReadSByte();
            }),
            SimpleProtocolId.ObjSfxMessage => throw new ReplayParseException(
                "Message id ObjSfxMessage is not supported by the reference client deserializer.",
                reader.Position),
            SimpleProtocolId.GameEnd => throw new ReplayParseException(
                "Message id GameEnd is not supported by the reference client deserializer.",
                reader.Position),
            SimpleProtocolId.Sprint => throw new ReplayParseException(
                "Message id Sprint is not supported by the reference client deserializer.",
                reader.Position),
            SimpleProtocolId.StealthGaugeInfo => throw new ReplayParseException(
                "Message id StealthGaugeInfo is not supported by the reference client deserializer.",
                reader.Position),
            SimpleProtocolId.Escape2TeamMatchMapRegion => throw new ReplayParseException(
                "Message id Escape2TeamMatchMapRegion is not supported by the reference client deserializer.",
                reader.Position),
            SimpleProtocolId.Escape2TeamMatchStage => throw new ReplayParseException(
                "Message id Escape2TeamMatchStage is not supported by the reference client deserializer.",
                reader.Position),
            SimpleProtocolId.DistributionBoxRespawn => ReadFixedStruct(id, reader, ctx, 48),
            SimpleProtocolId.UsedSkill => ReadFixedStruct(id, reader, ctx, 32),
            SimpleProtocolId.MapRegion => ReadFixedStruct(id, reader, ctx, 56),
            SimpleProtocolId.BreakableState => ReadFixedStruct(id, reader, ctx, 56),
            SimpleProtocolId.UccSpray => ReadFixedStruct(id, reader, ctx, 80),
            SimpleProtocolId.FireSubWeapon => ReadFixedStruct(id, reader, ctx, 64),
            SimpleProtocolId.TimeAndHealth => ReadFixedStruct(id, reader, ctx, 48),
            SimpleProtocolId.PlayerInfosFromObs => ReadFixedStruct(id, reader, ctx, 64),
            SimpleProtocolId.BombSites => ReadFixedStruct(id, reader, ctx, 144),
            SimpleProtocolId.SpecialKnifeWarheadShoot => ReadFixedStruct(id, reader, ctx, 72),
            SimpleProtocolId.GhostGaugeInfo => ReadFixedStruct(id, reader, ctx, 32),
            SimpleProtocolId.ChangeMovingScannerPoint => ReadFixedStruct(id, reader, ctx, 96),
            SimpleProtocolId.SprinklerState => ReadFixedStruct(id, reader, ctx, 88),
            SimpleProtocolId.DoorState => ReadFixedStruct(id, reader, ctx, 56),
            SimpleProtocolId.CreateSoccerBall => ReadFixedStruct(id, reader, ctx, 56),
            SimpleProtocolId.SoccerDropBall => ReadFixedStruct(id, reader, ctx, 64),
            SimpleProtocolId.SoccerResetBall => ReadFixedStruct(id, reader, ctx, 56),
            SimpleProtocolId.InitMountedWeapon => ReadFixedStruct(id, reader, ctx, 40),
            SimpleProtocolId.CharacterChange => ReadFixedStruct(id, reader, ctx, 56),
            SimpleProtocolId.CaptainSyncData => ReadFixedStruct(id, reader, ctx, 272),
            SimpleProtocolId.CreateGrenadeKnifeDropBox => ReadFixedStruct(id, reader, ctx, 56),
            SimpleProtocolId.DestroyGrenadeKnifeDropBox => ReadFixedStruct(id, reader, ctx, 56),
            SimpleProtocolId.NotifyGrenadeKnifeDropBox => ReadFixedStruct(id, reader, ctx, 48),
            SimpleProtocolId.ChangeVScope => ReadFixedStruct(id, reader, ctx, 32),
            SimpleProtocolId.Groggy => ReadFixedStruct(id, reader, ctx, 32),
            SimpleProtocolId.Tagging => ReadFixedStruct(id, reader, ctx, 32),
            SimpleProtocolId.FighterBomberStart => ReadFixedStruct(id, reader, ctx, 64),
            SimpleProtocolId.FighterBomberBoom => ReadFixedStruct(id, reader, ctx, 40),
            SimpleProtocolId.Nano4ExNanoGhostIndex => ReadFixedStruct(id, reader, ctx, 24),
            SimpleProtocolId.Nano4ExParasiteInfectionIndex => ReadFixedStruct(id, reader, ctx, 24),
            SimpleProtocolId.EscapeTeamMatchAiVipAdd => ReadFixedStruct(id, reader, ctx, 68),
            SimpleProtocolId.EscapeTeamMatchAiVipFire => ReadFixedStruct(id, reader, ctx, 192),
            SimpleProtocolId.EscapeTeamMatchAiVipDie => ReadFixedStruct(id, reader, ctx, 40),
            SimpleProtocolId.EscapeTeamMatchAiVipPosVelocity => ReadFixedStruct(id, reader, ctx, 56),
            SimpleProtocolId.EscapeTeamMatchHelicopter => ReadFixedStruct(id, reader, ctx, 72),
            SimpleProtocolId.EscapeTeamMatchWayPoint => ReadFixedStruct(id, reader, ctx, 80),
            SimpleProtocolId.EscapeTeamMatchMapRegion => ReadFixedStruct(id, reader, ctx, 72),
            SimpleProtocolId.EscapeTeamMatchStage => ReadFixedStruct(id, reader, ctx, 40),
            SimpleProtocolId.SetSpyState => ReadFixedStruct(id, reader, ctx, 32),
            SimpleProtocolId.RememberMeCeremony => ReadFixedStruct(id, reader, ctx, 88),
            _ => throw new ReplayParseException(
                $"Unsupported or unimplemented replay message id {id} (0x{(byte)id:X2}).",
                reader.Position),
        };
    }

    private static GenericReplayMessage ReadGeneric(
        SimpleProtocolId id,
        CfrBinaryReader reader,
        CfrReadContext ctx,
        Action<GenericReplayMessage> readBody)
    {
        var msg = ProtocolReadHelpers.Create(
            id,
            ProtocolReadHelpers.ReadVersion(reader, ctx),
            ProtocolReadHelpers.ReadTime(reader));
        readBody(msg);
        return msg;
    }

    private static GenericReplayMessage ReadFixedStruct(
        SimpleProtocolId id,
        CfrBinaryReader reader,
        CfrReadContext ctx,
        int structSize)
    {
        var start = reader.Position;
        var ver = ProtocolReadHelpers.ReadVersion(reader, ctx);
        var time = ProtocolReadHelpers.ReadTime(reader);
        var header = (int)(reader.Position - start);
        var tail = structSize - header;
        var msg = ProtocolReadHelpers.Create(id, ver, time);
        if (tail > 0)
            msg.Fields["Body"] = reader.ReadBytesExact(tail);
        return msg;
    }

    private static MapInfoMessage ReadMapInfo(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new MapInfoMessage { MessageId = SimpleProtocolId.MapInfo };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.MapIndex = reader.ReadInt16();
        msg.RoundType = reader.ReadInt32();
        msg.GameRule = reader.ReadInt32();
        msg.WinGoal = reader.ReadInt32();
        if (msg.ProtocolVersion != 0)
            msg.UseVvipSubMode = reader.ReadSByte();
        if (ctx.HasFeature(ReplayFeatureFlags.UseClanSystemRefine2nd))
        {
            msg.ClanServerMatch = reader.ReadBoolean();
            var blLen = reader.ReadInt32();
            msg.ClanNameBl = reader.ReadFixedString(blLen);
            var grLen = reader.ReadInt32();
            msg.ClanNameGr = reader.ReadFixedString(grLen);
        }
        else
        {
            msg.ClanNameBl = reader.ReadFixedString(0x22);
            msg.ClanNameGr = reader.ReadFixedString(0x22);
        }
        if (msg.ProtocolVersion >= 3)
            msg.IsNano4ExpansionMode = reader.ReadBoolean();
        if (ctx.FileVersion >= 18)
            msg.IsAutoSideChange = reader.ReadBoolean();
        return msg;
    }

    private static PlayerInMessage ReadPlayerIn(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new PlayerInMessage { MessageId = SimpleProtocolId.PlayerIn };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.CharacterIndex = reader.ReadSByte();
        msg.TeamIndex = reader.ReadSByte();
        msg.UserName = reader.ReadFixedString(0xE);
        for (var i = 0; i < 7; i++) msg.DressItemInfoIndex[i] = reader.ReadInt32();
        msg.CharItemInfoIndex = reader.ReadInt32();
        msg.IsDead = reader.ReadBoolean();
        msg.ZoomStep = reader.ReadByte();
        if (ctx.FileVersion >= 1)
            msg.UserId = ctx.FileVersion >= 30 ? reader.ReadInt64() : reader.ReadInt32();
        if (ctx.FileVersion >= 2)
            msg.IsObserver = reader.ReadBoolean();
        if (ctx.FileVersion >= 4)
            for (var i = 0; i < 4; i++) msg.ClanMark[i] = reader.ReadInt32();
        if (ctx.FileVersion >= 6)
            msg.SpecialKickWeaponIndex = reader.ReadInt16();
        if (msg.ProtocolVersion != 0)
            msg.ClanName = reader.ReadFixedString(0x1A);
        if (ctx.FileVersion >= 14 && msg.ProtocolVersion >= 2)
        {
            msg.MaxAccessory = reader.ReadByte();
            msg.AccessoryItemIndex = new int[msg.MaxAccessory];
            for (var i = 0; i < msg.MaxAccessory; i++)
                msg.AccessoryItemIndex[i] = reader.ReadInt32();
        }
        if (ctx.FileVersion >= 14)
            msg.SocialMotionWeaponIndex = reader.ReadInt16();
        if (ctx.FileVersion >= 16)
        {
            msg.FuncItemIndex = new int[50];
            for (var i = 0; i < 50; i++) msg.FuncItemIndex[i] = reader.ReadInt32();
        }
        if (ctx.FileVersion >= 22)
        {
            msg.FuncItemPeriodIndex = new int[50];
            for (var i = 0; i < 50; i++) msg.FuncItemPeriodIndex[i] = reader.ReadInt32();
        }
        if (ctx.FileVersion >= 18)
            msg.AutoSideChangeState = reader.ReadInt32();
        if (ctx.FileVersion >= 24)
            msg.ThrowMoneyWeaponIndex = reader.ReadInt16();
        if (ctx.FileVersion >= 31)
            msg.RememberMePrecedeWeaponIndex = reader.ReadInt16();
        if (ctx.HasFeature(ReplayFeatureFlags.UseCharacterSkinChangeSystem))
            msg.CharacterSkinIndex = reader.ReadSByte();
        else
            msg.CharacterSkinIndex = -1;
        return msg;
    }

    private static FireMessage ReadFire(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new FireMessage { MessageId = SimpleProtocolId.Fire };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.CharacterIndex = reader.ReadSByte();
        msg.GunRot = reader.ReadVector3();
        msg.ColorMuzzle = reader.ReadByte();
        if (msg.ProtocolVersion >= 2)
            msg.ShapeMuzzle = reader.ReadByte();
        msg.LeftShoot = reader.ReadBoolean();
        if (msg.ProtocolVersion != 0)
            msg.KnifeAttack = reader.ReadBoolean();
        if (msg.ProtocolVersion >= 3)
        {
            msg.ShotsPerAmmo = reader.ReadByte();
            msg.ShootThroughNum = reader.ReadBytesExact(8);
            msg.HitPos = new Vector3F[8][];
            msg.HitNodeType = new int[8][];
            for (var i = 0; i < 8; i++)
            {
                msg.HitPos[i] = new Vector3F[4];
                msg.HitNodeType[i] = new int[4];
                for (var j = 0; j < 4; j++)
                    msg.HitPos[i][j] = reader.ReadVector3();
            }
            for (var i = 0; i < 8; i++)
                for (var j = 0; j < 4; j++)
                    msg.HitNodeType[i][j] = reader.ReadInt32();
        }
        if (msg.ProtocolVersion >= 4)
        {
            msg.ShotPos = reader.ReadVector3();
            msg.ShotDir = reader.ReadVector3();
        }
        if (ctx.FileVersion >= 8)
            msg.ChargeShot = reader.ReadBoolean();
        return msg;
    }

    private static AnimMessage ReadAnim(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new AnimMessage { MessageId = SimpleProtocolId.Anim };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.AnimIndex = reader.ReadUInt16();
        msg.TrackerId = reader.ReadByte();
        msg.Looping = reader.ReadBoolean();
        msg.AnimRate = reader.ReadSingle();
        msg.CharacterIndex = reader.ReadSByte();
        msg.Idle = reader.ReadBoolean();
        if ((msg.ProtocolVersion == 0 && ctx.FileVersion >= 6) || msg.ProtocolVersion >= 3)
            msg.AnimSubType = reader.ReadByte();
        return msg;
    }

    private static SendC4ObjectMessage ReadSendC4Object(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new SendC4ObjectMessage { MessageId = SimpleProtocolId.SendC4Object };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.Planted = reader.ReadBoolean();
        msg.C4Pos = reader.ReadVector3();
        if (msg.ProtocolVersion != 0)
            msg.BombSiteIndex = reader.ReadInt16();
        if (msg.ProtocolVersion >= 2)
        {
            msg.ChangeC4Object = reader.ReadBoolean();
            msg.C4WeaponType = reader.ReadInt16();
        }

        return msg;
    }

    private static GunDirRotMessage ReadGunDirRot(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new GunDirRotMessage { MessageId = SimpleProtocolId.GunDirRot };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.CharacterIndex = reader.ReadSByte();
        msg.Rot = reader.ReadVector3();
        return msg;
    }

    private static VelocityMessage ReadVelocity(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new VelocityMessage { MessageId = SimpleProtocolId.Velocity };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.IncludeVelocity = reader.ReadBoolean();
        msg.Pos = reader.ReadVector3();
        msg.Vel = reader.ReadVector3();
        msg.CharacterIndex = reader.ReadSByte();
        msg.Teleport = reader.ReadBoolean();
        return msg;
    }

    private static PlayerOutMessage ReadPlayerOut(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new PlayerOutMessage { MessageId = SimpleProtocolId.PlayerOut };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.CharacterIndex = reader.ReadSByte();
        if (msg.ProtocolVersion != 0)
        {
            msg.ExitReason = reader.ReadByte();
            msg.FirstCode = reader.ReadUInt16();
            msg.SecondCode = reader.ReadUInt16();
        }

        return msg;
    }

    private static DelayTriggerMessage ReadDelayTrigger(
        CfrBinaryReader reader,
        CfrReadContext ctx,
        SimpleProtocolId id)
    {
        var msg = new DelayTriggerMessage { MessageId = id };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.TriggerId = reader.ReadInt32();
        return msg;
    }

    private static TriggerOnOffReplayMessage ReadTriggerOnOffReplay(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new TriggerOnOffReplayMessage { MessageId = SimpleProtocolId.TriggerOnOffReplay };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.Data = reader.ReadBytesExact(0x80);
        return msg;
    }

    private static TeamChangeMessage ReadTeamChange(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new TeamChangeMessage { MessageId = SimpleProtocolId.TeamChange };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        return msg;
    }

    private static TeamChangeEndMessage ReadTeamChangeEnd(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new TeamChangeEndMessage { MessageId = SimpleProtocolId.TeamChangeEnd };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        return msg;
    }

    private static CountDownNoDeathMessage ReadCountDownNoDeath(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new CountDownNoDeathMessage { MessageId = SimpleProtocolId.CountDownNoDeath };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        return msg;
    }

    private static SoccerFadeStartMessage ReadSoccerFadeStart(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new SoccerFadeStartMessage { MessageId = SimpleProtocolId.SoccerFadeStart };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        return msg;
    }

    private static RoundStartMessage ReadRoundStart(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new RoundStartMessage { MessageId = SimpleProtocolId.RoundStart };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        if (ctx.FileVersion >= 18)
            msg.AutoSideChangeState = reader.ReadInt32();
        return msg;
    }

    private static RoundEndMessage ReadRoundEnd(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new RoundEndMessage { MessageId = SimpleProtocolId.RoundEnd };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.WinTeamIndex = reader.ReadSByte();
        if (msg.ProtocolVersion != 0)
            msg.ClanWinTeamIndex = reader.ReadSByte();
        return msg;
    }

    private static PlayerDieMessage ReadPlayerDie(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new PlayerDieMessage { MessageId = SimpleProtocolId.PlayerDie };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.CharacterIndex = reader.ReadSByte();
        msg.AttackerIndex = reader.ReadSByte();
        msg.WeaponType = reader.ReadInt16();
        msg.HitNodeType = reader.ReadInt32();
        if (msg.ProtocolVersion != 0)
            msg.KnifeAttack = reader.ReadBoolean();
        if (ctx.FileVersion >= 8)
            msg.ChargeShot = reader.ReadBoolean();
        if (ctx.FileVersion >= 10)
            msg.DieDir = reader.ReadByte();
        if (ctx.FileVersion >= 15)
            msg.AiObjectIndex = reader.ReadSByte();
        return msg;
    }

    private static AllScoresMessage ReadAllScores(CfrBinaryReader reader, CfrReadContext ctx)
    {
        var msg = new AllScoresMessage { MessageId = SimpleProtocolId.AllScores };
        if (ctx.FileVersion >= 5)
            msg.ProtocolVersion = reader.ReadByte();
        msg.Timestamp = reader.ReadUInt32();
        msg.TeamScoreGr = reader.ReadUInt16();
        msg.TeamScoreBl = reader.ReadUInt16();
        var userIdBytes = ctx.FileVersion >= 30 ? 128 : 64;
        msg.UserIds = reader.ReadBytesExact(userIdBytes);
        msg.Kills = reader.ReadBytesExact(0x20);
        msg.Deaths = reader.ReadBytesExact(0x20);
        if (msg.ProtocolVersion >= 2)
            msg.SoccerGoalCounts = reader.ReadBytesExact(0x20);
        if (ctx.FileVersion >= 18)
        {
            msg.FirstHalfTeamScoreGr = reader.ReadUInt16();
            msg.FirstHalfTeamScoreBl = reader.ReadUInt16();
        }

        return msg;
    }
}
