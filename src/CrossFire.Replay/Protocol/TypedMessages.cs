using CrossFire.Replay.IO;
using CrossFire.Replay.Protocol;
using CrossFire.Replay.Protocol.Game;
using CrossFire.Replay.Protocol.Messages;

namespace CrossFire.Replay.Protocol;

internal static class ProtocolReadHelpers
{
    public static GenericReplayMessage Create(SimpleProtocolId id, byte protocolVersion, uint timestamp) =>
        new()
        {
            MessageId = id,
            ProtocolVersion = protocolVersion,
            Timestamp = timestamp,
        };

    public static byte ReadVersion(CfrBinaryReader reader, CfrReadContext ctx) =>
        reader.ReadProtocolVersion(ctx);

    public static uint ReadTime(CfrBinaryReader reader) => reader.ReadUInt32();

    public static void ReadClanNamesRefined(
        CfrBinaryReader reader,
        CfrReadContext ctx,
        GenericReplayMessage message,
        bool readLegacyBlFirst)
    {
        if (ctx.HasFeature(ReplayFeatureFlags.UseClanSystemRefine2nd))
        {
            message.Fields["ClanServerMatch"] = reader.ReadBoolean();
            var blLen = reader.ReadInt32();
            message.Fields["ClanNameBL"] = reader.ReadFixedString(blLen);
            var grLen = reader.ReadInt32();
            message.Fields["ClanNameGR"] = reader.ReadFixedString(grLen);
        }
        else
        {
            message.Fields["ClanNameBL"] = reader.ReadFixedString(0x22);
            message.Fields["ClanNameGR"] = reader.ReadFixedString(0x22);
        }
    }
}

public sealed class GenericReplayMessage : Messages.ReplayMessage
{
    public Dictionary<string, object?> Fields { get; } = new(StringComparer.Ordinal);
}

public sealed class MapInfoMessage : Messages.ReplayMessage
{
    /// <summary>Round type (<see cref="RoundType"/>).</summary>
    public int RoundType { get; set; }

    /// <summary>Game rule / deathmatch type (<see cref="DeathMatchType"/>).</summary>
    public int GameRule { get; set; }

    public RoundType RoundMode => (RoundType)RoundType;
    public DeathMatchType MatchWinCondition => (DeathMatchType)GameRule;

    public short MapIndex { get; set; }
    public int WinGoal { get; set; }
    public sbyte UseVvipSubMode { get; set; }
    public bool ClanServerMatch { get; set; }
    public string ClanNameBl { get; set; } = string.Empty;
    public string ClanNameGr { get; set; } = string.Empty;
    public bool IsNano4ExpansionMode { get; set; }
    public bool IsAutoSideChange { get; set; }
}

public sealed class PlayerInMessage : Messages.ReplayMessage
{
    public sbyte CharacterIndex { get; set; }
    public sbyte TeamIndex { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int[] DressItemInfoIndex { get; set; } = new int[7];
    public int CharItemInfoIndex { get; set; }
    public bool IsDead { get; set; }
    public byte ZoomStep { get; set; }
    public long UserId { get; set; }
    public bool IsObserver { get; set; }
    public int[] ClanMark { get; set; } = new int[4];
    public short SpecialKickWeaponIndex { get; set; }
    public string ClanName { get; set; } = string.Empty;
    public byte MaxAccessory { get; set; }
    public int[] AccessoryItemIndex { get; set; } = Array.Empty<int>();
    public short SocialMotionWeaponIndex { get; set; }
    public int[] FuncItemIndex { get; set; } = Array.Empty<int>();
    public int[] FuncItemPeriodIndex { get; set; } = Array.Empty<int>();
    public int AutoSideChangeState { get; set; }
    public short ThrowMoneyWeaponIndex { get; set; }
    public short RememberMePrecedeWeaponIndex { get; set; }
    public sbyte CharacterSkinIndex { get; set; } = -1;
}

public sealed class FireMessage : Messages.ReplayMessage
{
    public sbyte CharacterIndex { get; set; }
    public Vector3F GunRot { get; set; }
    public byte ColorMuzzle { get; set; }
    public byte ShapeMuzzle { get; set; }
    public bool LeftShoot { get; set; }
    public bool KnifeAttack { get; set; }
    public byte ShotsPerAmmo { get; set; }
    public byte[] ShootThroughNum { get; set; } = new byte[8];
    public Vector3F[][] HitPos { get; set; } = Array.Empty<Vector3F[]>();
    public int[][] HitNodeType { get; set; } = Array.Empty<int[]>();
    public Vector3F ShotPos { get; set; }
    public Vector3F ShotDir { get; set; }
    public bool ChargeShot { get; set; }
}

public sealed class AnimMessage : Messages.ReplayMessage
{
    public ushort AnimIndex { get; set; }
    public byte TrackerId { get; set; }
    public bool Looping { get; set; }
    public float AnimRate { get; set; }
    public sbyte CharacterIndex { get; set; }
    public bool Idle { get; set; }
    public byte AnimSubType { get; set; }
}

public sealed class RoundStartMessage : Messages.ReplayMessage
{
    public int AutoSideChangeState { get; set; }
}

public sealed class RoundEndMessage : Messages.ReplayMessage
{
    public sbyte WinTeamIndex { get; set; }
    public sbyte ClanWinTeamIndex { get; set; }
}

public sealed class PlayerDieMessage : Messages.ReplayMessage
{
    public sbyte CharacterIndex { get; set; }
    public sbyte AttackerIndex { get; set; }
    public short WeaponType { get; set; }
    public int HitNodeType { get; set; }
    public bool KnifeAttack { get; set; }
    public bool ChargeShot { get; set; }
    public byte DieDir { get; set; }
    public sbyte AiObjectIndex { get; set; }
}

public sealed class VelocityMessage : Messages.ReplayMessage
{
    public bool IncludeVelocity { get; set; }
    public Vector3F Pos { get; set; }
    public Vector3F Vel { get; set; }
    public sbyte CharacterIndex { get; set; }
    public bool Teleport { get; set; }
}

public sealed class GunDirRotMessage : Messages.ReplayMessage
{
    public sbyte CharacterIndex { get; set; }
    public Vector3F Rot { get; set; }
}

public sealed class SendC4ObjectMessage : Messages.ReplayMessage
{
    public bool Planted { get; set; }
    public Vector3F C4Pos { get; set; }
    public short BombSiteIndex { get; set; }
    public bool ChangeC4Object { get; set; }
    public short C4WeaponType { get; set; }
}

public sealed class PlayerOutMessage : Messages.ReplayMessage
{
    public sbyte CharacterIndex { get; set; }
    public byte ExitReason { get; set; }
    public ushort FirstCode { get; set; }
    public ushort SecondCode { get; set; }
}

public sealed class DelayTriggerMessage : Messages.ReplayMessage
{
    public int TriggerId { get; set; }
}

public sealed class TriggerOnOffReplayMessage : Messages.ReplayMessage
{
    public byte[] Data { get; set; } = new byte[0x80];
}

public sealed class AllScoresMessage : Messages.ReplayMessage
{
    public ushort TeamScoreGr { get; set; }
    public ushort TeamScoreBl { get; set; }
    public byte[] UserIds { get; set; } = Array.Empty<byte>();
    public byte[] Kills { get; set; } = Array.Empty<byte>();
    public byte[] Deaths { get; set; } = Array.Empty<byte>();
    public byte[]? SoccerGoalCounts { get; set; }
    public ushort FirstHalfTeamScoreGr { get; set; }
    public ushort FirstHalfTeamScoreBl { get; set; }
}

public sealed class TeamChangeMessage : Messages.ReplayMessage;

public sealed class TeamChangeEndMessage : Messages.ReplayMessage;

public sealed class CountDownNoDeathMessage : Messages.ReplayMessage;

public sealed class SoccerFadeStartMessage : Messages.ReplayMessage;
