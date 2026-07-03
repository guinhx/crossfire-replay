using CrossFire.Replay.Formats.PacketSimulator;
using Spectre.Console;

namespace CrossFire.Replay.Cli.Output;

internal static class PacketSimulatorDocumentWriter
{
    public static void Write(PacketSimulatorReplayDocument ps, bool dump)
    {
        var table = new Table().Border(TableBorder.Rounded).Title("PacketSimulator");
        table.AddColumn("Field");
        table.AddColumn("Value");

        table.AddRow("Container", ps.ContainerKind.ToString());
        table.AddRow("Inner format", ps.InnerFormat.ToString());
        table.AddRow("Inner payload", $"{ps.InnerPayload.Length:N0} bytes");
        table.AddRow("Spectating room info", $"{ps.SpectatingRoomInfo.Length:N0} bytes");
        table.AddRow("Spectating packets", ps.SpectatingPackets.Packets.Count.ToString());
        table.AddRow("Game packets", ps.GamePackets.Packets.Count.ToString());
        table.AddRow("Special FX packets", ps.SpecialEffectPackets.Packets.Count.ToString());

        if (ps.Header is { } header)
        {
            table.AddRow("MapId", header.MapId.ToString());
            table.AddRow("Game goal", header.GameGoal.ToString());
            table.AddRow("Deathmatch", header.DeathMatchType.ToString());
            table.AddRow("Clan game", header.IsClanGame ? $"{header.ClanNameGr} vs {header.ClanNameBl}" : "no");
        }

        if (ps.InnerFormat == PacketSimulatorInnerFormat.ModernV2026)
        {
            table.AddRow("Unified timeline", ps.UnifiedTimeline.Count.ToString());
            table.AddRow("Deduplicated timeline", ps.DeduplicatedTimeline.Count.ToString());
            table.AddRow("Binary snapshots", ps.BinarySnapshots.Count.ToString());
            table.AddRow("Middle blob packets", ps.MiddleBlobPackets.Packets.Count.ToString());

            if (ps.ModernDescriptor is { } modern)
            {
                table.AddRow("Map label", modern.MapLabel);
                table.AddRow("Host", modern.HostLabel);
                table.AddRow("Map type id", modern.MapTypeId.ToString());
            }

            if (ps.ModernMetadata is { } meta)
            {
                var tailNonZero = meta.TailBytes.Count(static b => b != 0);
                table.AddRow("Metadata mapId", meta.Header.MapId.ToString());
                table.AddRow("Metadata goal", meta.Header.GameGoal.ToString());
                table.AddRow("Metadata tail non-zero", tailNonZero.ToString());

                if (meta.RoomInfoPrefix is { } room)
                {
                    table.AddRow(
                        "Room prefix",
                        $"max={room.RoomMaxUser} mapType={room.MapType} winGoal={room.WinGoal} @ {meta.RoomInfoPrefixOffset}");
                }

                if (meta.RoomInfoSources.Count > 0)
                    table.AddRow("Room info sources", string.Join(", ", meta.RoomInfoSources));
            }

            if (ps.MiddleBlobHeader is { } midHeader)
            {
                table.AddRow(
                    "Middle header",
                    $"mapTypeId={midHeader.MapTypeId} playTimeMs={midHeader.PlayTimeMs} label={midHeader.MapLabel}");
            }

            table.AddRow(
                "Middle blob coverage",
                $"offset={ps.MiddleBlobStreamOffset} segments={ps.MiddleBlobSegmentCount} ratio={ps.MiddleBlobCoverage.ParsedRatio:P1} gaps={ps.MiddleBlobGapContainers.Count}");
            table.AddRow("Special FX assets", ps.SpecialEffectAssets.Count.ToString());
        }

        AnsiConsole.Write(table);

        if (ps.InnerFormat == PacketSimulatorInnerFormat.ModernV2026)
        {
            foreach (var archive in ps.RawArchives)
                AnsiConsole.MarkupLine($"  [grey]archive[/] {Markup.Escape(archive.Name)}: {archive.Data.Length:N0} bytes");

            foreach (var snapshot in ps.BinarySnapshots)
            {
                AnsiConsole.MarkupLine(
                    $"  [grey]snapshot[/] ts={snapshot.Timestamp} size={snapshot.PayloadSize} body={snapshot.Body.Length} chunks={snapshot.Chunks.Count} embedded={snapshot.EmbeddedPackets.Count} chunkSize={GuessChunkSize(snapshot.Header)}");
                if (snapshot.WorldProps is { } world)
                    AnsiConsole.MarkupLine(
                        $"    worldProps farZ={world.FarZ} fogNear={world.FogNearZ} fogFar={world.FogFarZ}");
            }

            var binarySnapshots = ps.UnifiedTimeline.Count(p => p.PayloadKind == TimestampPayloadKind.BinarySnapshot);
            if (binarySnapshots > 0)
                AnsiConsole.MarkupLine($"  [grey]timeline binary snapshots[/] {binarySnapshots}");
        }

        if (dump && ps.GamePackets.Packets.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Game packet sample[/]");
            foreach (var pkt in ps.GamePackets.Packets.Take(5))
            {
                AnsiConsole.MarkupLine(
                    $"  ts={pkt.Timestamp} id={pkt.MessageId} decoded={pkt.Decoded?.GetType().Name ?? "null"} payload={pkt.Payload.Length}");
            }
        }

        if (dump && ps.InnerFormat == PacketSimulatorInnerFormat.Unknown)
        {
            var head = ps.InnerPayload.AsSpan(0, Math.Min(64, ps.InnerPayload.Length));
            AnsiConsole.MarkupLine($"[bold]Payload head[/] {Convert.ToHexString(head)}");
        }
    }

    private static int GuessChunkSize(BinarySnapshotHeader header) =>
        header.Field0 != 0 ? (int)header.Field0 : 65536;
}
