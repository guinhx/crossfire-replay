namespace CrossFire.Replay.Protocol.Lt;

internal static class LtSemanticDecoders
{
    public static LtDecodedMessage? TryDecodeScore(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScScore)
                return null;

            return new LtScoreDecoded(
                reader.ReadUInt16(),
                reader.ReadGuardedUInt32(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16());
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeRoundTimeLeft(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScRoundTimeLeft)
                return null;

            var roundTime = reader.ReadGuardedSingle();
            var health = new ushort[16];
            for (var i = 0; i < health.Length; i++)
                health[i] = reader.ReadUInt16();

            return new LtRoundTimeLeftDecoded(roundTime, health);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeAllScores(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScAllScores)
                return null;

            var teamCount = reader.ReadUInt8();
            if (teamCount is 0 or > 8)
                return null;

            var teamScores = new ushort[teamCount];
            for (var i = 0; i < teamCount; i++)
                teamScores[i] = reader.ReadUInt16();

            var playerCount = reader.ReadUInt8();
            if (playerCount > 50)
                return null;

            var players = new List<LtAllScoresPlayerDecoded>(playerCount);
            for (var i = 0; i < playerCount; i++)
            {
                var clientIndex = reader.ReadUInt8();
                var characterIndex = reader.ReadUInt8();
                var health = reader.ReadUInt16();
                var kills = reader.ReadUInt16();
                var deaths = reader.ReadUInt16();
                var goalInCount = (short)reader.ReadUInt16();
                reader.ReadUInt16();
                var totalDamage = reader.ReadGuardedUInt32();
                var ping = reader.ReadUInt16();
                var teamIndex = reader.ReadUInt8();
                var userId = reader.ReadUInt64();
                var escapeCount = reader.ReadUInt16();
                var aiKillScore = reader.ReadGuardedUInt32();
                reader.ReadUInt16();
                reader.ReadUInt16();
                reader.ReadUInt16();
                reader.ReadGuardedUInt32();
                reader.ReadGuardedUInt32();
                reader.ReadGuardedUInt32();
                reader.ReadUInt16();
                reader.ReadUInt16();
                reader.ReadGuardedUInt32();
                var soldierType = reader.ReadUInt8();
                reader.ReadGuardedUInt32();
                reader.ReadGuardedUInt32();
                reader.ReadGuardedUInt32();
                reader.ReadUInt64();
                reader.ReadGuardedUInt32();
                reader.ReadUInt64();

                players.Add(new LtAllScoresPlayerDecoded(
                    clientIndex,
                    characterIndex,
                    teamIndex,
                    health,
                    kills,
                    deaths,
                    goalInCount,
                    totalDamage,
                    ping,
                    userId,
                    escapeCount,
                    aiKillScore,
                    soldierType));
            }

            var aceUserId = reader.ReadUInt64();
            var topEscapeUserId = reader.ReadUInt64();
            var totalKills = reader.ReadGuardedInt32();
            var killCountsForWin = reader.ReadGuardedInt32();
            var currentLevel = reader.ReadGuardedInt32();
            var activeBomberScore = reader.ReadUInt16();
            var firstHalf = new ushort[2];
            firstHalf[0] = reader.ReadUInt16();
            firstHalf[1] = reader.ReadUInt16();

            return new LtAllScoresDecoded(
                teamCount,
                teamScores,
                playerCount,
                players,
                aceUserId,
                topEscapeUserId,
                totalKills,
                killCountsForWin,
                currentLevel,
                activeBomberScore,
                firstHalf);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeThrowGrenade(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScThrowGrenade)
                return null;

            var throwPos = reader.ReadVector3();
            var velocity = reader.ReadVector3();
            var weaponType = reader.ReadUInt16();
            var characterIndex = reader.ReadUInt8();
            var previousCreated = reader.ReadBoolean();
            var createTime = reader.ReadGuardedSingle();
            var createBoomTime = reader.ReadGuardedSingle();
            var boomDuration = reader.ReadGuardedSingle();
            var boomStatus = reader.ReadBoolean();
            var objectId = reader.ReadGuardedUInt32();
            reader.ReadBoolean();
            reader.ReadUInt8();
            reader.ReadUInt8();
            reader.ReadUInt8();
            reader.ReadUInt16();
            reader.ReadUInt8();
            reader.ReadUInt8();

            return new LtThrowGrenadeDecoded(
                throwPos,
                velocity,
                weaponType,
                characterIndex,
                previousCreated,
                createTime,
                createBoomTime,
                boomDuration,
                boomStatus,
                objectId);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeShotInfo(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScShotInfo)
                return null;

            return new LtShotInfoDecoded(
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16());
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeSetCurWeapon(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScSetCurWeapon)
                return null;

            var characterIndex = reader.ReadUInt8();
            var objectId = reader.ReadObjectId();
            var weaponObjectId = reader.ReadObjectId();
            var weaponType = reader.ReadUInt16();
            var selectedFuncItem = reader.ReadUInt16();

            return new LtSetCurWeaponDecoded(
                characterIndex,
                objectId,
                weaponObjectId,
                weaponType,
                selectedFuncItem);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeLadderArea(ReadOnlySpan<byte> payload) =>
        LtNativeSerializers.TryDecodeLadderArea(payload);

    public static LtDecodedMessage? TryDecodePlayerRespawn(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScPlayerRespawn)
                return null;

            return new LtPlayerRespawnDecoded(
                reader.ReadUInt8(),
                reader.ReadUInt8(),
                reader.ReadObjectId(),
                reader.ReadVector3(),
                reader.ReadGuardedInt32(),
                reader.ReadUInt8(),
                reader.ReadUInt8(),
                reader.ReadBoolean());
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodePlayerIn(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScPlayerIn)
                return null;

            var characterIndex = reader.ReadUInt8();
            var teamIndex = reader.ReadUInt8();
            var objectId = reader.ReadObjectId();
            var userName = reader.ReadFixedString(13);
            if (userName.Length > 12 || !IsPlausiblePlayerName(userName))
                return null;

            var repCount = reader.ReadUInt8();
            for (var i = 0; i < 7; i++)
                reader.ReadGuardedInt32();
            reader.ReadUInt8();
            reader.ReadUInt8();
            var rankType = reader.ReadUInt8();
            var scoreboardIndex = reader.ReadUInt8();

            return new LtPlayerInDecoded(
                characterIndex,
                teamIndex,
                objectId,
                userName,
                repCount,
                rankType,
                scoreboardIndex);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static bool IsPlausiblePlayerName(string userName)
    {
        if (userName.Length == 0)
            return false;

        foreach (var ch in userName)
        {
            if (ch is >= ' ' and <= '~')
                continue;
            if (ch > '\u007F')
                continue;
            return false;
        }

        return true;
    }

    public static LtDecodedMessage? TryDecodeDamage(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScDamage)
                return null;

            return new LtDamageDecoded(
                reader.ReadInt8(),
                reader.ReadVector3(),
                reader.ReadUInt16(),
                reader.ReadUInt8(),
                reader.ReadInt16(),
                reader.ReadBoolean(),
                reader.ReadUInt8());
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public const int SetWeaponSlotWeaponCount = 11;
    public const int SetWeaponSlotCustomSetPerBag = 5;
    public const int HitInfoBlobBytes = 2880;

    public static LtDecodedMessage? TryDecodeSetWeaponSlot(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScSetWeaponSlot)
                return null;

            var selectedSlot = reader.ReadUInt8();
            var weaponTypes = new short[SetWeaponSlotWeaponCount];
            var zeroStates = new bool[SetWeaponSlotWeaponCount];
            for (var i = 0; i < SetWeaponSlotWeaponCount; i++)
            {
                weaponTypes[i] = reader.ReadInt16();
                zeroStates[i] = reader.ReadBoolean();
            }

            var changeBagNum = reader.ReadBoolean();
            var bagNum = reader.ReadUInt8();
            var rapidChangeBuff = reader.ReadGuardedSingle();
            var changeWeaponState = reader.ReadGuardedInt32();
            var motion = reader.ReadUInt8();
            var customSetBits = SetWeaponSlotWeaponCount * SetWeaponSlotCustomSetPerBag * 32;
            var customSets = new List<IReadOnlyList<LtVvipWeaponCustomColor>>(SetWeaponSlotWeaponCount);
            if (reader.RemainingBits >= customSetBits)
            {
                for (var bag = 0; bag < SetWeaponSlotWeaponCount; bag++)
                {
                    var slots = new LtVvipWeaponCustomColor[SetWeaponSlotCustomSetPerBag];
                    for (var slot = 0; slot < SetWeaponSlotCustomSetPerBag; slot++)
                    {
                        var red = reader.ReadUInt8();
                        var blue = reader.ReadUInt8();
                        var green = reader.ReadUInt8();
                        var material = reader.ReadUInt8();
                        slots[slot] = new LtVvipWeaponCustomColor(red, green, blue, material);
                    }

                    customSets.Add(slots);
                }
            }
            else
            {
                for (var bag = 0; bag < SetWeaponSlotWeaponCount; bag++)
                    customSets.Add(Array.Empty<LtVvipWeaponCustomColor>());
            }

            var hasExtended = reader.HasRemaining;

            return new LtSetWeaponSlotDecoded(
                selectedSlot,
                weaponTypes,
                zeroStates,
                changeBagNum,
                bagNum,
                rapidChangeBuff,
                changeWeaponState,
                motion,
                customSets,
                hasExtended);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static LtDecodedMessage? TryDecodeHitInfo(ReadOnlySpan<byte> payload)
    {
        try
        {
            var reader = new LtBitstreamReader(payload);
            if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgScHitInfo)
                return null;

            reader.SkipBytes(HitInfoBlobBytes);
            return new LtHitInfoDecoded(
                HitInfoBlobBytes,
                reader.ReadGuardedSingle(),
                reader.ReadGuardedSingle(),
                reader.ReadGuardedInt32());
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
