namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Writes ILT packet archives for replay inner payloads.
/// Supports legacy [u32 count][packets] and modern tagged headers with preserved padding.
/// </summary>
public static class IltPacketArchiveWriter
{
    public const int DefaultTaggedPacketStart = 8;
    public const int GameTaggedPacketStart = 7;

    public sealed class TaggedHeaderOptions
    {
        public uint Tag { get; init; }
        public byte Count { get; init; }
        public ReadOnlyMemory<byte> PrefixAfterCount { get; init; } = ReadOnlyMemory<byte>.Empty;
    }

    /// <summary>
    /// Rebuilds an archive by preserving the original header and any unconsumed tail bytes.
    /// </summary>
    public static byte[] RebuildPreservingLayout(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
            return Array.Empty<byte>();

        var section = IltPacketArchiveReader.ParseTimestampArchive(source);
        if (section.Packets.Count == 0)
            return source.ToArray();

        return BuildFromParts(source, section.Packets, section.BytesConsumed);
    }

    /// <summary>
    /// Rebuilds a special-effect archive, preserving ASCII asset blobs verbatim.
    /// </summary>
    public static byte[] RebuildSpecialEffectPreservingLayout(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
            return Array.Empty<byte>();

        var section = IltPacketArchiveReader.ParseSpecialEffectArchive(source);
        if (section.Packets.Count == 0)
            return source.ToArray();

        return BuildSpecialEffectFromParts(source, section.Packets, section.BytesConsumed);
    }

    /// <summary>Legacy inner v6: [u32 count][u32 size][u32 ts][payload]...</summary>
    public static byte[] WriteLegacyTimestampArchive(
        IReadOnlyList<TimestampPacketRecord> packets,
        PacketSimulatorTimingState? timing = null)
    {
        using var stream = new MemoryStream();
        WriteUInt32(stream, (uint)packets.Count);
        WriteTimestampPackets(stream, packets, timing);
        return stream.ToArray();
    }

    /// <summary>Modern tagged archive with explicit header prefix after the 4-byte tag.</summary>
    public static byte[] WriteTaggedTimestampArchive(
        TaggedHeaderOptions header,
        IReadOnlyList<TimestampPacketRecord> packets,
        PacketSimulatorTimingState? timing = null)
    {
        using var stream = new MemoryStream();
        WriteUInt32(stream, header.Tag);
        stream.WriteByte(header.Count);
        if (!header.PrefixAfterCount.IsEmpty)
            stream.Write(header.PrefixAfterCount.Span);

        WriteTimestampPackets(stream, packets, timing);

        var trailing = ComputeTaggedTrailingPadding(header, packets);
        if (trailing > 0)
            stream.Write(new byte[trailing]);

        return stream.ToArray();
    }

    /// <summary>Legacy inner v6 special-effect section.</summary>
    public static byte[] WriteLegacySpecialEffectArchive(
        IReadOnlyList<SpecialEffectPacketRecord> packets,
        PacketSimulatorTimingState? timing = null)
    {
        using var stream = new MemoryStream();
        WriteUInt32(stream, (uint)packets.Count);
        WriteSpecialEffectPackets(stream, packets, timing);
        return stream.ToArray();
    }

    /// <summary>Builds a modern tagged archive from layout hints and top-level packets.</summary>
    public static byte[] BuildTimestampArchiveFromLayout(ModernArchiveLayout? layout, uint defaultTag)
    {
        if (layout is null)
            return Array.Empty<byte>();

        if (layout.WirePieces.Count > 0)
            return PacketSimulatorModernLayoutBuilder.ComposeWirePieces(layout.WirePieces);

        var packets = layout.TopLevelPackets.Packets;
        if (packets.Count == 0)
            return layout.RawBytes.Length > 0 ? layout.RawBytes : Array.Empty<byte>();

        if (layout.TaggedHeader is { Tag: > 0 } header)
            return WriteTaggedTimestampArchive(header, packets);

        return WriteTaggedTimestampArchive(
            CreateDefaultTaggedHeader(defaultTag, packets.Count),
            packets);
    }

    /// <summary>Writes a null-terminated ASCII asset path blob for special-effect archives.</summary>
    public static byte[] WriteSpecialEffectAssetBlob(IReadOnlyList<SpecialEffectAssetRecord> assets)
    {
        if (assets.Count == 0)
            return Array.Empty<byte>();

        using var stream = new MemoryStream();
        foreach (var asset in assets)
        {
            if (string.IsNullOrWhiteSpace(asset.AssetPath))
                continue;

            var bytes = System.Text.Encoding.ASCII.GetBytes(asset.AssetPath);
            stream.Write(bytes);
            stream.WriteByte(0);
        }

        return stream.ToArray();
    }

    public static TaggedHeaderOptions CreateDefaultTaggedHeader(uint tag, int packetCount) =>
        tag switch
        {
            IltPacketArchiveTags.Game => new TaggedHeaderOptions
            {
                Tag = tag,
                Count = (byte)Math.Clamp(packetCount, 0, 255),
                PrefixAfterCount = Array.Empty<byte>(),
            },
            _ => new TaggedHeaderOptions
            {
                Tag = tag,
                Count = (byte)Math.Clamp(packetCount, 0, 255),
                PrefixAfterCount = new byte[] { 0x2F, 0x00, 0x00 },
            },
        };

  internal static byte[] BuildFromParts(
        ReadOnlySpan<byte> source,
        IReadOnlyList<TimestampPacketRecord> packets,
        int bytesConsumed)
    {
        var (packetStart, _) = IltPacketArchiveReader.ResolveHeaderForTests(source);
        packetStart = Math.Clamp(packetStart, 0, source.Length);

        using var stream = new MemoryStream(source.Length);
        if (packetStart > 0)
            stream.Write(source[..packetStart]);

        if (TryWriteSourceSlices(stream, source, packets, packetStart, out var writtenEnd))
        {
            if (writtenEnd < bytesConsumed)
                stream.Write(source[writtenEnd..bytesConsumed]);

            if (bytesConsumed < source.Length)
                stream.Write(source[bytesConsumed..]);

            return stream.ToArray();
        }

        WriteTimestampPackets(stream, packets);

        if (bytesConsumed < source.Length)
            stream.Write(source[bytesConsumed..]);

        return stream.ToArray();
    }

    internal static byte[] BuildSpecialEffectFromParts(
        ReadOnlySpan<byte> source,
        IReadOnlyList<SpecialEffectPacketRecord> packets,
        int bytesConsumed)
    {
        var packetStart = ResolveSpecialEffectPacketStart(source);
        packetStart = Math.Clamp(packetStart, 0, source.Length);

        using var stream = new MemoryStream(source.Length);
        if (packetStart > 0)
            stream.Write(source[..packetStart]);

        if (TryWriteSourceSlices(stream, source, packets, packetStart, out var writtenEnd))
        {
            if (writtenEnd < bytesConsumed)
                stream.Write(source[writtenEnd..bytesConsumed]);

            if (bytesConsumed < source.Length)
                stream.Write(source[bytesConsumed..]);

            return stream.ToArray();
        }

        WriteSpecialEffectPackets(stream, packets);

        if (bytesConsumed < source.Length)
            stream.Write(source[bytesConsumed..]);

        return stream.ToArray();
    }

    private static bool TryWriteSourceSlices(
        Stream stream,
        ReadOnlySpan<byte> source,
        IReadOnlyList<TimestampPacketRecord> packets,
        int packetStart,
        out int writtenEnd)
    {
        writtenEnd = packetStart;
        if (packets.Count == 0 || packets.Any(static packet => packet.SourceOffset < 0))
            return false;

        foreach (var packet in packets.OrderBy(static packet => packet.SourceOffset))
        {
            if (packet.SourceOffset > writtenEnd)
                stream.Write(source[writtenEnd..packet.SourceOffset]);

            var packetLength = 8 + packet.Payload.Length;
            if (packet.SourceOffset + packetLength > source.Length)
                return false;

            stream.Write(source.Slice(packet.SourceOffset, packetLength));
            writtenEnd = packet.SourceOffset + packetLength;
        }

        return true;
    }

    private static bool TryWriteSourceSlices(
        Stream stream,
        ReadOnlySpan<byte> source,
        IReadOnlyList<SpecialEffectPacketRecord> packets,
        int packetStart,
        out int writtenEnd)
    {
        writtenEnd = packetStart;
        if (packets.Count == 0 || packets.Any(static packet => packet.SourceOffset < 0))
            return false;

        foreach (var packet in packets.OrderBy(static packet => packet.SourceOffset))
        {
            if (packet.SourceOffset > writtenEnd)
                stream.Write(source[writtenEnd..packet.SourceOffset]);

            var packetLength = 10 + packet.Payload.Length;
            if (packet.SourceOffset + packetLength > source.Length)
                return false;

            stream.Write(source.Slice(packet.SourceOffset, packetLength));
            writtenEnd = packet.SourceOffset + packetLength;
        }

        return true;
    }

    internal static TaggedHeaderOptions ExtractTaggedHeader(ReadOnlySpan<byte> source)
    {
        var (packetStart, count) = IltPacketArchiveReader.ResolveHeaderForTests(source);
        if (packetStart < 5 || source.Length < 5)
            return new TaggedHeaderOptions();

        var tag = ReadUInt32(source, 0);
        var prefixLength = packetStart - 5;
        var prefix = prefixLength > 0
            ? source.Slice(5, prefixLength).ToArray()
            : Array.Empty<byte>();

        return new TaggedHeaderOptions
        {
            Tag = tag,
            Count = (byte)Math.Clamp(count, 0, 255),
            PrefixAfterCount = prefix,
        };
    }

    private static int ResolveSpecialEffectPacketStart(ReadOnlySpan<byte> source)
    {
        if (source.Length < 4)
            return 0;

        if (IltPacketArchiveReader.ResolveHeaderForTests(source).PacketStart is var taggedStart and > 0 and < 64)
            return taggedStart;

        var legacyCount = ReadUInt32(source, 0);
        if (legacyCount is > 0 and <= 500_000 && IsValidSpecialEffectPacketAt(source, 4))
            return 4;

        return 4;
    }

    private static int ComputeTaggedTrailingPadding(TaggedHeaderOptions header, IReadOnlyList<TimestampPacketRecord> packets)
    {
        var packetStart = 5 + header.PrefixAfterCount.Length;
        if (header.Tag == IltPacketArchiveTags.Game && packetStart == 5)
            return GameTaggedPacketStart - packetStart;

        if (header.Tag == IltPacketArchiveTags.Spectating && packetStart == 5)
            return DefaultTaggedPacketStart - packetStart;

        return 0;
    }

    private static void WriteTimestampPackets(
        Stream stream,
        IReadOnlyList<TimestampPacketRecord> packets,
        PacketSimulatorTimingState? timing = null)
    {
        foreach (var packet in packets)
        {
            var payload = packet.Payload;
            var size = packet.PayloadSize != 0 ? packet.PayloadSize : (uint)payload.Length;
            var timestamp = timing is null
                ? packet.Timestamp
                : PacketSimulatorTimestampRemapper.RemapForWrite(packet.Timestamp, timing);
            WriteUInt32(stream, size);
            WriteUInt32(stream, timestamp);
            stream.Write(payload);
        }
    }

    private static void WriteSpecialEffectPackets(
        Stream stream,
        IReadOnlyList<SpecialEffectPacketRecord> packets,
        PacketSimulatorTimingState? timing = null)
    {
        foreach (var packet in packets)
        {
            var payload = packet.Payload;
            var size = packet.PayloadSize != 0 ? packet.PayloadSize : (uint)payload.Length;
            var timestamp = timing is null
                ? packet.Timestamp
                : PacketSimulatorTimestampRemapper.RemapForWrite(packet.Timestamp, timing);
            WriteUInt32(stream, size);
            WriteUInt32(stream, timestamp);
            WriteUInt16(stream, packet.ObjectId);
            stream.Write(payload);
        }
    }

    private static bool IsValidSpecialEffectPacketAt(ReadOnlySpan<byte> archive, int offset)
    {
        if (offset + 10 > archive.Length)
            return false;

        var size = ReadUInt32(archive, offset);
        return size <= 5_000_000 && offset + 10 + (int)size <= archive.Length;
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        buffer[0] = (byte)value;
        buffer[1] = (byte)(value >> 8);
        buffer[2] = (byte)(value >> 16);
        buffer[3] = (byte)(value >> 24);
        stream.Write(buffer);
    }

    private static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        buffer[0] = (byte)value;
        buffer[1] = (byte)(value >> 8);
        stream.Write(buffer);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) =>
        data[offset]
        | ((uint)data[offset + 1] << 8)
        | ((uint)data[offset + 2] << 16)
        | ((uint)data[offset + 3] << 24);
}
