namespace CrossFire.Replay.Protocol.Game;

/// <summary>Client deathmatch mode enum. Stored in map info as round/game rule.</summary>
public enum DeathMatchType : int
{
    None = 0x0,
    Kill = 0x1,
    Time = 0x2,
    TeamChange = 0x3,
    Tutorial = 0x4,
    Easy = 0x5,
    Normal = 0x6,
    Hard = 0x7,
    Ai = 0xA,
    Hidden2Elimination = 0xB,
    Hidden2Injection = 0xC,
    CasualScore = 0xD,
    Kings = 0xE,
    Ai3Easy = 0x10,
    Ai3Normal = 0x11,
    Ai3Hard = 0x12,
    Ai3Asceticism = 0x13,
    Ai3Nightmare = 0x14,
    WeaponMaster = 0x15,
    BattleTdKill = 0x16,
    BattleOccupationScore = 0x17,
    TeamDeathMatchWeaponMasterScore = 0x18,
    PveRankMatch = 0x19,
    AiExtraBossTower = 0x1A,
    Max = 0x1B,
}
