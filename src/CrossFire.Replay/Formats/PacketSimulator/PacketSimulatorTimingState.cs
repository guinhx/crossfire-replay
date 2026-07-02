namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Timing anchors used by ILT archive timestamp remap on write.
/// </summary>
public sealed class PacketSimulatorTimingState
{
    public uint InitEndPacketTime { get; init; }
    public uint FirstGamePacketTime { get; init; }

    /// <summary>
    /// When true, packet timestamps are already in on-disk wire format and must not be remapped on write.
    /// </summary>
    public bool UseWireTimestamps { get; init; } = true;

    public bool HasRemapAnchors =>
        InitEndPacketTime != 0
        && FirstGamePacketTime != 0
        && FirstGamePacketTime > InitEndPacketTime;
}
