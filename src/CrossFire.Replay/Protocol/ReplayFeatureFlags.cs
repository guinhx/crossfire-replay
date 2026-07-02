namespace CrossFire.Replay.Protocol;

public static class ReplayFeatureFlags
{
    public const string ImproveReplayFileSystem = "IMPROVE_REPLAY_FILE_SYSTEM";
    public const string UseClanSystemRefine2nd = "USE_CLAN_SYSTEM_REFINE_2ND";
    public const string UseCharacterSkinChangeSystem = "USE_CHARACTER_SKIN_CHANGE_SYSTEM";

    /// <summary>Client compile-time flag gating optional CFR body checksum.</summary>
    public const string UseReplayCrypt = "USE_REPLAY_CRYPT";

    public static readonly IReadOnlyList<string> DefaultWriteFlags =
    [
        ImproveReplayFileSystem,
        UseClanSystemRefine2nd,
        UseCharacterSkinChangeSystem,
    ];
}
