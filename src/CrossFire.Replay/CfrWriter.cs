using CrossFire.Replay.Formats.SimpleProtocol;

namespace CrossFire.Replay;

/// <summary>
/// Writes CrossFire replay (.cfr) files. Supports lossless round-trip when messages retain their original payload bytes.
/// For unified .cfr / .cfn / .cfo support use <see cref="Core.ReplayWriteService"/>.
/// </summary>
public static class CfrWriter
{
    public static void WriteFile(string path, CfrDocument document, bool prependChecksum = false) =>
        Core.ReplayWriteService.Default.WriteFile(
            path,
            document,
            prependChecksum
                ? Abstractions.ReplayWriteOptions.PlainCfrWithChecksum
                : Abstractions.ReplayWriteOptions.PlainCfr);

    public static void Write(Stream stream, CfrDocument document, bool prependChecksum = false)
    {
        var bytes = SimpleProtocolWriter.WritePayload(
            document,
            prependChecksum
                ? SimpleProtocolWriteOptions.WithChecksum
                : SimpleProtocolWriteOptions.Default);
        stream.Write(bytes, 0, bytes.Length);
    }
}
