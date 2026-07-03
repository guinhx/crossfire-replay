using CrossFire.Replay.Protocol;

namespace CrossFire.Replay.Protocol.Lt;

/// <summary>
/// Faithful ILT read/write aligned with CShell <c>GAMEPROTO_*::ReadData/WriteData</c>
/// and the shared <c>cf_client</c> protocol serializers.
/// </summary>
public static class LtNativeSerializers
{
    private static readonly Vector3F DefaultWorldMin = new(-4096f, -4096f, -4096f);
    private static readonly Vector3F DefaultWorldMax = new(4096f, 4096f, 4096f);

    private const ushort DamageSitePacketId = 0x100;
    private const ushort DamageSiteServerHeaderPacketId = 0xFF;
    private const ushort DamageSiteStatePacketId = 0x101;
    private const ushort DamageSiteStateServerHeaderPacketId = 0x100;
    private const ushort AiScorePacketId = 0x6D;
    private const ushort AiScoreServerHeaderPacketId = 0x6C;
    private const ushort RappelVelAndRotPacketId = 0x15A;
    private const ushort RappelVelAndRotServerHeaderPacketId = 0x159;
    private const ushort DamageCalculationRequestPacketId = 0xA1;
    private const ushort BossRevivePacketId = 0x30C;
    private const ushort ForceLeavePollStartPacketId = 0x93;
    private const ushort NjAiFireStartPacketId = 0x200;

    public static bool IsDamageSiteId(ushort id) =>
        id is DamageSitePacketId or DamageSiteServerHeaderPacketId;

    public static bool IsDamageSiteStateId(ushort id) =>
        id is DamageSiteStatePacketId or DamageSiteStateServerHeaderPacketId;

    public static bool IsAiScoreId(ushort id) =>
        id is AiScorePacketId or AiScoreServerHeaderPacketId;

    public static bool IsRappelVelAndRotId(ushort id) =>
        id is RappelVelAndRotPacketId or RappelVelAndRotServerHeaderPacketId;

    public static LtScDamageSiteDecoded? TryDecodeDamageSite(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if (!IsDamageSiteId(reader.ReadMessageId()))
                return null;

            var dimension = reader.ReadVector3();
            var damageSiteType = reader.ReadGuardedInt32();
            var renderEffect = reader.ReadBoolean();
            return new LtScDamageSiteDecoded(dimension, damageSiteType, renderEffect);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static byte[] EncodeDamageSite(LtScDamageSiteDecoded message)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScDamageSite);
        writer.WriteVector3(message.Dimension);
        writer.WriteInt32(message.DamageSiteType);
        writer.WriteBoolean(message.RenderEffect);
        return writer.ToArray();
    }

    public static LtScDamageSiteStateDecoded? TryDecodeDamageSiteState(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if (!IsDamageSiteStateId(reader.ReadMessageId()))
                return null;

            var objectId = reader.ReadObjectId();
            var isOn = reader.ReadBoolean();
            return new LtScDamageSiteStateDecoded(objectId, isOn);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static byte[] EncodeDamageSiteState(LtScDamageSiteStateDecoded message)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScDamageSiteState);
        writer.WriteUInt16(message.ObjectId);
        writer.WriteBoolean(message.IsOn);
        return writer.ToArray();
    }

    public static LtScAiScoreDecoded? TryDecodeAiScore(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if (!IsAiScoreId(reader.ReadMessageId()))
                return null;

            var botIndex = reader.ReadInt8();
            var health = reader.ReadUInt16();
            var armorPoint = reader.ReadUInt16();
            var numKill = reader.ReadUInt16();
            var numDeath = reader.ReadUInt16();
            var currentGameMoney = reader.ReadUInt16();
            return new LtScAiScoreDecoded(
                botIndex,
                health,
                armorPoint,
                numKill,
                numDeath,
                currentGameMoney);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static byte[] EncodeAiScore(LtScAiScoreDecoded message)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScAiScore);
        writer.WriteInt8(message.BotIndex);
        writer.WriteUInt16(message.Health);
        writer.WriteUInt16(message.ArmorPoint);
        writer.WriteUInt16(message.NumKill);
        writer.WriteUInt16(message.NumDeath);
        writer.WriteUInt16(message.CurrentGameMoney);
        return writer.ToArray();
    }

    public static LtCsRappelVelAndRotDecoded? TryDecodeRappelVelAndRot(ReadOnlySpan<byte> payload)
    {
        var entries = LtBundledReplayReader.DecodeEntries<LtCsRappelVelAndRotEntryDecoded>(
            payload,
            RappelVelAndRotServerHeaderPacketId,
            TryDecodeRappelVelAndRotEntryBundled);

        return entries is { Count: > 0 } ? new LtCsRappelVelAndRotDecoded(entries) : null;
    }

    public static byte[] EncodeRappelVelAndRot(LtCsRappelVelAndRotDecoded message)
    {
        var chunks = new List<byte[]>(message.Entries.Count);
        foreach (var entry in message.Entries)
            chunks.Add(EncodeRappelVelAndRotEntry(entry));

        var total = chunks.Sum(chunk => chunk.Length);
        var payload = new byte[total];
        var offset = 0;
        foreach (var chunk in chunks)
        {
            chunk.CopyTo(payload, offset);
            offset += chunk.Length;
        }

        return payload;
    }

    public static byte[] EncodeRappelVelAndRotEntry(LtCsRappelVelAndRotEntryDecoded entry)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgCsRappelVelAndRot);
        writer.WriteUInt8(entry.AreaIndex);
        writer.WriteUInt16((ushort)(sbyte)entry.CharacterIndex);
        LtCompressedVectorCodec.WriteCompLtVector(writer, entry.Velocity);
        LtCompressedVectorCodec.WriteCompPos(writer, entry.Position, DefaultWorldMin, DefaultWorldMax);
        writer.WriteSingle(entry.Ratio);
        return writer.ToArray();
    }

    public static bool TryDecodeRappelVelAndRotEntry(
        ReadOnlySpan<byte> payload,
        out LtCsRappelVelAndRotEntryDecoded entry,
        out int consumedBytes)
    {
        entry = default!;
        consumedBytes = 0;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if (!IsRappelVelAndRotId(reader.ReadMessageId()))
                return false;

            var areaIndex = reader.ReadUInt8();
            var characterIndex = (sbyte)reader.ReadUInt16();
            var velocity = LtCompressedVectorCodec.ReadCompLtVector(reader);
            var compPos = LtCompressedVectorCodec.ReadCompWorldPos(reader, includeExtraByte: false);
            var position = LtCompressedVectorCodec.DecodeCompWorldPos(
                compPos,
                DefaultWorldMin,
                DefaultWorldMax);
            var ratio = reader.ReadSingle();

            if (BitConverter.SingleToUInt32Bits(ratio) == 0x64646464u)
                return false;

            entry = new LtCsRappelVelAndRotEntryDecoded(
                areaIndex,
                characterIndex,
                velocity,
                position,
                ratio);
            consumedBytes = (reader.ConsumedBitCount + 7) / 8;
            return consumedBytes > 2;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static LtScDamageCalculationRequestDecoded? TryDecodeDamageCalculationRequest(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScDamageCalculationRequest)
                return null;

            var attacker = reader.ReadInt8();
            var shotPos = reader.ReadVector3();
            var weaponType = reader.ReadInt16();
            var gunDirX = reader.ReadSingle();
            var gunDirY = reader.ReadSingle();
            var gunDirZ = reader.ReadSingle();
            var gunDirW = reader.ReadSingle();
            var gunRotAddY = reader.ReadSingle();
            var lastTargetSize = reader.ReadSingle();
            var curZoomStep = reader.ReadUInt16();
            var curDetailMoveIndex = reader.ReadInt32();
            var gunRotLeftCenterRight = reader.ReadUInt8();
            var playerInAirState = reader.ReadInt32();
            var gvCurrMoveTypeFlag = reader.ReadInt32();
            var inLadder = reader.ReadBoolean();
            var matchItemRecoilless = reader.ReadSingle();

            var thirdPartyPositions = new List<Vector3F>(16);
            for (var i = 0; i < 16 && reader.RemainingBits >= 96; i++)
                thirdPartyPositions.Add(reader.ReadVector3());

            return new LtScDamageCalculationRequestDecoded(
                attacker,
                shotPos,
                weaponType,
                gunDirX,
                gunDirY,
                gunDirZ,
                gunDirW,
                gunRotAddY,
                lastTargetSize,
                curZoomStep,
                curDetailMoveIndex,
                gunRotLeftCenterRight,
                playerInAirState,
                gvCurrMoveTypeFlag,
                inLadder,
                matchItemRecoilless,
                thirdPartyPositions,
                reader.HasRemaining);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static byte[] EncodeDamageCalculationRequest(LtScDamageCalculationRequestDecoded message)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScDamageCalculationRequest);
        writer.WriteInt8(message.Attacker);
        writer.WriteVector3(message.ShotPos);
        writer.WriteInt16(message.WeaponType);
        writer.WriteSingle(message.GunDirX);
        writer.WriteSingle(message.GunDirY);
        writer.WriteSingle(message.GunDirZ);
        writer.WriteSingle(message.GunDirW);
        writer.WriteSingle(message.GunRotAddY);
        writer.WriteSingle(message.LastTargetSize);
        writer.WriteUInt16(message.CurZoomStep);
        writer.WriteInt32(message.CurDetailMoveIndex);
        writer.WriteUInt8(message.GunRotLeftCenterRight);
        writer.WriteInt32(message.PlayerInAirState);
        writer.WriteInt32(message.GvCurrMoveTypeFlag);
        writer.WriteBoolean(message.InLadder);
        writer.WriteSingle(message.MatchItemRecoilless);

        for (var i = 0; i < 16; i++)
        {
            var position = i < message.ThirdPartyPositions.Count
                ? message.ThirdPartyPositions[i]
                : new Vector3F(0f, 0f, 0f);
            writer.WriteVector3(position);
        }

        return writer.ToArray();
    }

    public static LtScNjAiFireStartDecoded? TryDecodeNjAiFireStart(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScNjAiFireStart)
                return null;

            if (payload.Length <= 10)
            {
                return new LtScNjAiFireStartDecoded(
                    payload.Length > 5 ? payload[5] : (byte)0,
                    payload.Length > 3 ? payload[3] : (byte)0,
                    payload.Length > 8);
            }

            var attackObject = reader.ReadObjectId();
            var targetObject = reader.ReadObjectId();
            var weaponType = reader.ReadInt16();
            return new LtScNjAiFireStartDecoded(
                (byte)weaponType,
                (byte)attackObject,
                reader.HasRemaining);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static byte[] EncodeNjAiFireStart(LtScNjAiFireStartDecoded message)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScNjAiFireStart);

        if (message.HasTrailingData || message.FieldB != 0)
        {
            writer.WriteUInt16(message.FieldB);
            writer.WriteUInt16(0);
            writer.WriteInt16((short)message.FieldA);
            return writer.ToArray();
        }

        writer.WriteZeroBytes(3);
        writer.WriteUInt8(message.FieldA);
        writer.WriteZeroBytes(2);
        return writer.ToArray();
    }

    public static LtScBossReviveDecoded? TryDecodeBossRevive(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 24)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScBossRevive)
                return null;

            var entries = LtBundledReplayReader.DecodeEntries<LtScBossReviveEntryDecoded>(
                payload,
                BossRevivePacketId,
                TryDecodeBossReviveEntryBundled);

            return entries is { Count: > 0 } ? new LtScBossReviveDecoded(entries) : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static byte[] EncodeBossRevive(LtScBossReviveDecoded message)
    {
        var chunks = new List<byte[]>(message.Entries.Count);
        foreach (var entry in message.Entries)
            chunks.Add(EncodeBossReviveEntry(entry));

        var total = chunks.Sum(chunk => chunk.Length);
        var payload = new byte[total];
        var offset = 0;
        foreach (var chunk in chunks)
        {
            chunk.CopyTo(payload, offset);
            offset += chunk.Length;
        }

        return payload;
    }

    public static byte[] EncodeBossReviveEntry(LtScBossReviveEntryDecoded entry)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScBossRevive);
        writer.WriteUInt32(entry.Timestamp);
        writer.WriteUInt32(entry.FieldA);
        writer.WriteUInt32(entry.FieldB);
        writer.WriteUInt32(entry.EventId);
        writer.WriteUInt32(entry.StateA);
        writer.WriteUInt32(entry.StateB);
        return writer.ToArray();
    }

    public static bool TryDecodeBossReviveEntry(
        ReadOnlySpan<byte> payload,
        out LtScBossReviveEntryDecoded entry,
        out int consumedBytes)
    {
        entry = default!;
        consumedBytes = 0;

        if (payload.Length < 24)
            return false;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScBossRevive)
                return false;

            var timestamp = reader.ReadUInt32();
            var fieldA = reader.ReadUInt32();
            var fieldB = reader.ReadUInt32();
            var eventId = reader.ReadUInt32();
            var stateA = reader.ReadUInt32();
            var stateB = reader.RemainingBits >= 32 ? reader.ReadUInt32() : 0u;

            entry = new LtScBossReviveEntryDecoded(timestamp, fieldA, fieldB, eventId, stateA, stateB);
            consumedBytes = (reader.ConsumedBitCount + 7) / 8;
            return consumedBytes > 2;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static LtScForceLeavePollStartDecoded? TryDecodeForceLeavePollStart(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
            return null;

        try
        {
            if (payload.Length <= 32 && TryDecodeForceLeavePollStartNative(payload, out var native))
                return native;

            return TryDecodeForceLeavePollStartBundled(payload);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static byte[] EncodeForceLeavePollStart(LtScForceLeavePollStartDecoded message)
    {
        var chunks = new List<byte[]>(message.Entries.Count);
        foreach (var entry in message.Entries)
            chunks.Add(entry.IsReplayBundledLayout
                ? EncodeForceLeavePollStartBundledEntry(entry)
                : EncodeForceLeavePollStartNativeEntry(entry));

        var total = chunks.Sum(chunk => chunk.Length);
        var payload = new byte[total];
        var offset = 0;
        foreach (var chunk in chunks)
        {
            chunk.CopyTo(payload, offset);
            offset += chunk.Length;
        }

        return payload;
    }

    private static LtScForceLeavePollStartDecoded? TryDecodeForceLeavePollStartBundled(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 14)
            return null;

        var reader = new LtBitstreamReader(payload);
        if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScForceLeavePollStart)
            return null;

        var entries = LtBundledReplayReader.DecodeEntries<LtScForceLeavePollStartEntryDecoded>(
            payload,
            ForceLeavePollStartPacketId,
            TryDecodeForceLeavePollStartEntryBundled);

        return entries is { Count: > 0 } ? new LtScForceLeavePollStartDecoded(entries) : null;
    }

    private static bool TryDecodeForceLeavePollStartNative(
        ReadOnlySpan<byte> payload,
        out LtScForceLeavePollStartDecoded? decoded)
    {
        decoded = null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScForceLeavePollStart)
                return false;

            var requester = reader.ReadFixedString(13);
            var target = reader.ReadFixedString(13);
            if (!reader.HasRemaining)
                return false;

            var reason = reader.ReadUInt8();
            var entry = new LtScForceLeavePollStartEntryDecoded(
                0,
                0,
                0,
                requester,
                target,
                reason,
                IsReplayBundledLayout: false);
            decoded = new LtScForceLeavePollStartDecoded([entry]);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static byte[] EncodeForceLeavePollStartNativeEntry(LtScForceLeavePollStartEntryDecoded entry)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScForceLeavePollStart);
        writer.WriteFixedString(entry.RequesterName, 13);
        writer.WriteFixedString(entry.TargetName, 13);
        writer.WriteUInt8(entry.ReasonNum);
        return writer.ToArray();
    }

    private static byte[] EncodeForceLeavePollStartBundledEntry(LtScForceLeavePollStartEntryDecoded entry)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScForceLeavePollStart);
        writer.WriteUInt32(entry.FieldA);
        writer.WriteUInt32(entry.FieldB);
        writer.WriteUInt32(entry.Timestamp);
        return writer.ToArray();
    }

    public static bool TryDecodeForceLeavePollStartEntry(
        ReadOnlySpan<byte> payload,
        out LtScForceLeavePollStartEntryDecoded entry,
        out int consumedBytes)
    {
        entry = default!;
        consumedBytes = 0;

        if (payload.Length < 14)
            return false;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScForceLeavePollStart)
                return false;

            var fieldA = reader.ReadUInt32();
            var fieldB = reader.ReadUInt32();
            var timestamp = reader.ReadUInt32();
            entry = new LtScForceLeavePollStartEntryDecoded(fieldA, fieldB, timestamp);
            consumedBytes = (reader.ConsumedBitCount + 7) / 8;
            return consumedBytes > 2;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryDecodeRappelVelAndRotEntryBundled(
        ReadOnlySpan<byte> slice,
        int _,
        out LtCsRappelVelAndRotEntryDecoded entry,
        out int consumedBytes) =>
        TryDecodeRappelVelAndRotEntry(slice, out entry, out consumedBytes);

    private static bool TryDecodeBossReviveEntryBundled(
        ReadOnlySpan<byte> slice,
        int _,
        out LtScBossReviveEntryDecoded entry,
        out int consumedBytes) =>
        TryDecodeBossReviveEntry(slice, out entry, out consumedBytes);

    private static bool TryDecodeForceLeavePollStartEntryBundled(
        ReadOnlySpan<byte> slice,
        int _,
        out LtScForceLeavePollStartEntryDecoded entry,
        out int consumedBytes) =>
        TryDecodeForceLeavePollStartEntry(slice, out entry, out consumedBytes);

    private const int ArcadiaCoreSwitchStateMinPayloadBytes = 125;
    private const int ArcadiaCoreSwitchStatePrimaryRecordBytes = 140;
    private const int ArcadiaContextExtensionBytes = 33;
    private const int ArcadiaLiveWirePayloadBytes = 14;
    private const int LadderAreaFullPayloadBytes = 44;

    public static LtScArcadiaCoreSwitchStateDecoded? TryDecodeArcadiaCoreSwitchState(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScArcadiaCoreSwitchState)
                return null;

            if (payload.Length <= ArcadiaLiveWirePayloadBytes)
                return DecodeArcadiaLiveWire(reader, payload.Length);

            if (payload.Length < ArcadiaCoreSwitchStateMinPayloadBytes)
                return null;

            return DecodeArcadiaReplayArchival(reader, payload.Length);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static LtScArcadiaCoreSwitchStateDecoded DecodeArcadiaLiveWire(LtBitstreamReader reader, int payloadLength)
    {
        var switchSlot = reader.ReadUInt32();
        var coreTableKey = reader.ReadInt32();
        var curHp = reader.ReadUInt32();

        return new LtScArcadiaCoreSwitchStateDecoded(
            CoreObjectId: 0,
            EventKind: (byte)switchSlot,
            Timestamp: 0,
            SubState: 0,
            PayloadLength: payloadLength,
            CurHp: curHp,
            SwitchSlot: switchSlot,
            CoreTableKey: coreTableKey,
            IsReplayArchivalLayout: false);
    }

    private static LtScArcadiaCoreSwitchStateDecoded DecodeArcadiaReplayArchival(LtBitstreamReader reader, int payloadLength)
    {
        for (var i = 0; i < 10 && reader.HasRemaining; i++)
            reader.ReadUInt32();

        var sentinelA = reader.ReadInt32();
        var sentinelB = reader.ReadInt32();

        var contextExtension = new byte[ArcadiaContextExtensionBytes];
        for (var i = 0; i < ArcadiaContextExtensionBytes; i++)
            contextExtension[i] = reader.ReadUInt8();

        var orientation = LtCompressedVectorCodec.ReadCompLtVector(reader);
        var compPos = LtCompressedVectorCodec.ReadCompWorldPos(reader, includeExtraByte: true);
        var position = LtCompressedVectorCodec.DecodeCompWorldPos(
            compPos,
            DefaultWorldMin,
            DefaultWorldMax);

        var guardByte = reader.ReadUInt8();
        if (reader.RemainingBits >= 8)
            reader.ReadUInt8();

        var validMask = reader.ReadUInt32();
        if (reader.RemainingBits >= 16)
            reader.SkipBytes(2);

        var coreObjectId = reader.ReadUInt16();
        if (reader.RemainingBits >= 16)
            reader.SkipBytes(2);

        var curHp = reader.ReadUInt32();

        var eventKind = reader.ReadUInt8();
        if (reader.RemainingBits >= 24)
            reader.SkipBytes(3);

        var subState = reader.ReadUInt8();
        if (reader.HasRemaining)
            reader.ReadUInt8();

        var timestamp = reader.ReadUInt32();
        var auxField = reader.RemainingBits >= 32 ? reader.ReadUInt32() : 0u;

        if (reader.RemainingBits >= 56)
            reader.SkipBytes(7);

        var relatedObjectId = reader.RemainingBits >= 16 ? reader.ReadUInt16() : (ushort)0;

        return new LtScArcadiaCoreSwitchStateDecoded(
            coreObjectId,
            eventKind,
            timestamp,
            subState,
            payloadLength,
            curHp,
            SwitchSlot: 0,
            CoreTableKey: 0,
            ReplaySentinelA: sentinelA,
            ReplaySentinelB: sentinelB,
            ContextExtension: contextExtension,
            Orientation: orientation,
            Position: position,
            GuardByte: guardByte,
            ValidMask: validMask,
            AuxField: auxField,
            RelatedObjectId: relatedObjectId,
            IsReplayArchivalLayout: true);
    }

    public static byte[] EncodeArcadiaCoreSwitchState(LtScArcadiaCoreSwitchStateDecoded message) =>
        message.IsReplayArchivalLayout
            ? EncodeArcadiaReplayArchival(message)
            : EncodeArcadiaLiveWire(message);

    private static byte[] EncodeArcadiaLiveWire(LtScArcadiaCoreSwitchStateDecoded message)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScArcadiaCoreSwitchState);
        writer.WriteUInt32(message.SwitchSlot);
        writer.WriteInt32(message.CoreTableKey);
        writer.WriteUInt32(message.CurHp);
        return writer.ToArray();
    }

    private static byte[] EncodeArcadiaReplayArchival(LtScArcadiaCoreSwitchStateDecoded message)
    {
        var payloadLength = Math.Max(message.PayloadLength, ArcadiaCoreSwitchStatePrimaryRecordBytes);
        var payload = new byte[payloadLength];
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScArcadiaCoreSwitchState);

        for (var i = 0; i < 10; i++)
            writer.WriteUInt32(0);

        writer.WriteInt32(message.ReplaySentinelA);
        writer.WriteInt32(message.ReplaySentinelB);

        var contextExtension = message.ContextExtension ?? new byte[ArcadiaContextExtensionBytes];
        for (var i = 0; i < ArcadiaContextExtensionBytes; i++)
            writer.WriteUInt8(i < contextExtension.Length ? contextExtension[i] : (byte)0);

        LtCompressedVectorCodec.WriteCompLtVector(writer, message.Orientation);
        LtCompressedVectorCodec.WriteCompPos(writer, message.Position, DefaultWorldMin, DefaultWorldMax, includeExtraByte: true);
        writer.WriteUInt8(message.GuardByte);
        writer.WriteZeroBytes(1);
        writer.WriteUInt32(message.ValidMask);
        writer.WriteZeroBytes(2);

        writer.WriteUInt16(message.CoreObjectId);
        writer.WriteZeroBytes(2);
        writer.WriteUInt32(message.CurHp);
        writer.WriteUInt8(message.EventKind);
        writer.WriteZeroBytes(3);
        writer.WriteUInt8(message.SubState);
        writer.WriteUInt8(0);
        writer.WriteUInt32(message.Timestamp);
        writer.WriteUInt32(message.AuxField);
        writer.WriteZeroBytes(7);
        writer.WriteUInt16(message.RelatedObjectId);

        var primary = writer.ToArray();
        primary.AsSpan(0, Math.Min(primary.Length, payload.Length)).CopyTo(payload);
        return payload;
    }

    public static LtLadderAreaDecoded? TryDecodeLadderArea(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 2)
            return null;

        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScLadderArea)
                return null;

            if (!reader.HasRemaining)
            {
                return new LtLadderAreaDecoded(
                    0,
                    new Vector3F(0, 0, 0),
                    new Vector3F(0, 0, 0),
                    new Vector3F(0, 0, 0),
                    0,
                    false);
            }

            if (payload.Length < LadderAreaFullPayloadBytes)
                return DecodeCompactLadderArea(reader);

            var objectId = reader.ReadObjectId();
            var position = reader.ReadVector3();
            var rotation = new Vector3F(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            reader.ReadSingle();
            var dimensions = reader.ReadVector3();
            var ladderType = reader.ReadUInt8();

            return new LtLadderAreaDecoded(objectId, position, rotation, dimensions, ladderType);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static byte[] EncodeLadderArea(LtLadderAreaDecoded message)
    {
        var writer = new LtBitstreamWriter();
        writer.WriteMessageId(EMessageId.MsgScLadderArea);

        if (!message.HasFullGeometry)
        {
            writer.WriteZeroBytes(6);
            writer.WriteUInt32((uint)message.Position.X);
            writer.WriteUInt32(0);
            writer.WriteUInt8(message.LadderType);
            return writer.ToArray();
        }

        writer.WriteUInt16(message.ObjectId);
        writer.WriteVector3(message.Position);
        writer.WriteSingle(message.Rotation.X);
        writer.WriteSingle(message.Rotation.Y);
        writer.WriteSingle(message.Rotation.Z);
        writer.WriteSingle(0f);
        writer.WriteVector3(message.Dimensions);
        writer.WriteUInt8(message.LadderType);
        return writer.ToArray();
    }

    private static LtLadderAreaDecoded DecodeCompactLadderArea(LtBitstreamReader reader)
    {
        if (reader.RemainingBits >= 48)
            reader.SkipBytes(6);

        var areaIndex = reader.RemainingBits >= 32 ? reader.ReadUInt32() : 0u;
        if (reader.RemainingBits >= 32)
            reader.ReadUInt32();

        var ladderType = reader.RemainingBits >= 8 ? reader.ReadUInt8() : (byte)0;

        return new LtLadderAreaDecoded(
            0,
            new Vector3F(areaIndex, 0, 0),
            new Vector3F(0, 0, 0),
            new Vector3F(0, 0, 0),
            ladderType,
            false);
    }
}
