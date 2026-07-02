namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Writes the modern PacketSimulator middle blob (descriptor + segmented ILT timeline + binary snapshot).
/// </summary>
public static class PacketSimulatorMiddleBlobWriter
{
    public const int DefaultHeaderSize = 64;

    /// <summary>
    /// Rebuilds the middle blob by preserving header, inter-segment padding, and trailing bytes.
    /// </summary>
    public static byte[] RebuildPreservingLayout(ReadOnlySpan<byte> middle)
    {
        if (middle.IsEmpty)
            return Array.Empty<byte>();

        var streamOffset = PacketSimulatorMiddleBlobReader.FindStreamOffset(middle);
        var segments = PacketSimulatorMiddleBlobReader.EnumerateSegmentRanges(middle, streamOffset);
        if (segments.Count == 0)
            return middle.ToArray();

        using var stream = new MemoryStream(middle.Length);
        var writtenEnd = 0;

        foreach (var segment in segments)
        {
            if (segment.StartOffset > writtenEnd)
                stream.Write(middle[writtenEnd..segment.StartOffset]);

            stream.Write(middle[segment.StartOffset..segment.EndOffset]);
            writtenEnd = segment.EndOffset;
        }

        if (writtenEnd < middle.Length)
            stream.Write(middle[writtenEnd..]);

        return stream.ToArray();
    }

    /// <summary>
    /// Rebuilds the middle blob from decomposed layout pieces and archive sizes.
    /// </summary>
    public static byte[] BuildFromLayout(
        IReadOnlyList<MiddleBlobLayoutPiece> layout,
        uint spectatingArchiveSize,
        uint gameArchiveSize,
        uint specialEffectArchiveSize)
    {
        if (layout.Count == 0)
            return Array.Empty<byte>();

        using var stream = new MemoryStream();
        var isFirstPiece = true;

        foreach (var piece in layout)
        {
            var data = piece.Data;
            if (isFirstPiece && data.Length >= 32)
            {
                data = PacketSimulatorModernLayoutBuilder.PatchArchiveSizes(
                    data,
                    spectatingArchiveSize,
                    gameArchiveSize,
                    specialEffectArchiveSize);
                isFirstPiece = false;
            }

            stream.Write(data);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Rebuilds the middle blob from a parsed replay document.
    /// </summary>
    public static byte[] Build(PacketSimulatorReplayDocument document)
    {
        if (document.MiddleBlob.Length > 0)
            return RebuildPreservingLayout(document.MiddleBlob);

        if (document.MiddleBlobLayout.Count > 0)
        {
            var sizes = ResolveArchiveSizes(document);
            return BuildFromLayout(document.MiddleBlobLayout, sizes.Spectating, sizes.Game, sizes.SpecialEffect);
        }

        return BuildFromParts(document);
    }

    /// <summary>
    /// Synthesizes a middle blob from header metadata and ILT packet sections (no layout padding).
    /// </summary>
    public static byte[] BuildFromParts(PacketSimulatorReplayDocument document)
    {
        MiddleBlobHeader? header = document.MiddleBlobHeader;
        if (header is null && document.ModernDescriptor is { } descriptor)
        {
            header = new MiddleBlobHeader
            {
                MapTypeId = descriptor.MapTypeId,
                SpectatingArchiveSize = descriptor.SpectatingArchiveSize,
                GameArchiveSize = descriptor.GameArchiveSize,
                SpecialEffectArchiveSize = descriptor.SpecialEffectArchiveSize,
                MapLabel = descriptor.MapLabel,
            };
        }

        var streamOffset = document.MiddleBlobStreamOffset > 0
            ? document.MiddleBlobStreamOffset
            : DefaultHeaderSize;

        var sizes = ResolveArchiveSizes(document);

        using var stream = new MemoryStream();
        WriteHeader(stream, header, streamOffset, sizes.Spectating, sizes.Game, sizes.SpecialEffect);
        WriteTimestampStream(stream, document.MiddleBlobPackets.Packets);
        return stream.ToArray();
    }

    private static (uint Spectating, uint Game, uint SpecialEffect) ResolveArchiveSizes(PacketSimulatorReplayDocument document)
    {
        if (document.ModernDescriptor is { } descriptor)
        {
            return (descriptor.SpectatingArchiveSize, descriptor.GameArchiveSize, descriptor.SpecialEffectArchiveSize);
        }

        if (document.MiddleBlobHeader is { } header)
        {
            return (header.SpectatingArchiveSize, header.GameArchiveSize, header.SpecialEffectArchiveSize);
        }

        return (0, 0, 0);
    }

    private static void WriteHeader(
        Stream stream,
        MiddleBlobHeader? header,
        int headerSize,
        uint spectatingArchiveSize,
        uint gameArchiveSize,
        uint specialEffectArchiveSize)
    {
        var buffer = new byte[Math.Max(headerSize, DefaultHeaderSize)];
        if (header is not null)
        {
            WriteUInt32(buffer, 0, header.Field0);
            WriteUInt32(buffer, 4, header.Field4);
            WriteUInt32(buffer, 8, header.Field8);
            WriteUInt32(buffer, 12, header.Field12);
            WriteUInt32(buffer, 16, header.MapTypeId);
            WriteUInt32(buffer, 20, spectatingArchiveSize != 0 ? spectatingArchiveSize : header.SpectatingArchiveSize);
            WriteUInt32(buffer, 24, gameArchiveSize != 0 ? gameArchiveSize : header.GameArchiveSize);
            WriteUInt32(buffer, 28, specialEffectArchiveSize != 0 ? specialEffectArchiveSize : header.SpecialEffectArchiveSize);
            WriteUInt32(buffer, 32, header.PlayTimeMs);
            WriteNullTerminatedAscii(buffer, 40, header.MapLabel);
        }

        stream.Write(buffer.AsSpan(0, headerSize));
    }

    private static void WriteTimestampStream(Stream stream, IReadOnlyList<TimestampPacketRecord> packets)
    {
        foreach (var packet in packets)
        {
            var payload = packet.Payload;
            var size = packet.PayloadSize != 0 ? packet.PayloadSize : (uint)payload.Length;
            WriteUInt32(stream, size);
            WriteUInt32(stream, packet.Timestamp);
            stream.Write(payload);
        }
    }

    private static void WriteNullTerminatedAscii(byte[] buffer, int offset, string value)
    {
        if (offset >= buffer.Length || string.IsNullOrEmpty(value))
            return;

        var bytes = System.Text.Encoding.ASCII.GetBytes(value);
        var copyLen = Math.Min(bytes.Length, buffer.Length - offset - 1);
        bytes.AsSpan(0, copyLen).CopyTo(buffer.AsSpan(offset));
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
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
}
