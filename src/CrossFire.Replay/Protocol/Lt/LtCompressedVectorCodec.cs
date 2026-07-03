using CrossFire.Replay.Protocol;

namespace CrossFire.Replay.Protocol.Lt;

/// <summary>
/// Lithtech <c>CompVector</c> / <c>CompWorldPos</c> codecs (see lithtech compress.cpp + ltmessage_client.cpp).
/// </summary>
public static class LtCompressedVectorCodec
{
    public const int CompLtVectorBits = 32 + 16 + 16 + 8;
    public const int CompWorldPosBits = 16 + 16 + 16 + 8;

    private const float CompVectorEncodeScale = 262143f;
    private const float CompWorldXzEncodeScale = 524287f;
    private const float CompWorldYEncodeScale = 262143f;
    private const float CompWorldXzDecodeScale = 1f / 524288f;
    private const float CompWorldYDecodeScale = 1f / 262144f;

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

    public static void WriteCompLtVector(LtBitstreamWriter writer, Vector3F value)
    {
        var comp = EncodeCompVector(value);
        writer.WriteSingle(comp.FA);
        writer.WriteUInt16(comp.DwB);
        writer.WriteUInt16(comp.DwC);
        writer.WriteUInt8(comp.Order);
    }

    public static LtCompWorldPos ReadCompWorldPos(LtBitstreamReader reader, bool includeExtraByte = true) =>
        new(
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            includeExtraByte ? reader.ReadUInt8() : (byte)0);

    public static void WriteCompPos(
        LtBitstreamWriter writer,
        Vector3F value,
        Vector3F worldMin,
        Vector3F worldMax,
        bool includeExtraByte = false)
    {
        var encoded = EncodeCompWorldPos(value, worldMin, worldMax);
        writer.WriteUInt16(encoded.X);
        writer.WriteUInt16(encoded.Y);
        writer.WriteUInt16(encoded.Z);
        if (includeExtraByte)
            writer.WriteUInt8(encoded.Extra);
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

    private static CompVector EncodeCompVector(Vector3F value)
    {
        var major = value.X;
        var first = value.Y;
        var second = value.Z;
        var majorAbs = MathF.Abs(value.X);
        var order = (byte)0;

        var yAbs = MathF.Abs(value.Y);
        if (yAbs > majorAbs)
        {
            major = value.Y;
            first = value.X;
            second = value.Z;
            majorAbs = yAbs;
            order = 0x40;
        }

        var zAbs = MathF.Abs(value.Z);
        if (zAbs > majorAbs)
        {
            major = value.Z;
            first = value.X;
            second = value.Y;
            majorAbs = zAbs;
            order = 0x80;
        }

        if (majorAbs <= 0f)
            return new CompVector { FA = 0f, DwB = 0, DwC = 0, Order = order };

        var firstQuantized = QuantizeCompVectorComponent(first, majorAbs);
        var secondQuantized = QuantizeCompVectorComponent(second, majorAbs);
        var flags = (byte)((((firstQuantized >> 16) & 0x3) << 3) | order);

        var majorSign = major < 0f ? -1 : 1;
        if ((first < 0f ? -1 : 1) != majorSign)
            flags = (byte)(flags | 0x20);

        flags = (byte)(flags | (byte)((secondQuantized >> 16) & 0x3));
        if ((second < 0f ? -1 : 1) != majorSign)
            flags = (byte)(flags | 0x04);

        return new CompVector
        {
            FA = major,
            DwB = (ushort)firstQuantized,
            DwC = (ushort)secondQuantized,
            Order = flags,
        };
    }

    private static LtCompWorldPos EncodeCompWorldPos(Vector3F value, Vector3F worldMin, Vector3F worldMax)
    {
        var x = QuantizeWorldComponent(value.X, worldMin.X, worldMax.X, CompWorldXzEncodeScale);
        var y = QuantizeWorldComponent(value.Y, worldMin.Y, worldMax.Y, CompWorldYEncodeScale);
        var z = QuantizeWorldComponent(value.Z, worldMin.Z, worldMax.Z, CompWorldXzEncodeScale);

        return new LtCompWorldPos(
            (ushort)(x >> 3),
            (ushort)(y >> 2),
            (ushort)(z >> 3),
            (byte)(((z & 7) << 5) | ((y & 3) << 3) | (x & 7)));
    }

    private static uint QuantizeCompVectorComponent(float value, float majorAbs) =>
        (uint)TruncateLikeCvttss2si(MathF.Abs(value) / majorAbs * CompVectorEncodeScale);

    private static int QuantizeWorldComponent(float value, float min, float max, float scale)
    {
        var range = max - min;
        var normalized = range == 0f ? 0f : (value - min) / range * scale;
        if (normalized <= 0f)
            normalized = 0f;
        else if (normalized > scale)
            normalized = scale;

        return TruncateLikeCvttss2si(normalized + 0.5f);
    }

    private static int TruncateLikeCvttss2si(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return int.MinValue;

        if (value >= int.MaxValue)
            return int.MaxValue;

        if (value <= int.MinValue)
            return int.MinValue;

        return (int)value;
    }

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

    private readonly struct CompVector
    {
        public float FA { get; init; }
        public ushort DwB { get; init; }
        public ushort DwC { get; init; }
        public byte Order { get; init; }
    }
}

public readonly record struct LtCompWorldPos(ushort X, ushort Y, ushort Z, byte Extra);
