namespace CrossFire.Replay;

public sealed class ReplayParseException : Exception
{
    public long Offset { get; }

    public ReplayParseException(string message, long offset, Exception? inner = null)
        : base($"{message} (offset 0x{offset:X})", inner)
    {
        Offset = offset;
    }
}
