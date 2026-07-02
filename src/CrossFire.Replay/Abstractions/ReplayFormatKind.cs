namespace CrossFire.Replay.Abstractions;

public enum ReplayFormatKind
{
    /// <summary>Plain <c>cfrversion</c> stream (extension .cfr).</summary>
    SimpleProtocol = 1,

    /// <summary>PacketSimulator inner payload (.cfo / wrapped .cfn).</summary>
    PacketSimulator = 2,
}

public enum ReplayContainerKind
{
    /// <summary>No outer wrapper; file bytes are the payload.</summary>
    None = 0,

    /// <summary>Type 0: u32 header + Brotli payload.</summary>
    BrotliWrapper = 1,

    /// <summary>Type 1: u32 header + AES-CBC + inner CF_ConDeV + Brotli.</summary>
    EncryptedBrotliWrapper = 2,
}
