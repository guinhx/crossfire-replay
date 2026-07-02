namespace CrossFire.Replay.Abstractions;

public sealed class ReplayWriteOptions
{
    public ReplayContainerKind ContainerKind { get; init; } = ReplayContainerKind.None;

    public bool PrependChecksum { get; init; }

    /// <summary>AES dictionary key row index for type-1 wrapper (.cfn).</summary>
    public int EncryptionKeyIndex { get; init; }

    /// <summary>AES dictionary IV row index for type-1 wrapper (.cfn).</summary>
    public int EncryptionIvIndex { get; init; }

    public static ReplayWriteOptions PlainCfr { get; } = new();

    public static ReplayWriteOptions PlainCfrWithChecksum { get; } = new() { PrependChecksum = true };

    public static ReplayWriteOptions CfoWrapper { get; } = new() { ContainerKind = ReplayContainerKind.BrotliWrapper };

    public static ReplayWriteOptions CfnWrapper { get; } = new()
    {
        ContainerKind = ReplayContainerKind.EncryptedBrotliWrapper,
    };

    public static ReplayWriteOptions FromFileKind(ReplayFileKind kind, bool prependChecksum = false) =>
        kind switch
        {
            ReplayFileKind.Cfr => prependChecksum ? PlainCfrWithChecksum : PlainCfr,
            ReplayFileKind.Cfo => CfoWrapper,
            ReplayFileKind.Cfn => CfnWrapper,
            _ => PlainCfr,
        };
}
