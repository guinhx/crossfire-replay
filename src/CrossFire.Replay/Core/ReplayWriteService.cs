using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Compression;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Formats.SimpleProtocol;

namespace CrossFire.Replay.Core;

public sealed class ReplayWriteService : IReplayWriter
{
    public static ReplayWriteService Default { get; } = new(new ReplayContainerEncoder());

    private readonly IReplayContainerEncoder _containerEncoder;

    public ReplayWriteService(IReplayContainerEncoder containerEncoder)
    {
        _containerEncoder = containerEncoder;
    }

    public bool CanWrite(IReplayDocument document) =>
        document is CfrDocument or PacketSimulatorReplayDocument;

    public byte[] Write(IReplayDocument document, ReplayWriteOptions? options = null)
    {
        options ??= ReplayWriteOptions.PlainCfr;

        if (options.ContainerKind == ReplayContainerKind.None)
        {
            return document switch
            {
                CfrDocument cfr => SimpleProtocolWriter.WritePayload(cfr, new SimpleProtocolWriteOptions
                {
                    PrependChecksum = options.PrependChecksum,
                }),
                PacketSimulatorReplayDocument ps => PacketSimulatorPayloadWriter.BuildInnerPayload(ps),
                _ => throw Unsupported(document),
            };
        }

        if (options.PrependChecksum)
            throw new NotSupportedException("Checksum prefix is only supported for plain .cfr files.");

        var innerPayload = document switch
        {
            CfrDocument cfr => SimpleProtocolWriter.WritePayload(cfr),
            PacketSimulatorReplayDocument ps => PacketSimulatorPayloadWriter.BuildInnerPayload(ps),
            _ => throw Unsupported(document),
        };

        return options.ContainerKind switch
        {
            ReplayContainerKind.BrotliWrapper => _containerEncoder.EncodeBrotliWrapper(innerPayload),
            ReplayContainerKind.EncryptedBrotliWrapper => _containerEncoder.EncodeEncryptedWrapper(
                innerPayload,
                options.EncryptionKeyIndex,
                options.EncryptionIvIndex),
            _ => throw new NotSupportedException($"Container kind {options.ContainerKind} is not supported."),
        };
    }

    public void WriteFile(string path, IReplayDocument document, ReplayWriteOptions? options = null)
    {
        options ??= ReplayWriteOptions.FromFileKind(ReplayFileKindExtensions.FromPath(path));
        File.WriteAllBytes(path, Write(document, options));
    }

    private static NotSupportedException Unsupported(IReplayDocument document) =>
        new($"Cannot write document type {document.GetType().Name}.");
}
