using CrossFire.Replay.Formats.PacketSimulator;

namespace CrossFire.Replay.Abstractions;

public sealed class ReplayInspection
{
    public ReplayFileKind FileKind { get; init; }
    public ReplayContainerKind ContainerKind { get; init; }
    public int FileSize { get; init; }
    public int PayloadLength { get; init; }
    public bool IsPlainCfr { get; init; }
    public PacketSimulatorInnerFormat PacketSimulatorInnerFormat { get; init; }
}
