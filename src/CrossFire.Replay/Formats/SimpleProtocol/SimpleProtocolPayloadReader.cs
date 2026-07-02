using CrossFire.Replay.Abstractions;

namespace CrossFire.Replay.Formats.SimpleProtocol;

public sealed class SimpleProtocolPayloadReader : IReplayPayloadReader
{
    public ReplayFormatKind FormatKind => ReplayFormatKind.SimpleProtocol;

    public bool CanRead(ReadOnlySpan<byte> payload) =>
        SimpleProtocolReader.LooksLikePlainCfr(payload);

    public IReplayDocument Read(ReadOnlySpan<byte> payload, string? sourcePath = null) =>
        SimpleProtocolReader.ReadPayload(payload, sourcePath);
}
