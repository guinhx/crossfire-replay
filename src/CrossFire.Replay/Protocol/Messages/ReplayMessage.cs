namespace CrossFire.Replay.Protocol.Messages;

public abstract class ReplayMessage
{
    public required SimpleProtocolId MessageId { get; init; }
    public byte ProtocolVersion { get; set; }
    public uint Timestamp { get; set; }

    /// <summary>
    /// Exact on-disk payload bytes (without message-id byte).
    /// </summary>
    public byte[] Payload { get; internal set; } = Array.Empty<byte>();
}
