using CrossFire.Replay.Protocol;

namespace CrossFire.Replay.Protocol.Lt;

public abstract record LtDecodedMessage(EMessageId MessageId);

public sealed record LtAnimDecoded(
    ushort AnimIndex,
    byte TrackerId,
    bool Looping,
    float AnimRate,
    sbyte CharacterIndex,
    bool Idle,
    byte AnimSubType) : LtDecodedMessage(EMessageId.MsgCsAnim);

public sealed record LtScAnimDecoded(
    ushort AnimIndex,
    byte TrackerId,
    bool Looping,
    float AnimRate,
    sbyte CharacterIndex,
    bool Idle,
    byte AnimSubType) : LtDecodedMessage(EMessageId.MsgScAnim);

public sealed record LtVelocityDecoded(
    bool IncludeVelocity,
    Vector3F Pos,
    Vector3F Vel,
    sbyte CharacterIndex,
    bool Teleport) : LtDecodedMessage(EMessageId.MsgScVelocity);

public sealed record LtGunDirRotDecoded(
    sbyte CharacterIndex,
    Vector3F Rot) : LtDecodedMessage(EMessageId.MsgScGunDirRot);

public sealed record LtRoundStartDecoded(
    int AutoSideChangeState) : LtDecodedMessage(EMessageId.MsgScRoundStart);

public sealed record LtRoundEndDecoded(
    sbyte WinTeamIndex,
    sbyte ClanWinTeamIndex) : LtDecodedMessage(EMessageId.MsgScRoundEnd);

public sealed record LtPlayerDieDecoded(
    sbyte CharacterIndex,
    sbyte AttackerIndex,
    short WeaponType,
    int HitNodeType,
    bool KnifeAttack) : LtDecodedMessage(EMessageId.MsgScPlayerDie);

public sealed record LtFireDecoded(
    sbyte CharacterIndex,
    Vector3F GunRot,
    byte ColorMuzzle,
    bool LeftShoot) : LtDecodedMessage(EMessageId.MsgScFire);

public sealed record LtPlayerScoreDecoded(
    sbyte CharacterIndex,
    ushort Score) : LtDecodedMessage(EMessageId.MsgScPlayerScore);

public sealed record LtSendC4ObjectDecoded(
    bool Planted,
    Vector3F C4Pos,
    short BombSiteIndex) : LtDecodedMessage(EMessageId.MsgScSendC4Object);

public sealed record LtPlayerOutDecoded(
    sbyte CharacterIndex,
    byte ExitReason) : LtDecodedMessage(EMessageId.MsgScPlayerOut);

public sealed record LtBombSiteInfo(
    byte AreaNumber,
    Vector3F Position,
    Vector3F Dimensions);

public sealed record LtBombSitesDecoded(
    IReadOnlyList<LtBombSiteInfo> Sites) : LtDecodedMessage(EMessageId.MsgScBombSites);

public sealed record LtWorldPropsDecoded(
    uint FarZ,
    Vector3F BackgroundColor,
    bool FogEnable,
    Vector3F FogColor,
    uint FogNearZ,
    uint FogFarZ,
    bool SkyFogEnable,
    uint SkyFogNearZ,
    uint SkyFogFarZ,
    float SkyScale) : LtDecodedMessage(EMessageId.MsgScWorldProps);

public sealed record LtUnknownDecoded(
    EMessageId Id,
    int PayloadBytes) : LtDecodedMessage(Id)
{
    public string NativeName => EMessageIdCatalog.GetName((ushort)Id);
}

public sealed record LtScoreDecoded(
    ushort Health,
    uint ArmorPoint,
    ushort NumKill,
    ushort NumDeath,
    ushort CurrentGameMoney) : LtDecodedMessage(EMessageId.MsgScScore);

public sealed record LtRoundTimeLeftDecoded(
    float RoundTimeLeftSeconds,
    IReadOnlyList<ushort> TeamHealth) : LtDecodedMessage(EMessageId.MsgScRoundTimeLeft);

public sealed record LtAllScoresPlayerDecoded(
    byte ClientIndex,
    byte CharacterIndex,
    byte TeamIndex,
    ushort Health,
    ushort Kills,
    ushort Deaths,
    short GoalInCount,
    uint TotalDamage,
    ushort Ping,
    ulong UserId,
    ushort EscapeCount,
    uint AiKillScore,
    byte SoldierType);

public sealed record LtAllScoresDecoded(
    byte TeamCount,
    IReadOnlyList<ushort> TeamScores,
    byte PlayerCount,
    IReadOnlyList<LtAllScoresPlayerDecoded> Players,
    ulong AceUserId,
    ulong TopEscapeUserId,
    int TotalKills,
    int KillCountsForWin,
    int CurrentLevel,
    ushort ActiveBomberScore,
    IReadOnlyList<ushort> FirstHalfTeamScores) : LtDecodedMessage(EMessageId.MsgScAllScores);

public sealed record LtThrowGrenadeDecoded(
    Vector3F ThrowPosition,
    Vector3F Velocity,
    ushort WeaponType,
    byte CharacterIndex,
    bool PreviousCreated,
    float CreateTime,
    float CreateBoomTime,
    float BoomDurationTime,
    bool BoomStatus,
    uint ObjectId) : LtDecodedMessage(EMessageId.MsgScThrowGrenade);

public sealed record LtShotInfoDecoded(
    ushort WeaponIndex,
    ushort CurrentAmmo,
    ushort MagazineAmmo,
    ushort FullAmmo,
    ushort MaxAmmo,
    ushort WeaponType,
    bool AddAmmoByVvipBuff,
    bool AmmoPlusByHeadShot,
    bool OneAmmoInMagWeapon,
    bool ProcessThrowMagazineAmmo,
    ushort ThrowMagazineAmmo,
    ushort VvipBuffAmmoCount,
    ushort LinkWeaponAmmoCount) : LtDecodedMessage(EMessageId.MsgScShotInfo);

public sealed record LtSetCurWeaponDecoded(
    byte CharacterIndex,
    ushort ObjectId,
    ushort WeaponObjectId,
    ushort WeaponType,
    ushort SelectedFuncItemIndex) : LtDecodedMessage(EMessageId.MsgScSetCurWeapon);

public sealed record LtLadderAreaDecoded(
    ushort ObjectId,
    Vector3F Position,
    Vector3F Rotation,
    Vector3F Dimensions,
    byte LadderType) : LtDecodedMessage(EMessageId.MsgScLadderArea);

public sealed record LtPlayerRespawnDecoded(
    byte CharacterIndex,
    byte CheckCharacterIndex,
    ushort ObjectId,
    Vector3F Position,
    int BaseLifeCount,
    byte UserCharacterIndex,
    byte TargetCharacterIndex,
    bool EnableRageState) : LtDecodedMessage(EMessageId.MsgScPlayerRespawn);

public sealed record LtPlayerInDecoded(
    byte CharacterIndex,
    byte TeamIndex,
    ushort ObjectId,
    string UserName,
    byte RepCount,
    byte CurrentRankType,
    byte ScoreboardIndex) : LtDecodedMessage(EMessageId.MsgScPlayerIn);

public sealed record LtCs1SecondPassedDecoded() : LtDecodedMessage(EMessageId.MsgCs1SecondPassed);

public sealed record LtHiddenTeamIndexDecoded(sbyte HiddenTeamIndex) : LtDecodedMessage(EMessageId.MsgScHiddenTeamIndex);

public sealed record LtDamageDecoded(
    sbyte AttackerIndex,
    Vector3F From,
    ushort Damage,
    byte DamageType,
    short WeaponType,
    bool IsAiSuperArmorActivated,
    byte HitNodeType) : LtDecodedMessage(EMessageId.MsgScDamage);

public sealed record LtSetWeaponSlotDecoded(
    byte SelectedSlotIndex,
    IReadOnlyList<short> WeaponTypes,
    IReadOnlyList<bool> ZeroWeaponStates,
    bool ChangeBagNum,
    byte BagNum,
    float RapidChangeBuff,
    int ChangeWeaponState,
    byte Motion,
    IReadOnlyList<IReadOnlyList<LtVvipWeaponCustomColor>> CustomSetInfo,
    bool HasExtendedTrailingData) : LtDecodedMessage(EMessageId.MsgScSetWeaponSlot);

public sealed record LtHitInfoDecoded(
    int HitInfoBlobBytes,
    float HitRates,
    float HeadKillRates,
    int SummingDamages) : LtDecodedMessage(EMessageId.MsgScHitInfo);

public sealed record LtVvipWeaponCustomColor(byte Red, byte Green, byte Blue, byte Material);

public sealed record LtCsPacketHeaderFields(uint DummyData, int PacketSeqIndex);

public sealed record LtCsVelAndRotDecoded(
    LtCsPacketHeaderFields Header,
    bool IncludeVelocity,
    Vector3F? Velocity,
    LtCompWorldPos CompPosition,
    Vector3F? DecodedPosition,
    bool Teleport,
    bool Stand,
    ushort ElapsedTime,
    bool Duck,
    Vector3F RepurposeVelocity,
    bool Jump,
    float FrameTime,
    bool StandOnCheck,
    int RespawnedFlag) : LtDecodedMessage(EMessageId.MsgCsVelAndRot);

public sealed record LtCsReqDropWeaponDecoded(
    LtCsPacketHeaderFields Header,
    byte Slot,
    ushort AmmoLeftInMagazine,
    ushort AmmoLeftInTotal) : LtDecodedMessage(EMessageId.MsgCsReqDropWeapon);

public sealed record LtCsChangeWeaponDecoded(
    EMessageId WireMessageId,
    LtCsPacketHeaderFields Header,
    byte Slot,
    ushort AmmoLeftInMagazine,
    ushort AmmoLeftTotal,
    byte CurSlot,
    uint ChangeWeaponTime,
    bool SelectSlotEx,
    int LastRecvShotInfoWeaponIndex,
    bool CommandChangeOldWeapon) : LtDecodedMessage(WireMessageId);

public sealed record LtCsLinkWeaponDecoded(
    EMessageId WireMessageId,
    LtCsPacketHeaderFields Header,
    byte CurSlot,
    int CurWeaponSelect,
    byte Motion,
    byte NextLinkWeaponExpandIdx) : LtDecodedMessage(WireMessageId);

public sealed record LtCsFrogJumpDecoded(
    LtCsPacketHeaderFields Header) : LtDecodedMessage(EMessageId.MsgCsFrogJump);

public sealed record LtCsLandingStateDecoded(
    LtCsPacketHeaderFields Header,
    float VelocityY,
    int LandType) : LtDecodedMessage(EMessageId.MsgCsLandingState);
