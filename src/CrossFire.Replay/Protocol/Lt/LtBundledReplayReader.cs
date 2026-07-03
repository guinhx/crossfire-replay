namespace CrossFire.Replay.Protocol.Lt;

internal delegate bool LtBundledEntryDecoder<TEntry>(
    ReadOnlySpan<byte> slice,
    int absoluteOffset,
    out TEntry entry,
    out int consumedBytes);

/// <summary>
/// Replay archives sometimes concatenate several ILT messages with padding between them.
/// Entry bodies are decoded natively; this helper advances to the next message boundary.
/// </summary>
internal static class LtBundledReplayReader
{
    public static bool LooksLikeMessageId(ReadOnlySpan<byte> payload, ushort messageId, int offset) =>
        offset + 1 < payload.Length &&
        (ushort)(payload[offset] | (payload[offset + 1] << 8)) == messageId;

    public static int FindNextMessageIdOffset(ReadOnlySpan<byte> payload, ushort messageId, int start)
    {
        for (var i = start; i + 1 < payload.Length; i++)
        {
            if (LooksLikeMessageId(payload, messageId, i))
                return i;
        }

        return -1;
    }

    public static int ResolveNextOffset(
        ReadOnlySpan<byte> payload,
        ushort messageId,
        int currentOffset,
        int consumedBytes)
    {
        var nextOffset = currentOffset + consumedBytes;
        if (nextOffset < payload.Length && !LooksLikeMessageId(payload, messageId, nextOffset))
        {
            var aligned = FindNextMessageIdOffset(payload, messageId, currentOffset + 1);
            if (aligned > currentOffset)
                nextOffset = aligned;
        }

        return nextOffset;
    }

    public static List<TEntry>? DecodeEntries<TEntry>(
        ReadOnlySpan<byte> payload,
        ushort messageId,
        LtBundledEntryDecoder<TEntry> tryDecodeEntry)
    {
        var entries = new List<TEntry>();
        var offset = 0;
        while (offset < payload.Length)
        {
            if (!tryDecodeEntry(payload[offset..], offset, out var entry, out var consumed))
            {
                var next = FindNextMessageIdOffset(payload, messageId, offset + 1);
                if (next < 0)
                    break;

                offset = next;
                continue;
            }

            entries.Add(entry);
            if (consumed <= 0)
                break;

            offset = ResolveNextOffset(payload, messageId, offset, consumed);
        }

        return entries.Count > 0 ? entries : null;
    }
}
