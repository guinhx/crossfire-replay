using System.Buffers.Binary;
using System.Text;
using CrossFire.Replay.Protocol;

namespace CrossFire.Replay.IO;

public sealed class CfrBinaryReader : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly bool _leaveOpen;

    public CfrBinaryReader(Stream stream, bool leaveOpen = false)
    {
        _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen);
        _leaveOpen = leaveOpen;
    }

    public Stream BaseStream => _reader.BaseStream;
    public long Position => _reader.BaseStream.Position;
    public long Length => _reader.BaseStream.Length;

    public void Seek(long offset, SeekOrigin origin) => _reader.BaseStream.Seek(offset, origin);

    public byte ReadByte() => _reader.ReadByte();
    public sbyte ReadSByte() => _reader.ReadSByte();
    public bool ReadBoolean() => _reader.ReadBoolean();
    public short ReadInt16() => _reader.ReadInt16();
    public ushort ReadUInt16() => _reader.ReadUInt16();
    public int ReadInt32() => _reader.ReadInt32();
    public uint ReadUInt32() => _reader.ReadUInt32();
    public long ReadInt64() => _reader.ReadInt64();
    public ulong ReadUInt64() => _reader.ReadUInt64();
    public float ReadSingle() => _reader.ReadSingle();

    public byte[] ReadBytes(int count) => _reader.ReadBytes(count);

    public byte[] ReadBytesExact(int count)
    {
        var data = _reader.ReadBytes(count);
        if (data.Length != count)
            throw new EndOfStreamException($"Expected {count} bytes, got {data.Length}.");
        return data;
    }

    public string ReadFixedString(int byteCount)
    {
        var bytes = ReadBytesExact(byteCount);
        var length = Array.IndexOf(bytes, (byte)0);
        if (length < 0)
            length = byteCount;
        return Encoding.UTF8.GetString(bytes, 0, length);
    }

    public Vector3F ReadVector3() =>
        new(ReadSingle(), ReadSingle(), ReadSingle());

    public byte ReadProtocolVersion(CfrReadContext ctx)
    {
        if (ctx.FileVersion >= 5)
            return ReadByte();
        return 0;
    }

    public byte[] ReadToEnd()
    {
        var remaining = (int)(Length - Position);
        if (remaining <= 0)
            return Array.Empty<byte>();
        return ReadBytesExact(remaining);
    }

    public void Dispose()
    {
        if (!_leaveOpen)
            _reader.Dispose();
    }
}

public static class CfrBinaryWriterExtensions
{
    public static void WriteVector3(this BinaryWriter writer, Vector3F v)
    {
        writer.Write(v.X);
        writer.Write(v.Y);
        writer.Write(v.Z);
    }

    public static void WriteFixedString(this BinaryWriter writer, string value, int byteCount)
    {
        var buffer = new byte[byteCount];
        var bytes = Encoding.UTF8.GetBytes(value);
        Array.Copy(bytes, buffer, Math.Min(bytes.Length, byteCount));
        writer.Write(buffer);
    }
}
