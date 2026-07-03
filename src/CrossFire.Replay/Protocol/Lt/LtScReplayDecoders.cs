namespace CrossFire.Replay.Protocol.Lt;

internal static class LtScReplayDecoders
{
    public static LtDecodedMessage? TryDecodeIngameItemDropped(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 28)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScIngameItemDropped)
                return null;

            var timestamp = reader.ReadUInt32();
            _ = reader.ReadUInt8();
            var dropKind = reader.ReadUInt8();
            reader.SkipBytes(8);
            var itemId = reader.ReadUInt16();
            _ = reader.ReadUInt8();
            var positionX = reader.ReadSingle();
            var fieldA = reader.ReadInt32();
            var fieldB = reader.IsEmpty ? (byte)0 : reader.ReadUInt8();

            return new LtScIngameItemDroppedDecoded(
                timestamp,
                dropKind,
                itemId,
                positionX,
                0f,
                fieldA,
                fieldB);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeDefenceTowerFire(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 24)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScAi2ModeDefenceTowerFire)
                return null;

            var timestamp = reader.ReadUInt32();
            var fieldA = reader.ReadUInt16();
            var fieldB = reader.ReadUInt16();
            var towerIndex = reader.ReadUInt16();
            var aimX = reader.ReadSingle();
            var aimY = reader.ReadSingle();
            var state = reader.ReadUInt32();

            return new LtScDefenceTowerFireDecoded(
                timestamp,
                fieldA,
                fieldB,
                towerIndex,
                aimX,
                aimY,
                state);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
