namespace CrossFire.Replay.Abstractions;

public interface IReplayWriter
{
    bool CanWrite(IReplayDocument document);

    byte[] Write(IReplayDocument document, ReplayWriteOptions? options = null);

    void WriteFile(string path, IReplayDocument document, ReplayWriteOptions? options = null);
}
