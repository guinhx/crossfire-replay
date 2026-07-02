namespace CrossFire.Replay.Abstractions;

public interface IReplayDocument
{
    ReplayFormatKind FormatKind { get; }
    string SourcePath { get; }
}
