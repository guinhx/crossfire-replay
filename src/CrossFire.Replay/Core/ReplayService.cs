using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Compression;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Formats.SimpleProtocol;

namespace CrossFire.Replay.Core;

public sealed class ReplayService : IReplayReader
{
    public static ReplayService Default { get; } = new(
        new ReplayContainerDecoder(),
        new IReplayPayloadReader[]
        {
            new SimpleProtocolPayloadReader(),
            new PacketSimulatorPayloadReader(),
        });

    private readonly IReplayContainerDecoder _containerDecoder;
    private readonly IReadOnlyList<IReplayPayloadReader> _payloadReaders;

    public ReplayService(
        IReplayContainerDecoder containerDecoder,
        IEnumerable<IReplayPayloadReader> payloadReaders)
    {
        _containerDecoder = containerDecoder;
        _payloadReaders = payloadReaders.ToList();
    }

    public bool CanRead(string path) =>
        File.Exists(path) && ReplayFileKindExtensions.FromPath(path).CanRead();

    public IReplayDocument Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return Read(bytes, path);
    }

    public IReplayDocument Read(byte[] fileBytes, string? sourcePath = null)
    {
        var (payload, containerKind) = ResolvePayload(fileBytes);

        foreach (var reader in _payloadReaders)
        {
            if (!reader.CanRead(payload))
                continue;

            if (reader is PacketSimulatorPayloadReader packetReader)
                return packetReader.Read(payload, containerKind, sourcePath);

            return reader.Read(payload, sourcePath);
        }

        throw new ReplayParseException("Unsupported replay format.", 0);
    }

    public ReplayInspection Inspect(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return InspectInternal(bytes, path);
    }

    public ReplayInspection Inspect(byte[] fileBytes) => InspectInternal(fileBytes, sourcePath: null);

    private ReplayInspection InspectInternal(byte[] fileBytes, string? sourcePath)
    {
        var (payload, containerKind) = ResolvePayload(fileBytes);
        var innerFormat = PacketSimulatorPayloadReader.DetectInnerFormat(payload);

        return new ReplayInspection
        {
            FileKind = sourcePath is null ? ReplayFileKind.Unknown : ReplayFileKindExtensions.FromPath(sourcePath),
            ContainerKind = containerKind,
            FileSize = fileBytes.Length,
            PayloadLength = payload.Length,
            IsPlainCfr = SimpleProtocolReader.LooksLikePlainCfr(payload),
            PacketSimulatorInnerFormat = innerFormat,
        };
    }

    private (byte[] payload, ReplayContainerKind containerKind) ResolvePayload(byte[] fileBytes)
    {
        if (SimpleProtocolReader.LooksLikePlainCfr(fileBytes))
            return (fileBytes, ReplayContainerKind.None);

        if (_containerDecoder.CanDecode(fileBytes))
        {
            var info = _containerDecoder.Decode(fileBytes);
            return (info.Payload, info.Kind);
        }

        return (fileBytes, ReplayContainerKind.None);
    }
}
