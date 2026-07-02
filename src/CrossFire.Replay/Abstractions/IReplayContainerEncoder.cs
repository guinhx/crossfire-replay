namespace CrossFire.Replay.Abstractions;

public interface IReplayContainerEncoder
{
    byte[] EncodeBrotliWrapper(ReadOnlySpan<byte> payload);
    byte[] EncodeEncryptedWrapper(ReadOnlySpan<byte> payload, int keyIndex, int ivIndex);
}
