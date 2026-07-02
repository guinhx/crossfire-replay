using System.Text;

namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// Parses special-effect asset path blobs (e.g. ".DTX" texture paths) from EOF sfx archives.
/// </summary>
public static class SpecialEffectAssetParser
{
    public static IReadOnlyList<SpecialEffectAssetRecord> Parse(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return Array.Empty<SpecialEffectAssetRecord>();

        var assets = new List<SpecialEffectAssetRecord>();
        var cursor = 0;
        while (cursor < data.Length)
        {
            while (cursor < data.Length && !IsPrintableAscii(data[cursor]))
                cursor++;

            if (cursor >= data.Length)
                break;

            var start = cursor;
            while (cursor < data.Length && IsPrintableAscii(data[cursor]))
                cursor++;

            if (cursor <= start)
                continue;

            var path = Encoding.ASCII.GetString(data.Slice(start, cursor - start));
            if (!LooksLikeAssetPath(path))
                continue;

            assets.Add(new SpecialEffectAssetRecord
            {
                Offset = start,
                AssetPath = path,
            });
        }

        return assets;
    }

    private static bool LooksLikeAssetPath(string path) =>
        path.Length >= 4
        && path.Contains('.', StringComparison.Ordinal)
        && !path.Contains(' ', StringComparison.Ordinal);

    private static bool IsPrintableAscii(byte value) => value is >= 0x20 and <= 0x7E;
}
