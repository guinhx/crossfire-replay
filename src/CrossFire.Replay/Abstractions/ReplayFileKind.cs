namespace CrossFire.Replay.Abstractions;

/// <summary>CrossFire replay file extensions used by the client (2022 reference build).</summary>
public enum ReplayFileKind
{
    Unknown,
    /// <summary>Plain history replay (<c>cfrversion</c>).</summary>
    Cfr,
    /// <summary>PacketSimulator Brotli wrapper (spectating).</summary>
    Cfo,
    /// <summary>Encrypted PacketSimulator wrapper (newer clients; same pipeline as .cfo after decrypt).</summary>
    Cfn,
    /// <summary>In-progress temp file during recording.</summary>
    Rtp,
}

public static class ReplayFileKindExtensions
{
    public static ReplayFileKind FromPath(string path)
    {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext))
            return ReplayFileKind.Unknown;

        return ext.ToLowerInvariant() switch
        {
            ".cfr" => ReplayFileKind.Cfr,
            ".cfo" => ReplayFileKind.Cfo,
            ".cfn" => ReplayFileKind.Cfn,
            ".rtp" => ReplayFileKind.Rtp,
            _ => ReplayFileKind.Unknown,
        };
    }

    public static bool CanRead(this ReplayFileKind kind) =>
        kind is ReplayFileKind.Cfr or ReplayFileKind.Cfo or ReplayFileKind.Cfn;
}
