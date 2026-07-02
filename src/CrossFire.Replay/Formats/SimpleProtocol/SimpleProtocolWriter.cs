using System.Text;
using CrossFire.Replay.IO;
using CrossFire.Replay.Protocol;
using CrossFire.Replay.Protocol.Messages;

namespace CrossFire.Replay.Formats.SimpleProtocol;

/// <summary>
/// Writes plain .cfr bodies (cfrversion stream).
/// </summary>
public static class SimpleProtocolWriter
{
    public static byte[] WritePayload(CfrDocument document) =>
        WritePayload(document, SimpleProtocolWriteOptions.Default);

    public static byte[] WritePayload(CfrDocument document, SimpleProtocolWriteOptions options)
    {
        using var stream = new MemoryStream();
        WriteBody(stream, document);

        var body = stream.ToArray();
        if (!options.PrependChecksum)
            return body;

        var checksum = Compression.ReplayChecksum.ComputeBlock(body);
        var file = new byte[checksum.Length + body.Length];
        checksum.CopyTo(file, 0);
        body.CopyTo(file, checksum.Length);
        return file;
    }

    public static void WriteBody(Stream stream, CfrDocument document)
    {
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        var versionText = $"cfrversion{document.FileVersion:D4}";
        writer.Write(Encoding.ASCII.GetBytes(versionText));

        if (document.FileVersion >= 32)
        {
            var flags = document.SpecDefines.Count > 0
                ? document.SpecDefines
                : ReplayFeatureFlags.DefaultWriteFlags;

            writer.Write(flags.Count);
            foreach (var flag in flags)
            {
                var bytes = Encoding.ASCII.GetBytes(flag);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }

        var ctx = new CfrReadContext
        {
            FileVersion = document.FileVersion,
            SpecDefines = document.SpecDefines,
        };

        foreach (var message in document.Messages)
        {
            writer.Write((byte)message.MessageId);
            if (message.Payload.Length > 0)
            {
                writer.Write(message.Payload);
            }
            else
            {
                ProtocolSerializer.WritePayload(writer, message, ctx);
            }
        }
    }
}

public sealed class SimpleProtocolWriteOptions
{
    public bool PrependChecksum { get; init; }

    public static SimpleProtocolWriteOptions Default { get; } = new();

    public static SimpleProtocolWriteOptions WithChecksum { get; } = new() { PrependChecksum = true };
}
