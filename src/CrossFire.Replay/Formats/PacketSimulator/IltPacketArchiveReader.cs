using CrossFire.Replay.Protocol.Lt;

namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Parses ILT packet archives from replay inner payloads.
/// Modern .cfn archives prepend a 4-byte tag before [u8 count][pad][u32 size][u32 timestamp][payload]...
/// </summary>
public static class IltPacketArchiveReader
{
    private const int MaxPacketPayloadSize = 5_000_000;
    private const int MaxPacketCount = 500_000;
    private const int MaxResyncBytes = 256;

    public static PacketSection<TimestampPacketRecord> ParseTimestampArchive(ReadOnlySpan<byte> archive) =>
        ParseTimestamp(archive, lenient: true);

    public static PacketSection<TimestampPacketRecord> ParseTimestampArchive(ReadOnlySpan<byte> archive, bool lenient) =>
        ParseTimestamp(archive, lenient);

    public static PacketSection<SpecialEffectPacketRecord> ParseSpecialEffectArchive(ReadOnlySpan<byte> archive) =>
        ParseSpecialEffect(archive, lenient: true);

    public static PacketSection<SpecialEffectPacketRecord> ParseSpecialEffectArchive(ReadOnlySpan<byte> archive, bool lenient) =>
        ParseSpecialEffect(archive, lenient);

    /// <summary>
    /// Drains a raw ILT stream without archive tag/count headers (middle blob and nested payloads).
    /// </summary>
    public static PacketSection<TimestampPacketRecord> ParseTimestampStream(ReadOnlySpan<byte> archive, int startOffset = 0)
    {
        if (startOffset >= archive.Length)
            return new PacketSection<TimestampPacketRecord>();

        var slice = archive[startOffset..];
        var packets = new List<TimestampPacketRecord>();
        var cursor = 0;
        DrainTimestampStream(slice, ref cursor, packets, lenient: true);

        return new PacketSection<TimestampPacketRecord>
        {
            Packets = packets,
            BytesConsumed = startOffset + cursor,
        };
    }

    /// <summary>
    /// Extracts all strict ILT timestamp packets from a byte span (e.g. binary snapshot body).
    /// Uses resync when the stream is misaligned or contains padding between packets.
    /// </summary>
    public static PacketSection<TimestampPacketRecord> ExtractAllTimestampPackets(ReadOnlySpan<byte> archive)
    {
        if (archive.Length < 9)
            return new PacketSection<TimestampPacketRecord>();

        var packets = new List<TimestampPacketRecord>();
        var cursor = 0;
        while (cursor < archive.Length - 8)
        {
            var start = cursor;
            if (TryReadTimestampPacket(archive, ref cursor, out var packet))
            {
                packets.Add(packet);
                continue;
            }

            cursor = start + 1;
            var next = FindNextStrictPacketOffset(archive, cursor, archive.Length);
            if (next < 0)
                break;

            cursor = next;
        }

        return new PacketSection<TimestampPacketRecord>
        {
            Packets = packets,
            BytesConsumed = archive.Length,
        };
    }

    public const int NestedPayloadThreshold = 2048;
    public const int MinNestedPacketCount = 2;

    /// <summary>
    /// Expands large timestamp payloads that embed nested ILT archives (e.g. game EOF first packet ~18 KB).
    /// </summary>
    public static IReadOnlyList<TimestampPacketRecord> ExpandNestedPayloads(IReadOnlyList<TimestampPacketRecord> packets)
    {
        if (packets.Count == 0)
            return packets;

        var expanded = new List<TimestampPacketRecord>(packets.Count);
        foreach (var packet in packets)
        {
            if (TryExpandNested(packet, out var inner))
            {
                expanded.AddRange(inner);
                continue;
            }

            expanded.Add(packet);
        }

        return expanded;
    }

    private static bool TryExpandNested(TimestampPacketRecord packet, out List<TimestampPacketRecord> expanded)
    {
        expanded = null!;
        if (packet.Payload.Length < NestedPayloadThreshold)
            return false;

        var inner = ParseTimestampStream(packet.Payload);
        if (inner.Packets.Count < MinNestedPacketCount)
            return false;

        var withMessageId = 0;
        foreach (var innerPacket in inner.Packets)
        {
            if (innerPacket.MessageId.HasValue)
                withMessageId++;
        }

        if (withMessageId < MinNestedPacketCount)
            return false;

        expanded = new List<TimestampPacketRecord>(inner.Packets.Count);
        foreach (var innerPacket in inner.Packets)
        {
            expanded.Add(new TimestampPacketRecord
            {
                PayloadSize = innerPacket.PayloadSize,
                Timestamp = innerPacket.Timestamp != 0 ? innerPacket.Timestamp : packet.Timestamp,
                MessageId = innerPacket.MessageId,
                Decoded = innerPacket.Decoded,
                Payload = innerPacket.Payload,
                SourceOffset = packet.SourceOffset,
            });
        }

        return true;
    }

    private static PacketSection<TimestampPacketRecord> ParseTimestamp(ReadOnlySpan<byte> archive, bool lenient)
    {
        if (archive.IsEmpty)
            return new PacketSection<TimestampPacketRecord>();

        var (packetStart, count) = ResolveHeader(archive);
        var packets = new List<TimestampPacketRecord>(Math.Min(count, 256));
        var cursor = packetStart;

        if (count > 0)
        {
            var limit = Math.Min(count, MaxPacketCount);
            for (var i = 0; i < limit; i++)
            {
                if (TryReadTimestampPacket(archive, ref cursor, out var packet))
                {
                    packets.Add(packet);
                    continue;
                }

                if (!lenient || !TryResyncTimestamp(archive, ref cursor))
                    break;
            }
        }
        else
        {
            DrainTimestampStream(archive, ref cursor, packets, lenient);
        }

        return new PacketSection<TimestampPacketRecord>
        {
            Packets = packets,
            BytesConsumed = cursor,
        };
    }

    private static void DrainTimestampStream(
        ReadOnlySpan<byte> archive,
        ref int cursor,
        List<TimestampPacketRecord> packets,
        bool lenient)
    {
        while (cursor + 8 <= archive.Length)
        {
            if (TryReadTimestampPacket(archive, ref cursor, out var packet))
            {
                packets.Add(packet);
                continue;
            }

            if (!lenient || !TryResyncTimestamp(archive, ref cursor))
                break;
        }
    }

    private static PacketSection<SpecialEffectPacketRecord> ParseSpecialEffect(ReadOnlySpan<byte> archive, bool lenient)
    {
        if (archive.IsEmpty)
            return new PacketSection<SpecialEffectPacketRecord>();

        if (LooksLikeSpecialEffectAssetBlob(archive))
            return ParseSpecialEffectAssetBlob(archive);

        var (packetStart, count) = ResolveHeader(archive);
        var packets = new List<SpecialEffectPacketRecord>(Math.Min(count, 64));
        var cursor = packetStart;

        if (count > 0)
        {
            var limit = Math.Min(count, MaxPacketCount);
            for (var i = 0; i < limit; i++)
            {
                if (TryReadSpecialEffectPacket(archive, ref cursor, out var packet))
                {
                    packets.Add(packet);
                    continue;
                }

                if (!lenient || !TryResyncSpecialEffect(archive, ref cursor))
                    break;
            }
        }
        else
        {
            DrainSpecialEffectStream(archive, ref cursor, packets, lenient);
        }

        return new PacketSection<SpecialEffectPacketRecord>
        {
            Packets = packets,
            BytesConsumed = cursor,
        };
    }

    private static void DrainSpecialEffectStream(
        ReadOnlySpan<byte> archive,
        ref int cursor,
        List<SpecialEffectPacketRecord> packets,
        bool lenient)
    {
        while (cursor + 10 <= archive.Length)
        {
            if (TryReadSpecialEffectPacket(archive, ref cursor, out var packet))
            {
                packets.Add(packet);
                continue;
            }

            if (!lenient || !TryResyncSpecialEffect(archive, ref cursor))
                break;
        }
    }

    /// <summary>Test/diagnostic hook for archive header detection.</summary>
    internal static (int PacketStart, int Count) ResolveHeaderForTests(ReadOnlySpan<byte> archive) =>
        ResolveHeader(archive);

    /// <summary>Finds the next strict ILT packet header in [from, toExclusive).</summary>
    public static int FindNextStrictPacketOffset(ReadOnlySpan<byte> archive, int from, int toExclusive)
    {
        var end = Math.Min(toExclusive, archive.Length - 9);
        for (var offset = Math.Max(0, from); offset <= end; offset++)
        {
            if (IsStrictTimestampPacketAt(archive, offset))
                return offset;
        }

        return -1;
    }

    /// <summary>Test/diagnostic hook for strict packet validation.</summary>
    internal static bool IsStrictTimestampPacketAtForTests(ReadOnlySpan<byte> archive, int offset) =>
        IsStrictTimestampPacketAt(archive, offset);

    private static (int PacketStart, int Count) ResolveHeader(ReadOnlySpan<byte> archive)
    {
        if (LooksLikePrintableAsciiPrefix(archive))
            return (archive.Length, 0);

        if (LooksLikeArchiveTag(ReadUInt32(archive, 0)))
            return ResolveTaggedHeader(archive);

        var legacyCount = ReadCountAt(archive, 0);
        if (IsPlausibleCount(legacyCount) && IsValidTimestampPacketAt(archive, 4))
            return (4, legacyCount);

        if (IsValidTimestampPacketAt(archive, 4))
            return (4, 0);

        return (4, 0);
    }

    private static (int PacketStart, int Count) ResolveTaggedHeader(ReadOnlySpan<byte> archive)
    {
        var packetStart = FindFirstTimestampPacketOffset(archive, 4, 16);
        if (packetStart < 0)
            return (8, 0);

        var u8Count = archive[4];
        if (u8Count is > 0 and <= 255)
            return (packetStart, u8Count);

        var u32Count = ReadCountAt(archive, 4);
        if (IsPlausibleCount(u32Count) && u32Count <= 10_000)
            return (packetStart, u32Count);

        return (packetStart, 0);
    }

    private static int FindFirstTimestampPacketOffset(ReadOnlySpan<byte> archive, int from, int to)
    {
        var end = Math.Min(to, archive.Length - 9);
        for (var start = from; start <= end; start++)
        {
            if (IsStrictTimestampPacketAt(archive, start))
                return start;
        }

        return -1;
    }

    private const int MaxResyncPayloadSize = 16_384;

    private static bool TryResyncTimestamp(ReadOnlySpan<byte> archive, ref int cursor)
    {
        var limit = Math.Min(cursor + MaxResyncBytes, archive.Length - 9);
        for (var probe = cursor + 1; probe <= limit; probe++)
        {
            if (!IsStrictTimestampPacketAt(archive, probe, MaxResyncPayloadSize))
                continue;

            cursor = probe;
            return true;
        }

        return false;
    }

    private static bool TryResyncSpecialEffect(ReadOnlySpan<byte> archive, ref int cursor)
    {
        var limit = Math.Min(cursor + MaxResyncBytes, archive.Length - 11);
        for (var probe = cursor + 1; probe <= limit; probe++)
        {
            if (!IsValidSpecialEffectPacketAt(archive, probe))
                continue;

            cursor = probe;
            return true;
        }

        return false;
    }

    private static bool IsStrictTimestampPacketAt(ReadOnlySpan<byte> archive, int offset, int maxPayloadSize = MaxPacketPayloadSize)
    {
        if (!IsValidTimestampPacketAt(archive, offset, maxPayloadSize))
            return false;

        var payload = archive.Slice(offset + 8, (int)ReadUInt32(archive, offset));
        return payload.Length >= 2 && LtMessageReader.TryPeekMessageId(payload, out _);
    }

    private static bool IsValidTimestampPacketAt(ReadOnlySpan<byte> archive, int offset, int maxPayloadSize = MaxPacketPayloadSize)
    {
        if (offset + 8 > archive.Length)
            return false;

        var size = ReadUInt32(archive, offset);
        if (size == 0 || size > (uint)maxPayloadSize)
            return false;

        if (offset + 8 + (int)size > archive.Length)
            return false;

        var timestamp = ReadUInt32(archive, offset + 4);
        if (timestamp > 10_000_000)
            return false;

        return true;
    }

    private static bool IsValidSpecialEffectPacketAt(ReadOnlySpan<byte> archive, int offset)
    {
        if (offset + 10 > archive.Length)
            return false;

        var size = ReadUInt32(archive, offset);
        if (size is > MaxPacketPayloadSize)
            return false;

        return offset + 10 + (int)size <= archive.Length;
    }

    private static bool LooksLikePrintableAsciiPrefix(ReadOnlySpan<byte> archive)
    {
        if (archive.Length < 4)
            return false;

        for (var i = 0; i < 4; i++)
        {
            if (archive[i] is < 0x20 or > 0x7E)
                return false;
        }

        return true;
    }

    private static bool LooksLikeSpecialEffectAssetBlob(ReadOnlySpan<byte> archive)
    {
        if (archive.Length < 16)
            return false;

        if (LooksLikePrintableAsciiPrefix(archive))
            return true;

        var ascii = 0;
        for (var i = 0; i < Math.Min(32, archive.Length); i++)
        {
            var b = archive[i];
            if (b is >= 0x20 and <= 0x7E)
                ascii++;
        }

        return ascii >= 12;
    }

    private static PacketSection<SpecialEffectPacketRecord> ParseSpecialEffectAssetBlob(ReadOnlySpan<byte> archive) =>
        new()
        {
            Packets = Array.Empty<SpecialEffectPacketRecord>(),
            BytesConsumed = archive.Length,
        };

    private static bool IsPlausibleCount(int count) => count is > 0 and <= MaxPacketCount;

    private static int ReadCountAt(ReadOnlySpan<byte> archive, int offset)
    {
        if (offset + 4 > archive.Length)
            return 0;

        return (int)ReadUInt32(archive, offset);
    }

    private static bool LooksLikeArchiveTag(uint value) =>
        value is not 0 and > MaxPacketPayloadSize;

    private static bool TryReadTimestampPacket(
        ReadOnlySpan<byte> archive,
        ref int cursor,
        out TimestampPacketRecord packet)
    {
        packet = null!;
        if (cursor + 8 > archive.Length)
            return false;

        var size = ReadUInt32(archive, cursor);
        var timestamp = ReadUInt32(archive, cursor + 4);
        cursor += 8;

        if (size is 0 or > MaxPacketPayloadSize || cursor + (int)size > archive.Length)
            return false;

        var payload = archive.Slice(cursor, (int)size).ToArray();
        cursor += (int)size;

        EMessageId? messageId = null;
        LtDecodedMessage? decoded = null;
        try
        {
            if (LtMessageReader.TryPeekMessageId(payload, out var id))
                messageId = id;
            decoded = LtMessageReader.TryDecode(payload);
        }
        catch (InvalidOperationException)
        {
            // Malformed LT payload; keep raw bytes without decoded view.
        }

        packet = new TimestampPacketRecord
        {
            PayloadSize = size,
            Timestamp = timestamp,
            Payload = payload,
            MessageId = messageId,
            Decoded = decoded,
            SourceOffset = cursor - 8 - (int)size,
        };
        return true;
    }

    private static bool TryReadSpecialEffectPacket(
        ReadOnlySpan<byte> archive,
        ref int cursor,
        out SpecialEffectPacketRecord packet)
    {
        packet = null!;
        if (cursor + 10 > archive.Length)
            return false;

        var size = ReadUInt32(archive, cursor);
        var timestamp = ReadUInt32(archive, cursor + 4);
        var objectId = ReadUInt16(archive, cursor + 8);
        cursor += 10;

        if (size is > MaxPacketPayloadSize || cursor + (int)size > archive.Length)
            return false;

        var payload = archive.Slice(cursor, (int)size).ToArray();
        cursor += (int)size;

        EMessageId? messageId = LtMessageReader.TryPeekMessageId(payload, out var id) ? id : null;

        packet = new SpecialEffectPacketRecord
        {
            PayloadSize = size,
            Timestamp = timestamp,
            ObjectId = objectId,
            Payload = payload,
            MessageId = messageId,
            Decoded = LtMessageReader.TryDecode(payload),
            SourceOffset = cursor - 10 - (int)size,
        };
        return true;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) =>
        data[offset]
        | ((uint)data[offset + 1] << 8)
        | ((uint)data[offset + 2] << 16)
        | ((uint)data[offset + 3] << 24);

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset) =>
        (ushort)(data[offset] | (data[offset + 1] << 8));
}
