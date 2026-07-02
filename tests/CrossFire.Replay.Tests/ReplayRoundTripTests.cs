using CrossFire.Replay;
using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Compression;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Formats.SimpleProtocol;
using CrossFire.Replay.Protocol;
using CrossFire.Replay.Protocol.Messages;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class ReplayRoundTripTests
{
    [Fact]
    public void PlainCfr_RoundTrip_PreservesMessages()
    {
        var doc = CreateSampleCfrDocument();
        var bytes = SimpleProtocolWriter.WritePayload(doc);
        var roundTrip = SimpleProtocolReader.ReadPayload(bytes);

        Assert.Equal(2, roundTrip.Messages.Count);
        Assert.Equal(SimpleProtocolId.MapInfo, roundTrip.Messages[0].MessageId);
        Assert.Equal(SimpleProtocolId.RoundStart, roundTrip.Messages[1].MessageId);
    }

    [Fact]
    public void ChecksumBlock_MatchesNativeMixing()
    {
        var body = SimpleProtocolWriter.WritePayload(CreateSampleCfrDocument());
        var block = ReplayChecksum.ComputeBlock(body);

        Assert.Equal(32, block.Length);
        Assert.True(ReplayChecksum.TryValidateBlock(body, block));
    }

    [Fact]
    public void ContainerRoundTrip_CfoAndCfn_PreservesCfrPayload()
    {
        var doc = CreateSampleCfrDocument();
        var writer = ReplayWriteService.Default;
        var decoder = new ReplayContainerDecoder();

        foreach (var options in new[] { ReplayWriteOptions.CfoWrapper, ReplayWriteOptions.CfnWrapper })
        {
            var wrapped = writer.Write(doc, options);
            Assert.True(decoder.CanDecode(wrapped));

            var decoded = decoder.Decode(wrapped);
            var roundTrip = SimpleProtocolReader.ReadPayload(decoded.Payload);
            Assert.Equal(2, roundTrip.Messages.Count);
        }
    }

    [Fact]
    public void PacketSimulatorLegacy_RoundTrip_PreservesStructure()
    {
        var original = CreateSamplePacketSimulatorDocument();
        var written = PacketSimulatorPayloadWriter.BuildInnerPayload(original);
        var reader = new PacketSimulatorPayloadReader();
        var roundTrip = Assert.IsType<PacketSimulatorReplayDocument>(reader.Read(written));

        Assert.Equal(PacketSimulatorInnerFormat.LegacyV2022, roundTrip.InnerFormat);
        Assert.Equal(original.Header!.MapId, roundTrip.Header!.MapId);
        Assert.Equal(original.SpectatingPackets.Packets.Count, roundTrip.SpectatingPackets.Packets.Count);
        Assert.Equal(original.GamePackets.Packets.Count, roundTrip.GamePackets.Packets.Count);
        Assert.Equal(original.SpecialEffectPackets.Packets.Count, roundTrip.SpecialEffectPackets.Packets.Count);
        Assert.Equal(
            original.GamePackets.Packets[0].Payload,
            roundTrip.GamePackets.Packets[0].Payload);
    }

    [Fact]
    public void TypedMessages_PlayerOutAndTriggers_RoundTrip()
    {
        var triggerData = Enumerable.Range(0, 0x80).Select(i => (byte)i).ToArray();
        var messages = new ReplayMessage[]
        {
            new PlayerOutMessage
            {
                MessageId = SimpleProtocolId.PlayerOut,
                ProtocolVersion = 1,
                Timestamp = 2000,
                CharacterIndex = 3,
                ExitReason = 2,
                FirstCode = 0x1234,
                SecondCode = 0x5678,
            },
            new DelayTriggerMessage
            {
                MessageId = SimpleProtocolId.DelayTriggerPlayEffect,
                ProtocolVersion = 0,
                Timestamp = 2100,
                TriggerId = 42,
            },
            new TriggerOnOffReplayMessage
            {
                MessageId = SimpleProtocolId.TriggerOnOffReplay,
                ProtocolVersion = 0,
                Timestamp = 2200,
                Data = triggerData,
            },
        };

        var doc = new CfrDocument
        {
            FileVersion = SimpleProtocolReader.CurrentFileVersion,
            SpecDefines = ReplayFeatureFlags.DefaultWriteFlags.ToList(),
            Messages = messages,
        };

        var bytes = SimpleProtocolWriter.WritePayload(doc);
        var roundTrip = SimpleProtocolReader.ReadPayload(bytes);

        var playerOut = Assert.IsType<PlayerOutMessage>(roundTrip.Messages[0]);
        Assert.Equal(3, playerOut.CharacterIndex);
        Assert.Equal((ushort)0x1234, playerOut.FirstCode);

        var trigger = Assert.IsType<DelayTriggerMessage>(roundTrip.Messages[1]);
        Assert.Equal(42, trigger.TriggerId);

        var onOff = Assert.IsType<TriggerOnOffReplayMessage>(roundTrip.Messages[2]);
        Assert.Equal(triggerData, onOff.Data);
    }

    private static CfrDocument CreateSampleCfrDocument()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);

        writer.Write(System.Text.Encoding.ASCII.GetBytes("cfrversion0032"));
        writer.Write(ReplayFeatureFlags.DefaultWriteFlags.Count);
        foreach (var flag in ReplayFeatureFlags.DefaultWriteFlags)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(flag);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        writer.Write((byte)SimpleProtocolId.MapInfo);
        writer.Write((byte)3);
        writer.Write(1000u);
        writer.Write((short)42);
        writer.Write(1);
        writer.Write(2);
        writer.Write(100);
        writer.Write((sbyte)-1);
        writer.Write(false);
        WriteNullTerminatedName(writer, "TeamBL");
        WriteNullTerminatedName(writer, "TeamGR");
        writer.Write(false);
        writer.Write(false);

        writer.Write((byte)SimpleProtocolId.RoundStart);
        writer.Write((byte)1);
        writer.Write(1500u);
        writer.Write(0);

        return SimpleProtocolReader.ReadPayload(stream.ToArray());
    }

    private static PacketSimulatorReplayDocument CreateSamplePacketSimulatorDocument()
    {
        var headerBlock = new byte[PacketSimulatorLayout.HeaderBlockSize];
        headerBlock[0] = 1;
        headerBlock[8] = 7;
        headerBlock[10] = 1;
        System.Text.Encoding.ASCII.GetBytes("ClanGR").CopyTo(headerBlock, 11);
        System.Text.Encoding.ASCII.GetBytes("ClanBL").CopyTo(headerBlock, 47);

        return new PacketSimulatorReplayDocument
        {
            InnerFormat = PacketSimulatorInnerFormat.LegacyV2022,
            ContainerVersion = PacketSimulatorLayout.ContainerVersion,
            ContainerMagic = PacketSimulatorLayout.ContainerMagic,
            Header = new PacketSimulatorHeaderBlock
            {
                DeathMatchTypeRaw = 1,
                GameGoal = 100,
                MapId = 7,
                IsClanGame = true,
                ClanNameGr = "ClanGR",
                ClanNameBl = "ClanBL",
                RawBlock = headerBlock,
            },
            SpectatingRoomInfo = [0x01, 0x02, 0x03],
            SpectatingRoomInfoDeclaredSize = 3,
            SpectatingPackets = new PacketSection<TimestampPacketRecord>
            {
                Packets =
                [
                    new TimestampPacketRecord
                    {
                        Timestamp = 100,
                        Payload = [0xAA, 0xBB],
                    },
                ],
            },
            GamePackets = new PacketSection<TimestampPacketRecord>
            {
                Packets =
                [
                    new TimestampPacketRecord
                    {
                        Timestamp = 200,
                        Payload = [0xCC],
                    },
                ],
            },
            SpecialEffectPackets = new PacketSection<SpecialEffectPacketRecord>
            {
                Packets =
                [
                    new SpecialEffectPacketRecord
                    {
                        Timestamp = 300,
                        ObjectId = 9,
                        Payload = [0xDD],
                    },
                ],
            },
        };
    }

    private static void WriteNullTerminatedName(BinaryWriter writer, string name)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(name);
        writer.Write(bytes.Length + 1);
        writer.Write(bytes);
        writer.Write((byte)0);
    }
}
