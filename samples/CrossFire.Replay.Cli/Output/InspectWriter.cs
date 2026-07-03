using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Cli.Interop;
using CrossFire.Replay.Compression;
using Spectre.Console;

namespace CrossFire.Replay.Cli.Output;

internal static class InspectWriter
{
    public static void Write(string path, ReadOnlySpan<byte> fileBytes, ReplayInspection info, bool includeHex)
    {
        var table = new Table().Border(TableBorder.Rounded).Title("Replay inspection");
        table.AddColumn("Field");
        table.AddColumn("Value");

        table.AddRow("File", Markup.Escape(path));
        table.AddRow("Kind", info.FileKind.ToString());
        table.AddRow("Size", $"{info.FileSize:N0} bytes");
        table.AddRow("Container", info.ContainerKind.ToString());
        table.AddRow("Payload size", $"{info.PayloadLength:N0} bytes");
        table.AddRow("Plain CFR", info.IsPlainCfr ? "yes" : "no");
        table.AddRow("Inner format", info.PacketSimulatorInnerFormat.ToString());

        if (info.IsPlainCfr)
            table.AddRow("CFR prefix", ReplayFileLayoutProbe.DescribePlainCfrPrefix(fileBytes));

        AnsiConsole.Write(table);

        if (ReplayFileLayoutProbe.TryReadContainerHeader(fileBytes, out var container))
        {
            AnsiConsole.WriteLine();
            StructLayoutFormatter.WriteTable("Outer container header", container);

            var kindRow = new Table().Border(TableBorder.None);
            kindRow.AddColumn("Derived");
            kindRow.AddColumn("Value");
            kindRow.AddRow("Kind byte", container.KindByte.ToString());
            if (container.KindByte == 1)
            {
                kindRow.AddRow("AES key index", container.KeyIndex.ToString());
                kindRow.AddRow("AES IV index", container.IvIndex.ToString());
            }

            AnsiConsole.Write(kindRow);
        }

        if (info.PayloadLength > 0)
        {
            var payload = TryGetPayloadPreview(fileBytes, info);
            if (payload.Length > 0 && ReplayFileLayoutProbe.TryReadMatchHeaderPrefix(payload, out var header))
            {
                AnsiConsole.WriteLine();
                StructLayoutFormatter.WriteTable("Match header fixed prefix", header);
            }
        }

        if (includeHex)
        {
            AnsiConsole.WriteLine();
            var preview = fileBytes[..Math.Min(64, fileBytes.Length)];
            AnsiConsole.MarkupLine("[bold]File head (64 B max)[/]");
            AnsiConsole.Write(new Panel(Convert.ToHexString(preview)) { Border = BoxBorder.Rounded });
        }
    }

    private static ReadOnlySpan<byte> TryGetPayloadPreview(ReadOnlySpan<byte> fileBytes, ReplayInspection info)
    {
        if (info.IsPlainCfr)
            return fileBytes;

        try
        {
            var decoder = new ReplayContainerDecoder();
            if (!decoder.CanDecode(fileBytes))
                return ReadOnlySpan<byte>.Empty;

            var decoded = decoder.Decode(fileBytes);
            return decoded.Payload.AsSpan(0, Math.Min(decoded.Payload.Length, 512));
        }
        catch
        {
            return ReadOnlySpan<byte>.Empty;
        }
    }
}
