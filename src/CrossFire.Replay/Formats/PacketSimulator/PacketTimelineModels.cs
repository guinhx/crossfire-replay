namespace CrossFire.Replay.Formats.PacketSimulator;

public enum PacketTimelineSource
{
    MiddleBlob = 0,
    SpectatingArchive = 1,
    GameArchive = 2,
    BinarySnapshotBody = 3,
}

public enum TimestampPayloadKind
{
    Standard = 0,
    NestedArchive = 1,
    BinarySnapshot = 2,
}
