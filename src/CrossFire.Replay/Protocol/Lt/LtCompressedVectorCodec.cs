using CrossFire.Replay.Protocol;

namespace CrossFire.Replay.Protocol.Lt;

/// <summary>
/// Lithtech <c>CompVector</c> / <c>CompWorldPos</c> codecs (see lithtech compress.cpp + ltmessage_client.cpp).
/// </summary>
public static class LtCompressedVectorCodec
{
    public const int CompLtVectorBits = 32 + 16 + 16 + 8;
    public const int CompWorldPosBits = 16 + 16 + 16 + 8;

    public static Vector3F ReadCompLtVector(LtBitstreamReader reader)
    {
        var comp = new CompVector
        {
            FA = reader.ReadSingle(),
            DwB = reader.ReadUInt16(),
            DwC = reader.ReadUInt16(),
            Order = reader.ReadUInt8(),
        };

        return DecodeCompVector(comp);
    }

    public static LtCompWorldPos ReadCompWorldPos(LtBitstreamReader reader, bool includeExtraByte = true) =>
        new(
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            includeExtraByte ? reader.ReadUInt8() : (byte)0);

    private static Vector3F DecodeCompVector(CompVector comp)
    {
        var dwTemp = ((comp.Order >> 3) & 0x03) << 16;
        var tempDwB = comp.DwB + dwTemp;
        var fB = comp.FA * tempDwB / (1 << 18);
        if ((comp.Order & (1 << 5)) != 0)
            fB *= -1f;

        dwTemp = (comp.Order & 0x03) << 16;
        var tempDwC = comp.DwC + dwTemp;
        var fC = comp.FA * tempDwC / (1 << 18);
        if ((comp.Order & (1 << 2)) != 0)
            fC *= -1f;

        return (comp.Order >> 6) switch
        {
            1 => new Vector3F(fB, comp.FA, fC),
            2 => new Vector3F(fB, fC, comp.FA),
            _ => new Vector3F(comp.FA, fB, fC),
        };
    }

    public static Vector3F DecodeCompWorldPos(
        LtCompWorldPos pos,
        Vector3F worldMin,
        Vector3F worldMax,
        bool hiRes = true)
    {
        const int extraBitsX = 3;
        const int extraBitsY = 2;
        const int extraBitsZ = 3;

        var raw = new uint[3];
        raw[0] = (uint)pos.X << extraBitsX;
        raw[1] = (uint)pos.Y << extraBitsY;
        raw[2] = (uint)pos.Z << extraBitsZ;

        if (hiRes)
        {
            raw[0] += (uint)(pos.Extra & ((1 << extraBitsX) - 1));
            raw[1] += (uint)((pos.Extra & ((1 << (extraBitsX + extraBitsY)) - 1)) >> extraBitsX);
            raw[2] += (uint)((pos.Extra & ((1 << (extraBitsX + extraBitsY + extraBitsZ)) - 1)) >> (extraBitsX + extraBitsY));
        }

        var bias = hiRes
            ? new Vector3F(0.5f, 0.5f, 0.5f)
            : new Vector3F((1 << extraBitsX) / 2f, (1 << extraBitsY) / 2f, (1 << extraBitsZ) / 2f);
        var scale = new Vector3F(1 << (16 + extraBitsX), 1 << (16 + extraBitsY), 1 << (16 + extraBitsZ));
        var worldPercent = new Vector3F(
            (raw[0] + bias.X) / scale.X,
            (raw[1] + bias.Y) / scale.Y,
            (raw[2] + bias.Z) / scale.Z);

        return new Vector3F(
            worldMin.X + (worldMax.X - worldMin.X) * worldPercent.X,
            worldMin.Y + (worldMax.Y - worldMin.Y) * worldPercent.Y,
            worldMin.Z + (worldMax.Z - worldMin.Z) * worldPercent.Z);
    }

    private readonly struct CompVector
    {
        public float FA { get; init; }
        public ushort DwB { get; init; }
        public ushort DwC { get; init; }
        public byte Order { get; init; }
    }
}

public readonly record struct LtCompWorldPos(ushort X, ushort Y, ushort Z, byte Extra);
