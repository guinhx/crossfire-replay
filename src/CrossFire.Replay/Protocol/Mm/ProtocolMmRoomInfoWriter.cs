using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Interop;
using CrossFire.Replay.Protocol.Game;

namespace CrossFire.Replay.Protocol.Mm;

/// <summary>
/// Writes the client room-info metadata prefix used in modern replay metadata blocks.
/// </summary>
public static class ProtocolMmRoomInfoWriter
{
    public static byte[] WritePrefix(ProtocolMmRoomInfoHeader header)
    {
        if (header.RawPrefix.Length >= ProtoMmRoomInfoPrefix.Size)
            return header.RawPrefix[..ProtoMmRoomInfoPrefix.Size].ToArray();

        var prefix = new ProtoMmRoomInfoPrefix
        {
            RoomNumber = header.RoomNumber,
            RoomMaxUser = header.RoomMaxUser,
            RoomObserverMaxUser = header.RoomObserverMaxUser,
            MapType = header.MapType,
            IsFreeCamera = header.IsFreeCamera,
            GameRule = header.GameRule,
            WeaponType = header.WeaponType,
            ThrowWeapon = header.ThrowWeapon,
            BanAllThrowWeapon = header.BanAllThrowWeapon,
            ItemDropType = header.ItemDropType,
            WinCondition = header.WinCondition,
            WinGoal = header.WinGoal,
            EliteMode = header.EliteMode,
            IsPlayingEnter = header.IsPlayingEnter,
            TimeToRespawn = header.TimeToRespawn,
            InitialTp = header.InitialTp,
            FriendlyFire = header.FriendlyFire,
        };

        return StructReader.Write(prefix);
    }

    public static ProtocolMmRoomInfoHeader FromPrefix(ProtoMmRoomInfoPrefix prefix, ReadOnlySpan<byte> rawPrefix) =>
        new()
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
            RawPrefix = rawPrefix[..Math.Min(rawPrefix.Length, ProtoMmRoomInfoPrefix.Size)].ToArray(),
        };

    /// <summary>
    /// Builds the 1024-byte modern metadata block from room info + header block.
    /// </summary>
    public static byte[] WriteMetadataBlock(
        byte[]? existingMetadata,
        ProtocolMmRoomInfoHeader? roomInfo,
        PacketSimulatorHeaderBlock? header,
        int blockSize = PacketSimulatorLayout.ModernMetadataBlockSize)
    {
        if (existingMetadata is not null && existingMetadata.Length >= blockSize)
            return existingMetadata[..blockSize].ToArray();

        var block = new byte[blockSize];
        if (existingMetadata is { Length: > 0 })
            existingMetadata.AsSpan(0, Math.Min(existingMetadata.Length, blockSize)).CopyTo(block);

        if (roomInfo is not null)
        {
            var prefix = WritePrefix(roomInfo);
            prefix.AsSpan(0, Math.Min(prefix.Length, blockSize)).CopyTo(block);
        }

        if (header?.RawBlock is { Length: >= PacketSimulatorLayout.HeaderBlockSize } rawBlock)
        {
            rawBlock.AsSpan(0, PacketSimulatorLayout.HeaderBlockSize)
                .CopyTo(block.AsSpan(0, PacketSimulatorLayout.HeaderBlockSize));
        }
        else if (header is not null)
        {
            WriteHeaderBlock(block, header);
        }

        return block;
    }

    private static void WriteHeaderBlock(byte[] block, PacketSimulatorHeaderBlock header)
    {
        if (block.Length < PacketSimulatorLayout.HeaderBlockSize)
            return;

        WriteUInt32(block, 0, header.DeathMatchTypeRaw);
        WriteUInt32(block, 4, header.GameGoal);
        WriteUInt16(block, 8, (ushort)header.MapId);
        block[10] = header.IsClanGame ? (byte)1 : (byte)0;
        WriteFixedString(block, 11, 36, header.ClanNameGr);
        WriteFixedString(block, 47, 36, header.ClanNameBl);
        block[83] = header.IsClanHalfTime ? (byte)1 : (byte)0;
        WriteInt32(block, 84, header.HalfTimeScoreGr);
        WriteInt32(block, 88, header.HalfTimeScoreBl);
    }

    private static void WriteFixedString(byte[] buffer, int offset, int maxLen, string value)
    {
        if (offset >= buffer.Length || maxLen <= 0)
            return;

        var bytes = System.Text.Encoding.ASCII.GetBytes(value);
        var copyLen = Math.Min(bytes.Length, maxLen - 1);
        Array.Copy(bytes, 0, buffer, offset, copyLen);
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteInt32(byte[] buffer, int offset, int value) =>
        WriteUInt32(buffer, offset, (uint)value);
}
