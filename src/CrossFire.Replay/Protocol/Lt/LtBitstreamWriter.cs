namespace CrossFire.Replay.Protocol.Lt;

/// <summary>
/// Lithtech-style bitstream writer matching <see cref="LtBitstreamReader"/>.
/// </summary>
public sealed class LtBitstreamWriter
{
    private readonly MemoryStream _stream = new();
    private byte _current;
    private int _bitPos;

    public byte[] ToArray()
    {
        FlushPartialByte();
        return _stream.ToArray();
    }

    public void WriteBoolean(bool value) => WriteBits(value ? 1u : 0u, 1);
    public void WriteUInt8(byte value) => WriteBits(value, 8);
    public void WriteUInt16(ushort value) => WriteBits(value, 16);
    public void WriteUInt32(uint value) => WriteBits(value, 32);
    public void WriteInt8(sbyte value) => WriteBits((uint)(byte)value, 8);
    public void WriteInt16(short value) => WriteBits((uint)(ushort)value, 16);
    public void WriteInt32(int value) => WriteBits((uint)value, 32);
    public void WriteUInt64(ulong value) => WriteBits(value, 64);

    public void WriteSingle(float value) =>
        WriteBits((uint)BitConverter.SingleToInt32Bits(value), 32);

    public void WriteVector3(Vector3F value)
    {
        WriteSingle(value.X);
        WriteSingle(value.Y);
        WriteSingle(value.Z);
    }

    public void WriteMessageId(EMessageId messageId) => WriteBits((ushort)messageId, 16);

    public void AlignToNextByte()
    {
        if (_bitPos != 0)
            FlushPartialByte();
    }

    public void WriteZeroBytes(int count)
    {
        if (count <= 0)
            return;

        AlignToNextByte();
        for (var i = 0; i < count; i++)
            _stream.WriteByte(0);
    }

    public void WriteBits(ulong value, int bitCount)
    {
        if (bitCount <= 0)
            return;

        var bitsRemaining = bitCount;
        var shift = 0;

        while (bitsRemaining > 0)
        {
            if (_bitPos == 8)
            {
                _stream.WriteByte(_current);
                _current = 0;
                _bitPos = 0;
            }

            var bitsLeftInByte = 8 - _bitPos;
            var bitsToWrite = Math.Min(bitsRemaining, bitsLeftInByte);
            var mask = (1u << bitsToWrite) - 1u;
            var chunk = (byte)((value >> shift) & mask);
            _current = (byte)(_current | (chunk << _bitPos));
            _bitPos += bitsToWrite;
            shift += bitsToWrite;
            bitsRemaining -= bitsToWrite;
        }
    }

    private void FlushPartialByte()
    {
        if (_bitPos == 0)
            return;

        _stream.WriteByte(_current);
        _current = 0;
        _bitPos = 0;
    }
}
