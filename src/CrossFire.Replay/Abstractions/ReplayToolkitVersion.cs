namespace CrossFire.Replay.Abstractions;

/// <summary>Semantic version of the CrossFire Replay Toolkit assembly (pre-1.0: format-first, no API stability guarantee).</summary>
public static class ReplayToolkitVersion
{
    public const string Current = "0.1.0";

    /// <summary>Major bump = breaking public API or export schema.</summary>
    public const int Major = 0;

    /// <summary>Minor bump = new decoders, formats, or non-breaking API.</summary>
    public const int Minor = 1;

    /// <summary>Patch bump = fixes only.</summary>
    public const int Patch = 0;
}
