namespace CrossFire.Replay.Abstractions;

using CrossFire.Replay.Formats.PacketSimulator;

public static class ReplayDocumentExtensions
{
    public static global::CrossFire.Replay.CfrDocument AsSimpleProtocol(this IReplayDocument document) =>
        document as global::CrossFire.Replay.CfrDocument
        ?? throw new InvalidCastException(
            $"Document is {document.FormatKind}, expected {ReplayFormatKind.SimpleProtocol}.");

    public static bool TryAsSimpleProtocol(this IReplayDocument document, out global::CrossFire.Replay.CfrDocument? cfr)
    {
        cfr = document as global::CrossFire.Replay.CfrDocument;
        return cfr is not null;
    }

    public static PacketSimulatorReplayDocument AsPacketSimulator(this IReplayDocument document) =>
        document as PacketSimulatorReplayDocument
        ?? throw new InvalidCastException(
            $"Document is {document.FormatKind}, expected {ReplayFormatKind.PacketSimulator}.");

    public static bool TryAsPacketSimulator(
        this IReplayDocument document,
        out PacketSimulatorReplayDocument? packetReplay)
    {
        packetReplay = document as PacketSimulatorReplayDocument;
        return packetReplay is not null;
    }
}
