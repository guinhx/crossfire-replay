namespace CrossFire.Replay.Formats.SimpleProtocol;

public sealed class SimpleProtocolReadOptions
{
    public static SimpleProtocolReadOptions Strict { get; } = new();

    /// <summary>Skip message IDs rejected by the reference client instead of throwing.</summary>
    public static SimpleProtocolReadOptions Lenient { get; } = new() { SkipUnsupportedMessageIds = true };

    /// <summary>Continue reading after parse errors on individual messages.</summary>
    public bool SkipUnsupportedMessageIds { get; init; }

    public bool SkipMessageParseErrors { get; init; }
}
