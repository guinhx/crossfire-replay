using System.Reflection;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace CrossFire.Replay.Cli.Output;

internal static class StructLayoutFormatter
{
    public static void WriteTable<T>(string title, T value) where T : unmanaged
    {
        AnsiConsole.MarkupLine($"[bold]{title}[/] ([grey]{typeof(T).Name}[/], {Marshal.SizeOf<T>()} bytes)");

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Offset");
        table.AddColumn("Field");
        table.AddColumn("Value");

        var offset = 0;
        foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            var fieldValue = field.GetValue(value);
            var size = SizeOfField(field.FieldType);
            table.AddRow(
                $"0x{offset:X2}",
                field.Name,
                FormatValue(fieldValue));
            offset += size;
        }

        AnsiConsole.Write(table);
    }

    private static int SizeOfField(Type type) =>
        type.IsEnum ? Marshal.SizeOf(Enum.GetUnderlyingType(type)) : Marshal.SizeOf(type);

    private static string FormatValue(object? value) =>
        value switch
        {
            null => "-",
            bool b => b ? "true" : "false",
            byte or sbyte or short or ushort or int or uint or long or ulong =>
                $"0x{Convert.ToUInt64(value):X} ({value})",
            float f => f.ToString("G9"),
            double d => d.ToString("G17"),
            _ => value.ToString() ?? "-",
        };
}
