namespace CrossFire.Replay.Protocol.Lt;

/// <summary>
/// Shared client-to-server packet bitstream prefix and optional packet-hash trailer.
/// </summary>
internal static class LtCsPacketReader
{
    public readonly record struct CsPacketHeader(uint DummyData, byte ScrambledSeq, int PacketSeqIndex);

    public static CsPacketHeader ReadHeader(LtBitstreamReader reader)
    {
        var dummy = reader.ReadBits(5);
        var scrambled = reader.ReadUInt8();
        var seq = (16 * scrambled) | ((scrambled >> 4) & 0xF);
        return new CsPacketHeader(dummy, scrambled, seq);
    }

    /// <summary>
    /// Consumes the optional 8-bit integrity trailer when present.
    /// </summary>
    public static void SkipPacketHash(LtBitstreamReader reader)
    {
        if (!reader.HasRemaining)
            return;

        _ = reader.ReadUInt8();
    }
}
