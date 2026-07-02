using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace CrossFire.Replay.Interop;

/// <summary>
/// Sequential byte reader modeled after Network.Packet.CBufferReader (IPGRA).
/// </summary>
public sealed class BufferReader
{
    private readonly byte[] _data;
    private int _pos;

    public BufferReader(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
    }

    public BufferReader(byte[] data)
    {
        _data = data;
    }

    public int Position => _pos;
    public int Length => _data.Length;
    public int Remaining => _data.Length - _pos;

    public void Reset() => _pos = 0;

    public void Skip(int count)
    {
        if (_pos + count > _data.Length)
            throw new InvalidOperationException("Buffer overrun while skipping.");

        _pos += count;
    }

    public byte ReadByte()
    {
        if (_pos >= _data.Length)
            throw new InvalidOperationException("Buffer overrun.");

        return _data[_pos++];
    }

    public bool ReadBoolean() => ReadByte() != 0;

    public short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(Read(2));
    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(Read(2));
    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(Read(4));
    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(Read(4));
    public float ReadSingle() => BinaryPrimitives.ReadSingleLittleEndian(Read(4));
    public long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(Read(8));

    public ReadOnlySpan<byte> ReadSpan(int count)
    {
        if (_pos + count > _data.Length)
            throw new InvalidOperationException("Buffer overrun.");

        var slice = _data.AsSpan(_pos, count);
        _pos += count;
        return slice;
    }

    public byte[] ReadBytes(int count) => ReadSpan(count).ToArray();

    public T ReadStruct<T>() where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();
        return StructReader.Read<T>(ReadSpan(size));
    }

    public string ReadNullTerminatedAscii(int maxLen)
    {
        var start = _pos;
        var end = Math.Min(_data.Length, start + maxLen);
        while (_pos < end && _data[_pos] != 0)
            _pos++;

        var slice = _data.AsSpan(start, _pos - start);
        if (_pos < _data.Length)
            _pos++;

        return Encoding.ASCII.GetString(slice);
    }

    private ReadOnlySpan<byte> Read(int count) => ReadSpan(count);
}
