namespace CrossFire.Replay.Tests.Support;

/// <summary>
/// Resolves optional integration-test replay fixtures from environment variables.
/// </summary>
public static class ReplayFixturePaths
{
    public const string FixtureCfnVariable = "CROSSFIRE_REPLAY_FIXTURE_CFN";
    public const string FixtureFolderVariable = "CROSSFIRE_REPLAY_FIXTURE_FOLDER";

    private static readonly string[] DefaultCfnCandidates =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Cross Fire", "Replay", "CFReplay20260701_0000.cfn"),
    ];

    public static bool TryGetPrimaryModernCfn(out string path)
    {
        var fromEnv = Environment.GetEnvironmentVariable(FixtureCfnVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
        {
            path = fromEnv;
            return true;
        }

        foreach (var candidate in DefaultCfnCandidates)
        {
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        path = string.Empty;
        return false;
    }

    public static string? GetReplayFolder()
    {
        var fromEnv = Environment.GetEnvironmentVariable(FixtureFolderVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv) && Directory.Exists(fromEnv))
            return fromEnv;

        var fromPrimary = TryGetPrimaryModernCfn(out var cfn)
            ? Path.GetDirectoryName(cfn)
            : null;
        if (!string.IsNullOrEmpty(fromPrimary) && Directory.Exists(fromPrimary))
            return fromPrimary;

        return null;
    }

    public static IEnumerable<string> EnumerateCfnFiles()
    {
        var folder = GetReplayFolder();
        if (folder is null)
            yield break;

        foreach (var file in Directory.GetFiles(folder, "*.cfn").OrderBy(static p => p, StringComparer.OrdinalIgnoreCase))
            yield return file;
    }

    public static IEnumerable<object[]> CfnFileTheoryData()
    {
        foreach (var file in EnumerateCfnFiles())
            yield return new object[] { file };
    }
}
