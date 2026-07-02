namespace CrossFire.Replay;

using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Compression;

public sealed class CfrDocument : IReplayDocument
{
    public ReplayFormatKind FormatKind => ReplayFormatKind.SimpleProtocol;

    public int FileVersion { get; init; }
    public bool HasChecksum { get; init; }

    /// <summary>Null when file has no checksum prefix; false when checksum does not match body.</summary>
    public bool? ChecksumValid { get; init; }

    public IReadOnlyList<string> SpecDefines { get; init; } = Array.Empty<string>();
    public IReadOnlyList<Protocol.Messages.ReplayMessage> Messages { get; init; } = Array.Empty<Protocol.Messages.ReplayMessage>();
    public IReadOnlyList<CfrParseError> ParseErrors { get; init; } = Array.Empty<CfrParseError>();
    public string SourcePath { get; init; } = string.Empty;

    public Protocol.Messages.ReplayMessage? MapInfo =>
        Messages.FirstOrDefault(m => m.MessageId == Protocol.SimpleProtocolId.MapInfo);
}

public sealed class CfrParseError
{
    public long Offset { get; init; }
    public Protocol.SimpleProtocolId? MessageId { get; init; }
    public string Message { get; init; } = string.Empty;
}
