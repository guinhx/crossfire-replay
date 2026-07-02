namespace CrossFire.Replay.Abstractions;

public interface IReplayPayloadReader
{
    ReplayFormatKind FormatKind { get; }
    bool CanRead(ReadOnlySpan<byte> payload);
    IReplayDocument Read(ReadOnlySpan<byte> payload, string? sourcePath = null);
}
