namespace CrossFire.Replay.Abstractions;

public interface IReplayContainerDecoder
{
    bool CanDecode(ReadOnlySpan<byte> fileBytes);
    ReplayContainerInfo Decode(ReadOnlySpan<byte> fileBytes);
}

public sealed class ReplayContainerInfo
{
    public required ReplayContainerKind Kind { get; init; }
    public required byte[] Payload { get; init; }
    public uint DeclaredOutputSize { get; init; }
    public uint HeaderWord { get; init; }
}
