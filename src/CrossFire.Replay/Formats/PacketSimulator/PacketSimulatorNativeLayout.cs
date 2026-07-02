namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>
/// On-disk PacketSimulator inner layouts (legacy and modern), validated by RE and round-trip tests.
/// </summary>
public static class PacketSimulatorNativeLayout
{
    /// <summary>Legacy inner prefix before the 512-byte header block.</summary>
    public const uint LegacyContainerVersion = 6;

    /// <summary>Legacy inner magic written before the 512-byte header block.</summary>
    public const uint LegacyInnerMagic = 1020107;

    /// <summary>Declared size prefix before spectating room-info bytes in legacy inner payload.</summary>
    public const int LegacySpectatingRoomInfoDeclaredSize = PacketSimulatorLayout.MaxSpectatingRoomInfoSize;

    /// <summary>
    /// Legacy write order (observed):
    /// version → magic → 512-byte header → room-info size → room-info bytes →
    /// spectating ILT archive → game ILT archive → special-FX archive → Brotli(compress all).
    /// </summary>
    public const string LegacyWriteLayoutId = "legacy-inner-write";

    /// <summary>
    /// Legacy read order (observed):
    /// skip container prefix → 512-byte header → room-info size → room-info bytes →
    /// spectating archive → game archive → sfx archive.
    /// </summary>
    public const string LegacyReadLayoutId = "legacy-inner-read";

    /// <summary>
    /// The 512-byte match header: deathmatch type, game goal, map id,
    /// clan names/scores when applicable. Reused as the first half of modern metadata.
    /// </summary>
    public const string HeaderBlockLayoutId = "match-header-512";

    /// <summary>
    /// Modern .cfn inner layout (observed 2026 replays):
    /// signature → metadata(1024) → extra(4) → descriptor → middle blob → 3 tail archives.
    /// Metadata = 512-byte header block + 512 reserved bytes (zero in samples).
    /// </summary>
    public const string ModernObservedLayout = "modern-inner-2026";

    /// <summary>
    /// Full room-info block (prefix + slot table) appears only in the legacy spectating-room region.
    /// Modern replays carry room context via descriptor and middle-blob header instead.
    /// </summary>
    public const string ModernRoomInfoNote =
        "Room-info block not written to modern metadata tail; see descriptor + middle-blob header";
}

/// <summary>Where modern replays expose room / match context when the legacy room-info block is absent.</summary>
public enum ModernReplayRoomInfoSource
{
    None = 0,
    MetadataHeaderBlock = 1,
    ModernDescriptor = 2,
    MiddleBlobHeader = 3,
    MetadataTailPrefix = 4,
}
