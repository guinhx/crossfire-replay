using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Formats.SimpleProtocol;
using CrossFire.Replay.Protocol;
using CrossFire.Replay.Protocol.Lt;
using CrossFire.Replay.Compression;
using CrossFire.Replay.Tests.Support;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class MiddleBlobGapReaderTests
{
    [Fact]
    public void ParseContainers_FindsLtPayloadInSizedWrapper()
    {
        var bombSites = new byte[] { 0x00, 0x00, 0x01, 0x02, 0x03 };
        var region = new byte[8 + bombSites.Length];
        BitConverter.TryWriteBytes(region.AsSpan(0), (uint)bombSites.Length);
        bombSites.CopyTo(region, 8);

        var containers = MiddleBlobGapReader.ParseContainers(region);
        Assert.Single(containers);
        Assert.Equal(EMessageId.MsgScBombSites, containers[0].MessageId);
    }

    [Fact]
    public void UserCfn_MiddleBlob_HasCoverageMetrics()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var ps = (PacketSimulatorReplayDocument)ReplayService.Default.Read(path);
        Assert.NotEmpty(ps.MiddleBlob);
        Assert.True(ps.MiddleBlobCoverage.TotalBytes > 0);
        Assert.True(ps.MiddleBlobCoverage.SegmentBytes > 0);
        Assert.InRange(ps.MiddleBlobCoverage.ParsedRatio, 0.0, 1.0);
    }
}

public sealed class LegacyTimelineParityTests
{
    [Fact]
    public void PacketSimulatorLegacy_RoundTrip_BuildsUnifiedTimeline()
    {
        var roundStartPayload = LtMessageWriter.Encode(new LtRoundStartDecoded(0));
        var original = new PacketSimulatorReplayDocument
        {
            InnerFormat = PacketSimulatorInnerFormat.LegacyV2022,
            ContainerVersion = PacketSimulatorLayout.ContainerVersion,
            ContainerMagic = PacketSimulatorLayout.ContainerMagic,
            Header = new PacketSimulatorHeaderBlock
            {
                DeathMatchTypeRaw = 1,
                GameGoal = 100,
                MapId = 7,
                RawBlock = new byte[PacketSimulatorLayout.HeaderBlockSize],
            },
            SpectatingRoomInfo = [0x01, 0x00, 0x08, 0x00],
            SpectatingRoomInfoDeclaredSize = 4,
            SpectatingPackets = new PacketSection<TimestampPacketRecord>
            {
                Packets =
                [
                    new TimestampPacketRecord { Timestamp = 100, Payload = roundStartPayload },
                ],
            },
            GamePackets = new PacketSection<TimestampPacketRecord>
            {
                Packets =
                [
                    new TimestampPacketRecord { Timestamp = 200, Payload = roundStartPayload },
                ],
            },
        };

        var written = PacketSimulatorPayloadWriter.BuildInnerPayload(original);
        var roundTrip = Assert.IsType<PacketSimulatorReplayDocument>(
            new PacketSimulatorPayloadReader().Read(written));

        Assert.Equal(2, roundTrip.UnifiedTimeline.Count);
        Assert.Equal(2, roundTrip.DeduplicatedTimeline.Count);
        Assert.All(roundTrip.UnifiedTimeline, p => Assert.NotNull(p.Source));
        Assert.Contains(roundTrip.UnifiedTimeline, p => p.Decoded is LtRoundStartDecoded);
    }
}

public sealed class CfrChecksumReaderTests
{
    [Fact]
    public void ChecksumPrefixedCfr_ValidatesOnRead()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("cfrversion0032"));
        writer.Write(0);
        writer.Write((byte)SimpleProtocolId.RoundStart);
        writer.Write((byte)1);
        writer.Write(1500u);
        writer.Write(0);

        var body = stream.ToArray();
        var block = ReplayChecksum.ComputeBlock(body);
        var file = new byte[block.Length + body.Length];
        block.CopyTo(file, 0);
        body.CopyTo(file, block.Length);

        var read = SimpleProtocolReader.ReadPayload(file);
        Assert.True(read.HasChecksum);
        Assert.True(read.ChecksumValid);
        Assert.Single(read.Messages);
    }
}

public sealed class LtTickDecoderTests
{
    [Fact]
    public void OneSecondPassed_DecodesEmptyBody()
    {
        var payload = new byte[] { 0x71, 0x00 };
        var decoded = Assert.IsType<LtCs1SecondPassedDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(EMessageId.MsgCs1SecondPassed, decoded.MessageId);
    }

    [Fact]
    public void HiddenTeamIndex_DecodesTeamByte()
    {
        var payload = new byte[] { 0x7B, 0x00, 0x02 };
        var decoded = Assert.IsType<LtHiddenTeamIndexDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(2, decoded.HiddenTeamIndex);
    }
}

public sealed class LtCombatDecoderTests
{
    [Fact]
    public void Damage_RoundTripsThroughWriter()
    {
        var original = new LtDamageDecoded(
            3,
            new Vector3F(1f, 2f, 3f),
            42,
            1,
            100,
            false,
            7);
        var payload = LtMessageWriter.Encode(original);
        var decoded = Assert.IsType<LtDamageDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(original.AttackerIndex, decoded.AttackerIndex);
        Assert.Equal(original.Damage, decoded.Damage);
        Assert.Equal(original.WeaponType, decoded.WeaponType);
    }

    [Fact]
    public void HitInfo_DecodesSummaryTail()
    {
        var original = new LtHitInfoDecoded(
            LtSemanticDecoders.HitInfoBlobBytes,
            0.75f,
            0.25f,
            9001);
        var payload = LtMessageWriter.Encode(original);
        var decoded = Assert.IsType<LtHitInfoDecoded>(LtMessageReader.TryDecode(payload));
        Assert.Equal(original.HitRates, decoded.HitRates);
        Assert.Equal(original.HeadKillRates, decoded.HeadKillRates);
        Assert.Equal(original.SummingDamages, decoded.SummingDamages);
    }
}

public sealed class ModernExtraBlockTests
{
    [Fact]
    public void TryRead_ParsesFourByteBlock()
    {
        var parsed = ModernExtraBlockReader.TryRead([0x01, 0x00, 0x00, 0x00]);
        Assert.NotNull(parsed);
        Assert.Equal(1u, parsed!.Value);
    }

    [Fact]
    public void UserCfn_HasParsedExtraBlock()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var ps = (PacketSimulatorReplayDocument)ReplayService.Default.Read(path);
        Assert.Equal(4, ps.ExtraBlock.Length);
        Assert.NotNull(ps.ParsedExtraBlock);
    }
}

public sealed class ExpandedTimelineTests
{
    [Fact]
    public void ExpandedTimeline_IncludesEmbeddedSnapshotPackets()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var ps = (PacketSimulatorReplayDocument)ReplayService.Default.Read(path);
        if (ps.BinarySnapshotEmbeddedPackets.Count == 0)
            return;

        Assert.True(ps.ExpandedUnifiedTimeline.Count >= ps.UnifiedTimeline.Count);
        Assert.Contains(
            ps.ExpandedUnifiedTimeline,
            p => p.Source == PacketTimelineSource.BinarySnapshotBody);
    }
}

public sealed class SimpleProtocolMarkerTests
{
    [Fact]
    public void TeamChange_ReadsAsTypedMessage()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("cfrversion0032"));
        writer.Write(0);
        writer.Write((byte)SimpleProtocolId.TeamChange);
        writer.Write((byte)1);
        writer.Write(500u);

        var read = SimpleProtocolReader.ReadPayload(stream.ToArray());
        Assert.IsType<TeamChangeMessage>(read.Messages[0]);
    }
}

public sealed class LtCsDecoderIntegrationTests
{
    [Fact]
    public void UserReplay_FrequentCsMessages_DecodeSemantically()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var ps = (PacketSimulatorReplayDocument)ReplayService.Default.Read(path);
        var targets = new Dictionary<EMessageId, int>
        {
            [EMessageId.MsgCsReqDropWeapon] = 0,
            [EMessageId.MsgScAckSublinkChangeWeapon] = 0,
            [EMessageId.MsgCsVelAndRot] = 0,
            [EMessageId.MsgCsFrogJump] = 0,
            [EMessageId.MsgScSetWeaponSlot] = 0,
        };

        foreach (var packet in ps.ExpandedUnifiedTimeline)
        {
            if (!LtMessageReader.TryPeekMessageId(packet.Payload, out var id))
                continue;
            if (!targets.ContainsKey(id))
                continue;

            var decoded = LtMessageReader.TryDecode(packet.Payload);
            if (decoded is null or LtUnknownDecoded)
                continue;

            targets[id]++;
        }

        Assert.True(targets[EMessageId.MsgCsReqDropWeapon] > 0);
        Assert.True(targets[EMessageId.MsgScAckSublinkChangeWeapon] > 0);
        Assert.True(targets[EMessageId.MsgScSetWeaponSlot] > 0);
    }

    [Fact]
    public void SetWeaponSlot_DecodesCustomColorSets()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var ps = (PacketSimulatorReplayDocument)ReplayService.Default.Read(path);
        var slot = ps.ExpandedUnifiedTimeline
            .Select(p => LtMessageReader.TryDecode(p.Payload))
            .OfType<LtSetWeaponSlotDecoded>()
            .FirstOrDefault();
        if (slot is null)
            return;

        Assert.Equal(LtSemanticDecoders.SetWeaponSlotWeaponCount, slot.CustomSetInfo.Count);
        Assert.Equal(LtSemanticDecoders.SetWeaponSlotCustomSetPerBag, slot.CustomSetInfo[0].Count);
    }
}
