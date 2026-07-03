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

    public static LtDecodedMessage? TryDecodeDamageSiteState(ReadOnlySpan<byte> payload) =>
        LtNativeSerializers.TryDecodeDamageSiteState(payload);

    public static LtDecodedMessage? TryDecodeDamageSite(ReadOnlySpan<byte> payload) =>
        LtNativeSerializers.TryDecodeDamageSite(payload);

    public static LtDecodedMessage? TryDecodeAiScore(ReadOnlySpan<byte> payload) =>
        LtNativeSerializers.TryDecodeAiScore(payload);

    public static LtDecodedMessage? TryDecodeDamageCalculationRequest(ReadOnlySpan<byte> payload) =>
        LtNativeSerializers.TryDecodeDamageCalculationRequest(payload);

    public static LtDecodedMessage? TryDecodeMscNone4(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 2)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgMscNone4)
                return null;

            return new LtMscNone4Decoded();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeNjAiFireStart(ReadOnlySpan<byte> payload) =>
        LtNativeSerializers.TryDecodeNjAiFireStart(payload);

    public static LtDecodedMessage? TryDecodePlayerLevelUp(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 7)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScPlayerLevelUp)
                return null;

            var levelOrValue = reader.ReadUInt16();
            var characterIndex = reader.ReadUInt8();
            var amount = reader.ReadUInt16();
            return new LtScPlayerLevelUpDecoded(levelOrValue, characterIndex, amount, reader.HasRemaining);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeStageLightNodeClear(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 3)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScStageLightNodeClear)
                return null;

            var nodeIndex = reader.ReadUInt8();
            return new LtScStageLightNodeClearDecoded(nodeIndex, reader.HasRemaining);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeAiAckCanDefuseC4(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 2)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScAiAckCanDefuseC4)
                return null;

            return new LtScAiAckCanDefuseC4Decoded(reader.HasRemaining);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeSheepWantedList(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 3)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScSheepWantedList)
                return null;

            var wantedCount = reader.IsEmpty ? (byte)0 : reader.ReadUInt8();
            return new LtScSheepWantedListDecoded(wantedCount, reader.HasRemaining);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodePresentTeamAceUser(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 32)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScPresentTeamAceUser)
                return null;

            var fieldA = reader.ReadUInt32();
            var fieldB = reader.ReadUInt32();
            var timestamp = reader.ReadUInt32();
            var fieldC = reader.ReadUInt16();
            var fieldD = reader.ReadUInt16();
            var fieldE = reader.ReadUInt32();
            return new LtScPresentTeamAceUserDecoded(fieldA, fieldB, timestamp, fieldC, fieldD, fieldE, payload.Length > 32);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeForceLeavePollStart(ReadOnlySpan<byte> payload) =>
        LtNativeSerializers.TryDecodeForceLeavePollStart(payload);

    public static LtDecodedMessage? TryDecodeBossRevive(ReadOnlySpan<byte> payload) =>
        LtNativeSerializers.TryDecodeBossRevive(payload);

    public static LtDecodedMessage? TryDecodeArcadiaCoreSwitchState(ReadOnlySpan<byte> payload) =>
        LtNativeSerializers.TryDecodeArcadiaCoreSwitchState(payload);
}
