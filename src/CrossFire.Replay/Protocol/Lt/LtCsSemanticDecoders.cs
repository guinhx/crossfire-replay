using CrossFire.Replay.Protocol;

namespace CrossFire.Replay.Protocol.Lt;

internal static class LtCsSemanticDecoders
{
  /// <summary>Fallback world bounds when map-specific min/max are unavailable.</summary>
  private static readonly Vector3F DefaultWorldMin = new(-4096f, -4096f, -4096f);
  private static readonly Vector3F DefaultWorldMax = new(4096f, 4096f, 4096f);

  public static LtDecodedMessage? TryDecodeVelAndRot(ReadOnlySpan<byte> payload)
  {
    try
    {
      var reader = new LtBitstreamReader(payload);
      if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgCsVelAndRot)
        return null;

      var header = ReadHeaderFields(reader);
      var includeVelocity = reader.ReadBoolean();
      Vector3F? velocity = includeVelocity ? LtCompressedVectorCodec.ReadCompLtVector(reader) : null;
      var compPos = LtCompressedVectorCodec.ReadCompWorldPos(reader);
      var decodedPos = LtCompressedVectorCodec.DecodeCompWorldPos(compPos, DefaultWorldMin, DefaultWorldMax);
      var teleport = reader.ReadBoolean();
      var stand = reader.ReadBoolean();
      var elapsed = (ushort)reader.ReadBits(10);
      var duck = reader.ReadBoolean();
      LtCsPacketReader.SkipPacketHash(reader);
      var repurposeVel = LtCompressedVectorCodec.ReadCompLtVector(reader);
      var jump = reader.ReadBoolean();
      var frameTime = reader.ReadGuardedSingle();
      var standOnCheck = reader.ReadBoolean();
      var respawnedFlag = reader.ReadGuardedInt32();

      return new LtCsVelAndRotDecoded(
        header,
        includeVelocity,
        velocity,
        compPos,
        decodedPos,
        teleport,
        stand,
        elapsed,
        duck,
        repurposeVel,
        jump,
        frameTime,
        standOnCheck,
        respawnedFlag);
    }
    catch (InvalidOperationException)
    {
      return null;
    }
  }

  public static LtDecodedMessage? TryDecodeReqDropWeapon(ReadOnlySpan<byte> payload)
  {
    if (payload.Length < 2)
      return null;

    try
    {
      var reader = new LtBitstreamReader(payload);
      if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgCsReqDropWeapon)
        return null;

      if (!reader.HasRemaining)
        return new LtCsReqDropWeaponDecoded(new LtCsPacketHeaderFields(0, 0), 0, 0, 0, true);

      if (payload.Length <= 6)
      {
        if (reader.RemainingBits >= 32)
          _ = reader.ReadUInt32();

        return new LtCsReqDropWeaponDecoded(new LtCsPacketHeaderFields(0, 0), 0, 0, 0, true);
      }

      if (payload.Length > 64)
        return null;

      var header = ReadHeaderFields(reader);
      var slot = reader.ReadUInt8();
      var mag = reader.ReadUInt16();
      var total = reader.ReadUInt16();
      LtCsPacketReader.SkipPacketHash(reader);

      return new LtCsReqDropWeaponDecoded(header, slot, mag, total);
    }
    catch (InvalidOperationException)
    {
      return null;
    }
  }

  public static LtDecodedMessage? TryDecodeChangeWeapon(ReadOnlySpan<byte> payload, EMessageId wireId)
  {
    if (wireId is not (
      EMessageId.MsgCsReqChangeWeapon
      or EMessageId.MsgScAckForceChangeWeapon
      or EMessageId.MsgScAckSublinkChangeWeapon))
      return null;

    try
    {
      var reader = new LtBitstreamReader(payload);
      if ((EMessageId)reader.ReadMessageId() != wireId)
        return null;

      var header = ReadHeaderFields(reader);
      var slot = reader.ReadUInt8();
      var mag = reader.ReadUInt16();
      var total = reader.ReadUInt16();
      var curSlot = reader.ReadUInt8();
      var changeTime = reader.ReadGuardedUInt32();
      var selectSlotEx = reader.ReadBoolean();
      var lastWeaponIndex = reader.ReadGuardedInt32();
      var commandOld = reader.ReadBoolean();
      LtCsPacketReader.SkipPacketHash(reader);

      return new LtCsChangeWeaponDecoded(
        wireId,
        header,
        slot,
        mag,
        total,
        curSlot,
        changeTime,
        selectSlotEx,
        lastWeaponIndex,
        commandOld);
    }
    catch (InvalidOperationException)
    {
      return null;
    }
  }

  public static LtDecodedMessage? TryDecodeLinkWeapon(ReadOnlySpan<byte> payload, EMessageId wireId)
  {
    if (wireId is not (EMessageId.MsgCsReqLinkWeapon or EMessageId.MsgScAckLinkWeapon))
      return null;

    try
    {
      var reader = new LtBitstreamReader(payload);
      if ((EMessageId)reader.ReadMessageId() != wireId)
        return null;

      var header = ReadHeaderFields(reader);
      var curSlot = reader.ReadUInt8();
      var weaponSelect = reader.ReadGuardedInt32();
      var motion = reader.ReadUInt8();
      var nextExpand = reader.ReadUInt8();
      LtCsPacketReader.SkipPacketHash(reader);

      return new LtCsLinkWeaponDecoded(
        wireId,
        header,
        curSlot,
        weaponSelect,
        motion,
        nextExpand);
    }
    catch (InvalidOperationException)
    {
      return null;
    }
  }

  public static LtDecodedMessage? TryDecodeFrogJump(ReadOnlySpan<byte> payload)
  {
    try
    {
      var reader = new LtBitstreamReader(payload);
      if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgCsFrogJump)
        return null;

      var header = ReadHeaderFields(reader);
      LtCsPacketReader.SkipPacketHash(reader);
      return new LtCsFrogJumpDecoded(header);
    }
    catch (InvalidOperationException)
    {
      return null;
    }
  }

  public static LtDecodedMessage? TryDecodeLandingState(ReadOnlySpan<byte> payload)
  {
    try
    {
      var reader = new LtBitstreamReader(payload);
      if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgCsLandingState)
        return null;

      var header = ReadHeaderFields(reader);
      var velY = reader.ReadGuardedSingle();
      var landType = reader.ReadGuardedInt32();
      LtCsPacketReader.SkipPacketHash(reader);

      return new LtCsLandingStateDecoded(header, velY, landType);
    }
    catch (InvalidOperationException)
    {
      return null;
    }
  }

  private static LtCsPacketHeaderFields ReadHeaderFields(LtBitstreamReader reader)
  {
    var header = LtCsPacketReader.ReadHeader(reader);
    return new LtCsPacketHeaderFields(header.DummyData, header.PacketSeqIndex);
  }

  public static LtDecodedMessage? TryDecodeFirstUpdate(ReadOnlySpan<byte> payload)
  {
    if (payload.Length < 2)
      return null;

    try
    {
      var reader = new LtBitstreamReader(payload);
      if ((EMessageId)reader.ReadMessageId() != EMessageId.MsgCsFirstUpdate)
        return null;

      return new LtCsFirstUpdateDecoded();
    }
    catch (InvalidOperationException)
    {
      return null;
    }
  }

  public static LtDecodedMessage? TryDecodeRappelVelAndRot(ReadOnlySpan<byte> payload) =>
      LtNativeSerializers.TryDecodeRappelVelAndRot(payload);
}
