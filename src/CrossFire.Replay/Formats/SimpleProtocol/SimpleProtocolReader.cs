using System.Text;
using CrossFire.Replay;
using CrossFire.Replay.Compression;
using CrossFire.Replay.IO;
using CrossFire.Replay.Protocol;

namespace CrossFire.Replay.Formats.SimpleProtocol;

/// <summary>
/// Reads plain .cfr files (cfrversion stream).
/// </summary>
public static class SimpleProtocolReader
{
    private const string VersionPrefix = "cfrversion";
    public const int CurrentFileVersion = 32;
    private const int ChecksumHeaderSize = 0x20;

    public static bool LooksLikePlainCfr(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 10 && data.StartsWith("cfrversion"u8))
            return true;

        return data.Length >= ChecksumHeaderSize + 10
               && data.Slice(ChecksumHeaderSize).StartsWith("cfrversion"u8);
    }

    public static CfrDocument ReadPayload(ReadOnlySpan<byte> payload, string? sourcePath = null) =>
        ReadPayload(payload, SimpleProtocolReadOptions.Strict, sourcePath);

    public static CfrDocument ReadPayload(
        ReadOnlySpan<byte> payload,
        SimpleProtocolReadOptions options,
        string? sourcePath = null)
    {
        bool? checksumValid = null;
        if (payload.Length >= ChecksumHeaderSize + 10 && !payload.StartsWith("cfrversion"u8))
        {
            checksumValid = ReplayChecksum.TryValidateBlock(
                payload[ChecksumHeaderSize..],
                payload[..ChecksumHeaderSize]);
        }

        using var stream = new MemoryStream(payload.ToArray(), writable: false);
        var doc = Read(stream, sourcePath, options);
        if (!checksumValid.HasValue)
            return doc;

        return new CfrDocument
        {
            FileVersion = doc.FileVersion,
            HasChecksum = doc.HasChecksum,
            ChecksumValid = checksumValid.Value,
            SpecDefines = doc.SpecDefines,
            Messages = doc.Messages,
            ParseErrors = doc.ParseErrors,
            SourcePath = doc.SourcePath,
        };
    }

    public static CfrDocument Read(Stream stream, string? sourcePath = null) =>
        Read(stream, sourcePath, SimpleProtocolReadOptions.Strict);

    public static CfrDocument Read(Stream stream, string? sourcePath, SimpleProtocolReadOptions options)
    {
        using var reader = new CfrBinaryReader(stream, leaveOpen: true);
        var (fileVersion, hasChecksum, specDefines, dataOffset) = ReadHeader(reader);

        var ctx = new CfrReadContext
        {
            FileVersion = fileVersion,
            HasChecksum = hasChecksum,
            SpecDefines = specDefines,
            ReadOptions = options,
        };

        reader.Seek(dataOffset, SeekOrigin.Begin);
        var (messages, parseErrors) = ReadMessages(reader, ctx);

        return new CfrDocument
        {
            FileVersion = fileVersion,
            HasChecksum = hasChecksum,
            SpecDefines = specDefines,
            Messages = messages,
            ParseErrors = parseErrors,
            SourcePath = sourcePath ?? string.Empty,
        };
    }

    private static (int fileVersion, bool hasChecksum, List<string> specDefines, long dataOffset) ReadHeader(
        CfrBinaryReader reader)
    {
        var specDefines = new List<string>();
        var hasChecksum = false;
        var fileVersion = 0;
        long dataOffset;

        if (TryReadVersionText(reader, 0, out fileVersion))
        {
            dataOffset = 14;
        }
        else
        {
            hasChecksum = true;
            if (!TryReadVersionText(reader, ChecksumHeaderSize, out fileVersion))
                throw new ReplayParseException("Missing cfrversion header.", reader.Position);

            dataOffset = ChecksumHeaderSize + 14;
        }

        reader.Seek(dataOffset, SeekOrigin.Begin);

        if (fileVersion >= 32)
        {
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytesExact(length);
                specDefines.Add(Encoding.ASCII.GetString(bytes));
            }

            dataOffset = reader.Position;
        }

        return (fileVersion, hasChecksum, specDefines, dataOffset);
    }

    private static bool TryReadVersionText(CfrBinaryReader reader, long offset, out int fileVersion)
    {
        fileVersion = 0;
        reader.Seek(offset, SeekOrigin.Begin);
        var headerBytes = reader.ReadBytesExact(14);
        var headerText = Encoding.ASCII.GetString(headerBytes);

        if (!headerText.StartsWith(VersionPrefix, StringComparison.Ordinal))
            return false;

        var versionText = headerText.Substring(VersionPrefix.Length);
        if (!int.TryParse(versionText, out fileVersion))
            throw new ReplayParseException($"Invalid CFR version text '{headerText}'.", offset);

        return true;
    }

    private static (List<Protocol.Messages.ReplayMessage> Messages, List<CfrParseError> Errors) ReadMessages(
        CfrBinaryReader reader,
        CfrReadContext ctx)
    {
        var messages = new List<Protocol.Messages.ReplayMessage>();
        var errors = new List<CfrParseError>();

        while (reader.Position < reader.Length)
        {
            var messageOffset = reader.Position;
            var messageIdValue = reader.ReadByte();
            var messageId = (SimpleProtocolId)messageIdValue;

            try
            {
                var message = Protocol.ProtocolDeserializer.ReadMessage(messageId, reader, ctx);
                messages.Add(message);
            }
            catch (ReplayParseException) when (ctx.ReadOptions.SkipUnsupportedMessageIds)
            {
                // Message id byte already consumed; reference client returns nullptr for these ids.
            }
            catch (ReplayParseException ex) when (ctx.ReadOptions.SkipMessageParseErrors)
            {
                errors.Add(new CfrParseError
                {
                    Offset = messageOffset,
                    MessageId = messageId,
                    Message = ex.Message,
                });
                break;
            }
            catch (EndOfStreamException ex)
            {
                throw new ReplayParseException(
                    $"Unexpected end of stream while reading message {messageId} (0x{messageIdValue:X2}).",
                    reader.Position,
                    ex);
            }
        }

        return (messages, errors);
    }
}
