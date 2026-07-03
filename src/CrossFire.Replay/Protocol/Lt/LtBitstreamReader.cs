namespace CrossFire.Replay.Protocol.Lt;

/// <summary>
/// Lithtech-style bitstream reader (LSB-first within each byte).
/// Bits are read from each byte starting at bit 0 (LSB-first within the byte).
/// </summary>
public sealed class LtBitstreamReader
{
    public const uint InvalidSentinel = 0x64646464;

    private readonly byte[] _data;
    private int _bytePos;
    private int _bitPos;

    public LtBitstreamReader(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
    }

    public int BytePosition => _bytePos;
    public int BitPosition => _bitPos;
    public bool IsEmpty => EffectiveBytePosition >= _data.Length;
    public bool HasRemaining => EffectiveBytePosition < _data.Length;
    public int RemainingBits
    {
        get
        {
            if (EffectiveBytePosition >= _data.Length)
                return 0;

            return (_data.Length - EffectiveBytePosition) * 8 - _bitPos;
        }
    }

    private int EffectiveBytePosition => _bitPos == 8 ? _bytePos + 1 : _bytePos;

    public bool ReadBoolean() => ReadBits(1) != 0;
    public byte ReadUInt8() => (byte)ReadBits(8);
    public ushort ReadUInt16() => (ushort)ReadBits(16);
    public uint ReadUInt32() => ReadBits(32);
    public sbyte ReadInt8() => (sbyte)ReadBits(8);
    public short ReadInt16() => (short)ReadBits(16);
    public int ReadInt32() => (int)ReadBits(32);

    public float ReadSingle()
    {
        var bits = ReadBits(32);
        return BitConverter.Int32BitsToSingle((int)bits);
    }

    public ulong ReadUInt64() => ReadBits(64);

    public Vector3F ReadVector3() =>
        new(ReadSingle(), ReadSingle(), ReadSingle());

    public ushort ReadObjectId() => ReadUInt16();

    public uint ReadGuardedUInt32()
    {
        var value = ReadUInt32();
        if (value == InvalidSentinel)
            throw new InvalidOperationException("LT read sentinel.");
        return value;
    }

    public int ReadGuardedInt32() => (int)ReadGuardedUInt32();

    public float ReadGuardedSingle()
    {
        var bits = ReadBits(32);
        if (bits == InvalidSentinel)
            throw new InvalidOperationException("LT read sentinel.");
        return BitConverter.Int32BitsToSingle((int)bits);
    }

    public string ReadFixedString(int maxChars)
    {
        var chars = new char[maxChars];
        var length = 0;
        for (var i = 0; i < maxChars; i++)
        {
            var ch = (char)ReadUInt8();
            if (ch == '\0')
                break;
            chars[length++] = ch;
        }

        return new string(chars, 0, length);
    }

    public void ReadLtRotationQuat()
    {
        ReadSingle();
        ReadSingle();
        ReadSingle();
        ReadSingle();
    }

    public uint ReadBits(int bitCount)
    {
        if (bitCount <= 0)
            return 0;

        var result = 0u;
        var bitsRead = 0;
        var bitsRemaining = bitCount;

        while (bitsRemaining > 0)
        {
            if (_bytePos >= _data.Length)
                throw new InvalidOperationException("LT bitstream overrun.");

            if (_bitPos == 8)
            {
                _bytePos++;
                _bitPos = 0;
                if (_bytePos >= _data.Length)
                    throw new InvalidOperationException("LT bitstream overrun.");
            }

            var bitsLeftInByte = 8 - _bitPos;
            var bitsToRead = Math.Min(bitsRemaining, bitsLeftInByte);
            var mask = (1u << bitsToRead) - 1u;
            var chunk = (_data[_bytePos] >> _bitPos) & mask;
            result |= (uint)(chunk << bitsRead);
            _bitPos += bitsToRead;
            bitsRead += bitsToRead;
            bitsRemaining -= bitsToRead;
        }

        NormalizeByteAlignment();
        return result;
    }

    private void NormalizeByteAlignment()
    {
        if (_bitPos == 8)
        {
            _bytePos++;
            _bitPos = 0;
        }
    }

    public ushort ReadMessageId() => (ushort)ReadBits(16);

    public void AlignToNextByte()
    {
        if (_bitPos == 0)
            return;

        _bytePos++;
        _bitPos = 0;
    }

    public void SkipBytes(int count)
    {
        if (count <= 0)
            return;

        AlignToNextByte();
        if (_bytePos + count > _data.Length)
            throw new InvalidOperationException("LT bitstream overrun.");

        _bytePos += count;
    }
}
