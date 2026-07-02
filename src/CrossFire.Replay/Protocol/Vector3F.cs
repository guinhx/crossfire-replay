namespace CrossFire.Replay.Protocol;

public readonly struct Vector3F(float x, float y, float z)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Z { get; } = z;

    public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";
}
