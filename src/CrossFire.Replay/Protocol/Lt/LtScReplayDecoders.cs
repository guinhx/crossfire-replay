namespace CrossFire.Replay.Protocol.Lt;

internal static class LtScReplayDecoders
{
    private const int BossReviveMinEntryBytes = 24;
    private const int ArcadiaSwitchStateMinBytes = 125;

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

    public static LtDecodedMessage? TryDecodeDefenceTowerChangeState(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 28)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScAi2ModeDefenceTowerChangeState)
                return null;

            var timestamp = reader.ReadUInt32();
            var fieldA = reader.ReadUInt16();
            var fieldB = reader.ReadUInt16();
            var towerIndex = reader.ReadUInt16();
            var stateA = reader.ReadUInt32();
            var stateB = reader.ReadUInt32();
            var stateC = reader.ReadUInt32();
            var stateD = reader.ReadUInt32();
            var hasTrailing = reader.HasRemaining;

            return new LtScDefenceTowerChangeStateDecoded(
                timestamp,
                fieldA,
                fieldB,
                towerIndex,
                stateA,
                stateB,
                stateC,
                stateD,
                hasTrailing);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeCheatScaleDown(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 10)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScCheatScaleDown)
                return null;

            var scale = reader.ReadSingle();
            var sendIndex = reader.ReadInt32();
            return new LtScCheatScaleDownDecoded(scale, sendIndex, reader.HasRemaining);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeAddTimeItem(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 2)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScAddTimeItem)
                return null;

            if (!reader.HasRemaining)
                return new LtScAddTimeItemDecoded(false, null, null, null);

            var characterIndex = reader.ReadUInt8();
            if (!reader.HasRemaining)
                return new LtScAddTimeItemDecoded(true, characterIndex, null, null);

            var itemType = reader.ReadUInt8();
            if (!reader.HasRemaining)
                return new LtScAddTimeItemDecoded(true, characterIndex, itemType, null);

            var ratio = reader.ReadSingle();
            return new LtScAddTimeItemDecoded(true, characterIndex, itemType, ratio);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeDamageSiteState(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScDamageSiteState)
                return null;

            var objectHandle = (uint)reader.ReadUInt16();
            var isOn = reader.HasRemaining && reader.ReadUInt8() != 0;
            return new LtScDamageSiteStateDecoded(objectHandle, isOn, reader.HasRemaining);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeBossRevive(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < BossReviveMinEntryBytes)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScBossRevive)
                return null;

            var entries = new List<LtScBossReviveEntryDecoded>();
            foreach (var offset in FindMessageIdOffsets(payload, EMessageId.MsgScBossRevive))
            {
                var end = offset + BossReviveMinEntryBytes <= payload.Length
                    ? FindNextMessageIdOffset(payload, EMessageId.MsgScBossRevive, offset + 2)
                    : payload.Length;
                if (end < 0)
                    end = payload.Length;

                var slice = payload.Slice(offset, end - offset);
                if (TryDecodeBossReviveEntry(slice, out var entry))
                    entries.Add(entry);
            }

            return entries.Count > 0 ? new LtScBossReviveDecoded(entries) : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeArcadiaCoreSwitchState(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < ArcadiaSwitchStateMinBytes)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScArcadiaCoreSwitchState)
                return null;

            var coreObjectId = BitConverter.ToUInt16(payload.Slice(107, 2));
            var eventKind = payload[115];
            var timestamp = BitConverter.ToUInt32(payload.Slice(121, 4));
            var subState = payload[119];

            return new LtScArcadiaCoreSwitchStateDecoded(
                coreObjectId,
                eventKind,
                timestamp,
                subState,
                payload.Length);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static bool TryDecodeBossReviveEntry(ReadOnlySpan<byte> slice, out LtScBossReviveEntryDecoded entry)
    {
        entry = default!;
        if (slice.Length < BossReviveMinEntryBytes)
            return false;

        try
        {
            var reader = new LtBitstreamReader(slice);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScBossRevive)
                return false;

            var timestamp = reader.ReadUInt32();
            var fieldA = reader.ReadUInt32();
            var fieldB = reader.ReadUInt32();
            var eventId = reader.ReadUInt32();
            var stateA = reader.ReadUInt32();
            var stateB = reader.RemainingBits >= 32 ? reader.ReadUInt32() : 0u;

            entry = new LtScBossReviveEntryDecoded(timestamp, fieldA, fieldB, eventId, stateA, stateB);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static List<int> FindMessageIdOffsets(ReadOnlySpan<byte> payload, EMessageId messageId)
    {
        var lo = (byte)((ushort)messageId & 0xFF);
        var hi = (byte)((ushort)messageId >> 8);
        var offsets = new List<int>();

        for (var i = 0; i < payload.Length - 1; i++)
        {
            if (payload[i] == lo && payload[i + 1] == hi)
                offsets.Add(i);
        }

        return offsets;
    }

    private static int FindNextMessageIdOffset(ReadOnlySpan<byte> payload, EMessageId messageId, int start)
    {
        var lo = (byte)((ushort)messageId & 0xFF);
        var hi = (byte)((ushort)messageId >> 8);

        for (var i = start; i < payload.Length - 1; i++)
        {
            if (payload[i] == lo && payload[i + 1] == hi)
                return i;
        }

        return -1;
    }
}
