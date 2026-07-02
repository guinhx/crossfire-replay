namespace CrossFire.Replay.IO;

public sealed class CfrReadContext
{
    public int FileVersion { get; init; }
    public bool HasChecksum { get; init; }
    public IReadOnlyList<string> SpecDefines { get; init; } = Array.Empty<string>();
    public Formats.SimpleProtocol.SimpleProtocolReadOptions ReadOptions { get; init; } =
        Formats.SimpleProtocol.SimpleProtocolReadOptions.Strict;

    public bool HasFeature(string flag) =>
        SpecDefines.Any(d => string.Equals(d, flag, StringComparison.Ordinal));
}
