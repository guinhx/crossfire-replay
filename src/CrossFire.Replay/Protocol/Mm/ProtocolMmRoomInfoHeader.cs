using CrossFire.Replay.Protocol.Game;

namespace CrossFire.Replay.Protocol.Mm;

/// <summary>
/// Prefix of the room-info metadata block used in replay metadata.
/// Full block includes a slot table and exceeds 1024 bytes.
/// </summary>
public sealed class ProtocolMmRoomInfoHeader
{
    public short RoomNumber { get; init; }
    public short RoomMaxUser { get; init; }
    public short RoomObserverMaxUser { get; init; }
    public short MapType { get; init; }
    public bool IsFreeCamera { get; init; }
    public RoundType GameRule { get; init; }
    public byte WeaponType { get; init; }
    public bool ThrowWeapon { get; init; }
    public bool BanAllThrowWeapon { get; init; }
    public byte ItemDropType { get; init; }
    public DeathMatchType WinCondition { get; init; }
    public int WinGoal { get; init; }
    public bool EliteMode { get; init; }
    public bool IsPlayingEnter { get; init; }
    public float TimeToRespawn { get; init; }
    public ushort InitialTp { get; init; }
    public bool FriendlyFire { get; init; }
    public byte[] RawPrefix { get; init; } = Array.Empty<byte>();
}
