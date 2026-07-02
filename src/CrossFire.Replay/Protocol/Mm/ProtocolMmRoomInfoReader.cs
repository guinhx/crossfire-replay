using CrossFire.Replay.Interop;

namespace CrossFire.Replay.Protocol.Mm;

/// <summary>
/// Reads the fixed prefix of the room-info metadata block from replay metadata.
/// </summary>
public static class ProtocolMmRoomInfoReader
{
    public static int PrefixSize => ProtoMmRoomInfoPrefix.Size;

    public static ProtocolMmRoomInfoHeader? TryReadPrefix(ReadOnlySpan<byte> block)
    {
        if (!StructReader.TryRead(block, out ProtoMmRoomInfoPrefix prefix))
            return null;

        var rawLen = Math.Min(block.Length, PrefixSize);
        return new ProtocolMmRoomInfoHeader
        {
            RoomNumber = prefix.RoomNumber,
            RoomMaxUser = prefix.RoomMaxUser,
            RoomObserverMaxUser = prefix.RoomObserverMaxUser,
            MapType = prefix.MapType,
            IsFreeCamera = prefix.IsFreeCamera,
            GameRule = prefix.GameRule,
            WeaponType = prefix.WeaponType,
            ThrowWeapon = prefix.ThrowWeapon,
            BanAllThrowWeapon = prefix.BanAllThrowWeapon,
            ItemDropType = prefix.ItemDropType,
            WinCondition = prefix.WinCondition,
            WinGoal = prefix.WinGoal,
            EliteMode = prefix.EliteMode,
            IsPlayingEnter = prefix.IsPlayingEnter,
            TimeToRespawn = prefix.TimeToRespawn,
            InitialTp = prefix.InitialTp,
            FriendlyFire = prefix.FriendlyFire,
            RawPrefix = block[..rawLen].ToArray(),
        };
    }
}
