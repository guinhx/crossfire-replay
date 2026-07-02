using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.SimpleProtocol;

namespace CrossFire.Replay;
/// <summary>
/// Backward-compatible entry point for plain .cfr files.
/// For unified .cfr / .cfn / .cfo support use <see cref="ReplayService"/>.
/// </summary>
public static class CfrReader
{
    public const int CurrentFileVersion = SimpleProtocolReader.CurrentFileVersion;

    public static CfrDocument ReadFile(string path) => Read(ReplayService.Default.Read(path));

    public static CfrDocument ReadFile(string path, SimpleProtocolReadOptions options)
    {
        var bytes = File.ReadAllBytes(path);
        return SimpleProtocolReader.ReadPayload(bytes, options, path);
    }

    public static CfrDocument Read(Stream stream, string? sourcePath = null)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Read(ReplayService.Default.Read(ms.ToArray(), sourcePath));
    }

    public static CfrDocument Read(byte[] fileBytes, string? sourcePath = null) =>
        Read(ReplayService.Default.Read(fileBytes, sourcePath));

    private static CfrDocument Read(IReplayDocument document)
    {
        if (document is CfrDocument cfr)
            return cfr;

        throw new ReplayParseException(
            $"Expected plain .cfr (SimpleProtocol), got {document.FormatKind}.",
            0);
    }
}
