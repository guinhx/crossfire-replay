namespace CrossFire.Replay.Abstractions;

public interface IReplayReader
{
    bool CanRead(string path);
    IReplayDocument Read(string path);
    IReplayDocument Read(byte[] fileBytes, string? sourcePath = null);
    ReplayInspection Inspect(string path);
    ReplayInspection Inspect(byte[] fileBytes);
}
