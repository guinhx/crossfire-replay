using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Mm;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class LegacyTimingAndMetadataTests
{
    [Theory]
    [InlineData(100u, 500u, 80u, 80u)]
    [InlineData(100u, 500u, 100u, 100u)]
    [InlineData(100u, 500u, 250u, 101u)]
    [InlineData(100u, 500u, 800u, 401u)]
    [InlineData(100u, 500u, 1500u, 1101u)]
    public void TimestampRemapper_Write_MatchesNative(
        uint initEnd,
        uint firstGame,
        uint logical,
        uint expectedWire)
    {
        var state = new PacketSimulatorTimingState
        {
            InitEndPacketTime = initEnd,
            FirstGamePacketTime = firstGame,
            UseWireTimestamps = false,
        };

        Assert.Equal(expectedWire, PacketSimulatorTimestampRemapper.RemapForWrite(logical, state));
    }

    [Fact]
    public void TimestampRemapper_WriteAndInverse_RoundTripsAfterFirstGame()
    {
        var state = new PacketSimulatorTimingState
        {
            InitEndPacketTime = 100,
            FirstGamePacketTime = 500,
            UseWireTimestamps = false,
        };

        const uint logical = 1500;
        var wire = PacketSimulatorTimestampRemapper.RemapForWrite(logical, state);
        Assert.Equal(1101u, wire);
        Assert.Equal(logical, PacketSimulatorTimestampRemapper.RemapFromWire(wire, state));
    }

    [Fact]
    public void TimestampRemapper_WireMode_DoesNotRemap()
    {
        var state = new PacketSimulatorTimingState
        {
            InitEndPacketTime = 100,
            FirstGamePacketTime = 500,
            UseWireTimestamps = true,
        };

        Assert.Equal(999u, PacketSimulatorTimestampRemapper.RemapForWrite(999, state));
    }

    [Fact]
    public void LegacyPacketSimulator_LogicalTimestamps_WriteRemappedTimestamps()
    {
        var doc = new PacketSimulatorReplayDocument
        {
            InnerFormat = PacketSimulatorInnerFormat.LegacyV2022,
            ContainerVersion = PacketSimulatorLayout.ContainerVersion,
            ContainerMagic = PacketSimulatorLayout.ContainerMagic,
            Header = new PacketSimulatorHeaderBlock { MapId = 1, RawBlock = new byte[PacketSimulatorLayout.HeaderBlockSize] },
            SpectatingPackets = new PacketSection<TimestampPacketRecord>
            {
                Packets =
                [
                    new TimestampPacketRecord { Timestamp = 90, Payload = [0x01] },
                    new TimestampPacketRecord { Timestamp = 500, Payload = [0x02] },
                ],
            },
            GamePackets = new PacketSection<TimestampPacketRecord>
            {
                Packets =
                [
                    new TimestampPacketRecord { Timestamp = 1500, Payload = [0x03] },
                ],
            },
            TimingState = new PacketSimulatorTimingState
            {
                InitEndPacketTime = 100,
                FirstGamePacketTime = 500,
                UseWireTimestamps = false,
            },
        };

        var written = PacketSimulatorPayloadWriter.BuildInnerPayload(doc);
        var reader = new PacketSimulatorPayloadReader();
        var parsed = Assert.IsType<PacketSimulatorReplayDocument>(reader.Read(written));

        Assert.Equal(90u, parsed.SpectatingPackets.Packets[0].Timestamp);
        Assert.Equal(101u, parsed.SpectatingPackets.Packets[1].Timestamp);
        Assert.Equal(1101u, parsed.GamePackets.Packets[0].Timestamp);
    }

    [Fact]
    public void ProtocolMmRoomInfoWriter_PrefixRoundTrips()
    {
        var original = new ProtocolMmRoomInfoHeader
        {
            RoomNumber = 42,
            RoomMaxUser = 8,
            RoomObserverMaxUser = 2,
            MapType = 99,
            IsFreeCamera = true,
            GameRule = Protocol.Game.RoundType.TeamDeathMatch,
            WeaponType = 3,
            ThrowWeapon = true,
            WinGoal = 100,
            TimeToRespawn = 3.5f,
            InitialTp = 1000,
            FriendlyFire = false,
        };

        var bytes = ProtocolMmRoomInfoWriter.WritePrefix(original);
        var parsed = ProtocolMmRoomInfoReader.TryReadPrefix(bytes);
        Assert.NotNull(parsed);
        Assert.Equal(original.RoomNumber, parsed!.RoomNumber);
        Assert.Equal(original.MapType, parsed.MapType);
        Assert.Equal(original.WinGoal, parsed.WinGoal);
        Assert.Equal(original.TimeToRespawn, parsed.TimeToRespawn);
    }

    public static IEnumerable<object[]> UserCfnFiles() => ModernSynthesisTests.UserCfnFiles();

    [Theory]
    [MemberData(nameof(UserCfnFiles))]
    public void UserCfn_MetadataBlock_PreservesWhenRoomInfoAndHeaderProvided(string path)
    {
        var original = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));

        var rebuilt = ProtocolMmRoomInfoWriter.WriteMetadataBlock(
            original.MetadataBlock,
            original.RoomInfoHeader,
            original.Header);

        Assert.Equal(original.MetadataBlock, rebuilt);
    }

    [Theory]
    [MemberData(nameof(UserCfnFiles))]
    public void UserCfn_MetadataPrefix_ReadWriteRoundTrips(string path)
    {
        var original = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        if (original.RoomInfoHeader is not { } roomInfo)
            return;

        var bytes = ProtocolMmRoomInfoWriter.WritePrefix(roomInfo);
        var parsed = ProtocolMmRoomInfoReader.TryReadPrefix(bytes);
        Assert.NotNull(parsed);
        Assert.Equal(roomInfo.RoomNumber, parsed!.RoomNumber);
        Assert.Equal(roomInfo.MapType, parsed.MapType);
    }
}
