using CrossFire.Replay;
using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Compression;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Formats.SimpleProtocol;
using CrossFire.Replay.Protocol;
using CrossFire.Replay.Protocol.Messages;

if (args.Length == 0)
{
    Console.WriteLine("Usage: CrossFire.Replay.Cli <file.cfr|.cfn|.cfo> [--dump] [--inspect]");
    Console.WriteLine("       CrossFire.Replay.Cli <file.cfr|.cfn|.cfo> --export-timeline <out.json|out.csv>");
    Console.WriteLine("       CrossFire.Replay.Cli --self-test");
    return 1;
}

if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
    return RunSelfTest();

var path = args[0];
var dump = args.Contains("--dump", StringComparer.OrdinalIgnoreCase);
var inspect = args.Contains("--inspect", StringComparer.OrdinalIgnoreCase);
var exportTimelinePath = GetExportTimelinePath(args);

try
{
    var bytes = File.ReadAllBytes(path);
    var service = ReplayService.Default;

    if (inspect)
        return RunInspect(service, path);

    var document = service.Read(bytes, path);
    if (exportTimelinePath is not null && document is PacketSimulatorReplayDocument psExport)
    {
        if (exportTimelinePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            PacketSimulatorTimelineExporter.WriteCsv(exportTimelinePath, psExport);
        else
            PacketSimulatorTimelineExporter.WriteJson(exportTimelinePath, psExport);

        Console.WriteLine($"Exported timeline: {exportTimelinePath}");
        Console.WriteLine($"  unified={psExport.UnifiedTimeline.Count} deduplicated={psExport.DeduplicatedTimeline.Count}");
        return 0;
    }

    return PrintDocument(document, dump);
}
catch (ReplayParseException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

static string? GetExportTimelinePath(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (!args[i].Equals("--export-timeline", StringComparison.OrdinalIgnoreCase))
            continue;

        return args[i + 1];
    }

    return null;
}

static int RunInspect(ReplayService service, string path)
{
    var info = service.Inspect(path);
    Console.WriteLine($"File: {path}");
    Console.WriteLine($"Kind: {info.FileKind}");
    Console.WriteLine($"Size: {info.FileSize}");
    Console.WriteLine($"Container: {info.ContainerKind}");
    Console.WriteLine($"Payload size: {info.PayloadLength}");
    Console.WriteLine($"Plain CFR: {info.IsPlainCfr}");
    Console.WriteLine($"PacketSimulator inner: {info.PacketSimulatorInnerFormat}");
    return 0;
}

static int PrintDocument(IReplayDocument document, bool dump)
{
    Console.WriteLine($"Format: {document.FormatKind}");
    Console.WriteLine($"Source: {document.SourcePath}");

    switch (document)
    {
        case CfrDocument cfr:
            Console.WriteLine($"Version: {cfr.FileVersion}");
            Console.WriteLine($"Checksum header: {cfr.HasChecksum}");
            Console.WriteLine($"Spec defines: {string.Join(", ", cfr.SpecDefines)}");
            Console.WriteLine($"Messages: {cfr.Messages.Count}");

            if (cfr.MapInfo is MapInfoMessage map)
                Console.WriteLine(
                    $"Map index: {map.MapIndex}, mode: {map.RoundMode}, win: {map.MatchWinCondition}, goal: {map.WinGoal}");

            if (dump)
            {
                foreach (var message in cfr.Messages)
                {
                    Console.WriteLine(
                        $"[{message.Timestamp,10}] {message.MessageId,-35} v{message.ProtocolVersion,2} payload={message.Payload.Length,4}");
                    if (message is GenericReplayMessage generic)
                    {
                        foreach (var (key, value) in generic.Fields)
                            Console.WriteLine($"    {key}: {value}");
                    }
                }
            }

            break;

        case PacketSimulatorReplayDocument ps:
            Console.WriteLine($"Container: {ps.ContainerKind}");
            Console.WriteLine($"Inner format: {ps.InnerFormat}");
            Console.WriteLine($"Inner payload: {ps.InnerPayload.Length} bytes");

            if (ps.InnerFormat == PacketSimulatorInnerFormat.ModernV2026 && ps.ModernDescriptor is { } modern)
            {
                Console.WriteLine($"Map label: {modern.MapLabel}, host: {modern.HostLabel}, mapTypeId: {modern.MapTypeId}");
                Console.WriteLine(
                    $"Raw archives: spectating={modern.SpectatingArchiveSize}, game={modern.GameArchiveSize}, sfx={modern.SpecialEffectArchiveSize}");
                Console.WriteLine($"Metadata block: {ps.MetadataBlock.Length} bytes, middle blob: {ps.MiddleBlob.Length} bytes");
                if (ps.ModernMetadata is { } meta)
                {
                    var tailNonZero = meta.TailBytes.Count(static b => b != 0);
                    Console.WriteLine(
                        $"Metadata parsed: mapId={meta.Header.MapId} goal={meta.Header.GameGoal} tailNonZero={tailNonZero}");
                    if (meta.RoomInfoPrefix is { } room)
                    {
                        Console.WriteLine(
                            $"  room prefix @ {meta.RoomInfoPrefixOffset}: max={room.RoomMaxUser} mapType={room.MapType} winGoal={room.WinGoal}");
                    }

                    if (meta.RoomInfoSources.Count > 0)
                    {
                        Console.WriteLine(
                            $"  room info sources: {string.Join(", ", meta.RoomInfoSources)}");
                    }
                }
                foreach (var archive in ps.RawArchives)
                    Console.WriteLine($"  {archive.Name}: {archive.Data.Length} bytes");
            }

            if (dump && ps.GamePackets.Packets.Count > 0)
            {
                var sample = ps.GamePackets.Packets.Take(5);
                foreach (var pkt in sample)
                {
                    Console.WriteLine(
                        $"  game pkt ts={pkt.Timestamp} id={pkt.MessageId} decoded={pkt.Decoded?.GetType().Name ?? "null"} payload={pkt.Payload.Length}");
                }
            }

            if (ps.Header is { } header)
            {
                Console.WriteLine(
                    $"MapId: {header.MapId}, goal: {header.GameGoal}, deathmatch: {header.DeathMatchType}");
                Console.WriteLine($"Clan: {header.IsClanGame} ({header.ClanNameGr} vs {header.ClanNameBl})");
            }

            Console.WriteLine($"Spectating room info: {ps.SpectatingRoomInfo.Length} bytes");
            Console.WriteLine($"Spectating packets: {ps.SpectatingPackets.Packets.Count}");
            Console.WriteLine($"Game packets: {ps.GamePackets.Packets.Count}");
            if (ps.InnerFormat == PacketSimulatorInnerFormat.ModernV2026)
            {
                Console.WriteLine(
                    $"Middle blob packets: {ps.MiddleBlobPackets.Packets.Count} (stream offset {ps.MiddleBlobStreamOffset}, segments {ps.MiddleBlobSegmentCount}, coverage {ps.MiddleBlobCoverage.ParsedRatio:P1}, gapContainers {ps.MiddleBlobGapContainers.Count})");
                if (ps.MiddleBlobHeader is { } midHeader)
                    Console.WriteLine(
                        $"Middle header mapTypeId={midHeader.MapTypeId} playTimeMs={midHeader.PlayTimeMs} label={midHeader.MapLabel}");
            }

            Console.WriteLine($"Special FX packets: {ps.SpecialEffectPackets.Packets.Count}");
            if (ps.InnerFormat == PacketSimulatorInnerFormat.ModernV2026)
            {
                Console.WriteLine($"Special FX assets: {ps.SpecialEffectAssets.Count}");
                Console.WriteLine($"Unified timeline: {ps.UnifiedTimeline.Count} packets");
                Console.WriteLine($"Deduplicated timeline: {ps.DeduplicatedTimeline.Count} packets");
                Console.WriteLine($"Binary snapshots: {ps.BinarySnapshots.Count}");
                foreach (var snapshot in ps.BinarySnapshots)
                {
                    Console.WriteLine(
                        $"  snapshot ts={snapshot.Timestamp} size={snapshot.PayloadSize} header=0x{snapshot.Header.Field0:X}/0x{snapshot.Header.Field1:X}/0x{snapshot.Header.Field2:X} body={snapshot.Body.Length} chunks={snapshot.Chunks.Count} embedded={snapshot.EmbeddedPackets.Count} chunkSize={BinarySnapshotBodyReader.GuessChunkSize(snapshot.Header)}");
                    if (snapshot.WorldProps is { } world)
                        Console.WriteLine($"    worldProps farZ={world.FarZ} fogNear={world.FogNearZ} fogFar={world.FogFarZ}");
                }

                var binarySnapshots = ps.UnifiedTimeline.Count(p => p.PayloadKind == TimestampPayloadKind.BinarySnapshot);
                if (binarySnapshots > 0)
                    Console.WriteLine($"  binary snapshots: {binarySnapshots}");
            }

            if (dump && ps.InnerFormat == PacketSimulatorInnerFormat.Unknown)
            {
                var head = ps.InnerPayload.AsSpan(0, Math.Min(64, ps.InnerPayload.Length));
                Console.WriteLine($"Payload head: {Convert.ToHexString(head)}");
            }

            break;
    }

    return 0;
}

static int RunSelfTest()
{
    var failures = 0;

    if (!RunPlainRoundTrip())
        failures++;

    if (!RunChecksumRoundTrip())
        failures++;

    if (!RunContainerRoundTrip(ReplayContainerKind.BrotliWrapper, ".cfo"))
        failures++;

    if (!RunContainerRoundTrip(ReplayContainerKind.EncryptedBrotliWrapper, ".cfn"))
        failures++;

    Console.WriteLine(failures == 0 ? "Self-test passed." : $"Self-test failed ({failures} checks).");
    return failures == 0 ? 0 : 3;
}

static CfrDocument CreateSampleDocument()
{
    var tempFile = Path.Combine(Path.GetTempPath(), "cfreplay_test.cfr");
    using (var stream = File.Create(tempFile))
    using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
    {
        writer.Write(System.Text.Encoding.ASCII.GetBytes("cfrversion0032"));
        writer.Write(ReplayFeatureFlags.DefaultWriteFlags.Count);
        foreach (var flag in ReplayFeatureFlags.DefaultWriteFlags)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(flag);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        writer.Write((byte)SimpleProtocolId.MapInfo);
        writer.Write((byte)3);
        writer.Write(1000u);
        writer.Write((short)42);
        writer.Write(1);
        writer.Write(2);
        writer.Write(100);
        writer.Write((sbyte)-1);
        writer.Write(false);
        var bl = System.Text.Encoding.ASCII.GetBytes("TeamBL");
        writer.Write(bl.Length + 1);
        writer.Write(bl);
        writer.Write((byte)0);
        var gr = System.Text.Encoding.ASCII.GetBytes("TeamGR");
        writer.Write(gr.Length + 1);
        writer.Write(gr);
        writer.Write((byte)0);
        writer.Write(false);
        writer.Write(false);

        writer.Write((byte)SimpleProtocolId.RoundStart);
        writer.Write((byte)1);
        writer.Write(1500u);
        writer.Write(0);
    }

    var doc = CfrReader.ReadFile(tempFile);
    File.Delete(tempFile);
    return doc;
}

static bool RunPlainRoundTrip()
{
    var doc = CreateSampleDocument();
    var roundTrip = Path.Combine(Path.GetTempPath(), "cfreplay_test_out.cfr");
    CfrWriter.WriteFile(roundTrip, doc);
    var doc2 = CfrReader.ReadFile(roundTrip);
    File.Delete(roundTrip);

    var ok = doc.Messages.Count == 2 && doc2.Messages.Count == 2;
    if (!ok)
        Console.Error.WriteLine("Plain CFR round-trip failed.");
    return ok;
}

static bool RunChecksumRoundTrip()
{
    var doc = CreateSampleDocument();
    var roundTrip = Path.Combine(Path.GetTempPath(), "cfreplay_test_checksum.cfr");
    CfrWriter.WriteFile(roundTrip, doc, prependChecksum: true);
    var doc2 = CfrReader.ReadFile(roundTrip);
    File.Delete(roundTrip);

    var ok = doc2.HasChecksum && doc2.Messages.Count == 2;
    if (!ok)
        Console.Error.WriteLine("Checksum CFR round-trip failed.");
    return ok;
}

static bool RunContainerRoundTrip(ReplayContainerKind containerKind, string extension)
{
    var doc = CreateSampleDocument();
    var writer = ReplayWriteService.Default;
    var options = containerKind switch
    {
        ReplayContainerKind.BrotliWrapper => ReplayWriteOptions.CfoWrapper,
        ReplayContainerKind.EncryptedBrotliWrapper => ReplayWriteOptions.CfnWrapper,
        _ => ReplayWriteOptions.PlainCfr,
    };

    var wrapped = writer.Write(doc, options);
    var decoder = new ReplayContainerDecoder();
    if (!decoder.CanDecode(wrapped))
    {
        Console.Error.WriteLine($"Container encode failed for {extension}.");
        return false;
    }

    var decoded = decoder.Decode(wrapped);
    var roundTrip = CfrReader.Read(decoded.Payload);
    var ok = roundTrip.Messages.Count == 2;
    if (!ok)
        Console.Error.WriteLine($"Container round-trip failed for {extension}.");
    return ok;
}
