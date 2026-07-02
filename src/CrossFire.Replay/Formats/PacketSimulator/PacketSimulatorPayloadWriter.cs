using System.Text;

namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Writes PacketSimulator inner payload (header block and ILT archives).
/// </summary>
public static class PacketSimulatorPayloadWriter
{
    public static byte[] BuildInnerPayload(PacketSimulatorReplayDocument document)
    {
        if (document.InnerFormat == PacketSimulatorInnerFormat.ModernV2026)
            return PacketSimulatorModernWriter.BuildInnerPayload(document);

        if (document.InnerFormat != PacketSimulatorInnerFormat.LegacyV2022)
            return document.InnerPayload;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        WriteUInt32(writer, document.ContainerVersion != 0
            ? document.ContainerVersion
            : PacketSimulatorLayout.ContainerVersion);
        WriteUInt32(writer, document.ContainerMagic != 0
            ? document.ContainerMagic
            : PacketSimulatorLayout.ContainerMagic);
        WriteUInt32(writer, PacketSimulatorLayout.HeaderBlockSize);

        var headerBlock = document.Header?.RawBlock is { Length: PacketSimulatorLayout.HeaderBlockSize } raw
            ? raw
            : BuildHeaderBlock(document.Header);
        writer.Write(headerBlock);

        var roomDeclared = document.SpectatingRoomInfoDeclaredSize ?? document.SpectatingRoomInfo.Length;
        WriteUInt32(writer, (uint)roomDeclared);
        writer.Write(document.SpectatingRoomInfo);
        if (document.SpectatingRoomInfo.Length < roomDeclared)
            writer.Write(new byte[roomDeclared - document.SpectatingRoomInfo.Length]);

        writer.Write(IltPacketArchiveWriter.WriteLegacyTimestampArchive(document.SpectatingPackets.Packets, document.TimingState));
        writer.Write(IltPacketArchiveWriter.WriteLegacyTimestampArchive(document.GamePackets.Packets, document.TimingState));
        writer.Write(IltPacketArchiveWriter.WriteLegacySpecialEffectArchive(document.SpecialEffectPackets.Packets, document.TimingState));

        return stream.ToArray();
    }

    private static byte[] BuildHeaderBlock(PacketSimulatorHeaderBlock? header)
    {
        var block = new byte[PacketSimulatorLayout.HeaderBlockSize];
        if (header is null)
            return block;

        WriteUInt32(block, 0, header.DeathMatchTypeRaw);
        WriteUInt32(block, 4, header.GameGoal);
        WriteUInt16(block, 8, (ushort)header.MapId);
        block[10] = header.IsClanGame ? (byte)1 : (byte)0;
        WriteFixedString(block, 11, 36, header.ClanNameGr);
        WriteFixedString(block, 47, 36, header.ClanNameBl);
        block[83] = header.IsClanHalfTime ? (byte)1 : (byte)0;
        WriteInt32(block, 84, header.HalfTimeScoreGr);
        WriteInt32(block, 88, header.HalfTimeScoreBl);
        return block;
    }

    private static void WriteFixedString(byte[] buffer, int offset, int maxLen, string value)
    {
        if (offset >= buffer.Length || maxLen <= 0)
            return;

        var bytes = Encoding.ASCII.GetBytes(value);
        var copyLen = Math.Min(bytes.Length, maxLen - 1);
        Array.Copy(bytes, 0, buffer, offset, copyLen);
    }

    private static void WriteUInt32(BinaryWriter writer, uint value)
    {
        writer.Write((byte)value);
        writer.Write((byte)(value >> 8));
        writer.Write((byte)(value >> 16));
        writer.Write((byte)(value >> 24));
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
