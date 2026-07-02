namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Timestamp remap applied when writing ILT packet archives to disk.
/// </summary>
public static class PacketSimulatorTimestampRemapper
{
    public static uint RemapForWrite(uint logicalTimestamp, PacketSimulatorTimingState state)
    {
        if (state.UseWireTimestamps || !state.HasRemapAnchors)
            return logicalTimestamp;

        var initEnd = state.InitEndPacketTime;
        var firstGame = state.FirstGamePacketTime;

        if (logicalTimestamp <= firstGame)
        {
            if (logicalTimestamp <= initEnd)
                return logicalTimestamp;

            return initEnd + 1;
        }

        return initEnd + logicalTimestamp - firstGame + 1;
    }

    public static uint RemapFromWire(uint wireTimestamp, PacketSimulatorTimingState state)
    {
        if (state.UseWireTimestamps || !state.HasRemapAnchors)
            return wireTimestamp;

        var initEnd = state.InitEndPacketTime;
        var firstGame = state.FirstGamePacketTime;

        if (wireTimestamp <= initEnd)
            return wireTimestamp;

        if (wireTimestamp <= initEnd + 1)
            return firstGame;

        return firstGame + wireTimestamp - initEnd - 1;
    }

    /// <summary>
    /// Best-effort inference of remap anchors from parsed packet timestamps (wire format).
    /// </summary>
    public static PacketSimulatorTimingState InferFromWireTimestamps(
        IReadOnlyList<TimestampPacketRecord> spectatingPackets,
        IReadOnlyList<TimestampPacketRecord> gamePackets)
    {
        if (spectatingPackets.Count == 0 || gamePackets.Count == 0)
        {
            return new PacketSimulatorTimingState { UseWireTimestamps = true };
        }

        var initEnd = spectatingPackets.Max(static packet => packet.Timestamp);
        var firstGame = gamePackets.Min(static packet => packet.Timestamp);

        if (firstGame <= initEnd)
        {
            return new PacketSimulatorTimingState { UseWireTimestamps = true };
        }

        return new PacketSimulatorTimingState
        {
            InitEndPacketTime = initEnd,
            FirstGamePacketTime = firstGame,
            UseWireTimestamps = true,
        };
    }
}
