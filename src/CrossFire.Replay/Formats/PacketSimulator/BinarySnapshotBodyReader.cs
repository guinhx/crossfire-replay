namespace CrossFire.Replay.Formats.PacketSimulator;

public sealed class BinarySnapshotChunk
{
    public int Offset { get; init; }
    public int Size { get; init; }
    public int IltPacketCount { get; init; }
    public int IltBytesConsumed { get; init; }
    public double NonZeroRatio { get; init; }
}

public static class BinarySnapshotBodyReader
{
    public static IReadOnlyList<BinarySnapshotChunk> SplitChunks(BinarySnapshotRecord snapshot)
    {
        if (snapshot.Body.Length == 0)
            return Array.Empty<BinarySnapshotChunk>();

        var chunkSize = GuessChunkSize(snapshot.Header);
        if (chunkSize <= 0)
            return Array.Empty<BinarySnapshotChunk>();

        var chunks = new List<BinarySnapshotChunk>();
        for (var offset = 0; offset < snapshot.Body.Length; offset += chunkSize)
        {
            var size = Math.Min(chunkSize, snapshot.Body.Length - offset);
            var slice = snapshot.Body.AsSpan(offset, size);
            var section = IltPacketArchiveReader.ParseTimestampStream(slice);
            chunks.Add(new BinarySnapshotChunk
            {
                Offset = offset,
                Size = size,
                IltPacketCount = section.Packets.Count,
                IltBytesConsumed = section.BytesConsumed,
                NonZeroRatio = ComputeNonZeroRatio(slice),
            });
        }

        return chunks;
    }

    internal static int GuessChunkSize(BinarySnapshotHeader header)
    {
        if (header.Field0 > 0 && header.Field0 == header.Field1)
            return (int)header.Field0;

        if (header.Field0 is >= 4096 and <= 1_048_576)
            return (int)header.Field0;

        if (header.Field1 is >= 4096 and <= 1_048_576)
            return (int)header.Field1;

        return 65_536;
    }

    private static double ComputeNonZeroRatio(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return 0;

        var nonZero = 0;
        foreach (var value in data)
        {
            if (value != 0)
                nonZero++;
        }

        return (double)nonZero / data.Length;
    }
}
