using System.Runtime.InteropServices;
using System.Text;
using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Interop;
using CrossFire.Replay.Protocol.Mm;

namespace CrossFire.Replay.Cli.Interop;

internal static class ReplayFileLayoutProbe
{
    private static ReadOnlySpan<byte> CfrVersionMagic => "cfrversion"u8;

    public static bool TryReadContainerHeader(ReadOnlySpan<byte> fileBytes, out ReplayContainerHeader header)
    {
        header = default;
        if (fileBytes.Length < Marshal.SizeOf<ReplayContainerHeader>())
            return false;

        if (fileBytes.StartsWith(CfrVersionMagic))
            return false;

        if (fileBytes[0] is not (0 or 1))
            return false;

        header = StructReader.Read<ReplayContainerHeader>(fileBytes);
        return true;
    }

    public static bool TryReadMatchHeaderPrefix(ReadOnlySpan<byte> payload, out MatchHeaderFixedLayout layout)
    {
        layout = default;
        if (payload.Length < Marshal.SizeOf<MatchHeaderFixedLayout>())
            return false;

        layout = StructReader.Read<MatchHeaderFixedLayout>(payload);
        return true;
    }

    public static bool TryReadRoomInfoPrefix(ReadOnlySpan<byte> block, int offset, out ProtoMmRoomInfoPrefix prefix)
    {
        prefix = default;
        if (offset < 0 || offset + ProtoMmRoomInfoPrefix.Size > block.Length)
            return false;

        prefix = StructReader.Read<ProtoMmRoomInfoPrefix>(block[offset..]);
        return true;
    }

    public static string DescribePlainCfrPrefix(ReadOnlySpan<byte> fileBytes)
    {
        if (!fileBytes.StartsWith(CfrVersionMagic))
            return "not plain CFR";

        var versionDigits = fileBytes.Length >= 12
            ? Encoding.ASCII.GetString(fileBytes[10..12])
            : "?";
        return $"cfrversion00{versionDigits}";
    }
}
