using System.Runtime.InteropServices;
using CrossFire.Replay.Protocol.Game;

namespace CrossFire.Replay.Protocol.Mm;

/// <summary>
/// Client-side prefix of the room-info metadata block (2-byte aligned layout).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct ProtoMmRoomInfoPrefix
{
    public short RoomNumber;
    public short RoomMaxUser;
    public short RoomObserverMaxUser;
    public short MapType;
    [MarshalAs(UnmanagedType.I1)] public bool IsFreeCamera;
    public RoundType GameRule;
    public byte WeaponType;
    [MarshalAs(UnmanagedType.I1)] public bool ThrowWeapon;
    [MarshalAs(UnmanagedType.I1)] public bool BanAllThrowWeapon;
    public byte ItemDropType;
    public DeathMatchType WinCondition;
    public int WinGoal;
    [MarshalAs(UnmanagedType.I1)] public bool EliteMode;
    [MarshalAs(UnmanagedType.I1)] public bool IsPlayingEnter;
    public float TimeToRespawn;
    public ushort InitialTp;
    [MarshalAs(UnmanagedType.I1)] public bool FriendlyFire;

    public static int Size => Marshal.SizeOf<ProtoMmRoomInfoPrefix>();
}
