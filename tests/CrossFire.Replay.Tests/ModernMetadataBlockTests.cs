using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Mm;
using Xunit;
using Xunit.Abstractions;

namespace CrossFire.Replay.Tests;

public sealed class ModernMetadataBlockTests
{
    private const string UserCfn = @"D:\Dinho\Documents\Cross Fire\Replay\CFReplay20260701_0000.cfn";
    private readonly ITestOutputHelper _output;

    public ModernMetadataBlockTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void UserCfn_ModernMetadata_HasHeaderAndTail()
    {
        if (!File.Exists(UserCfn))
            return;

        var ps = (PacketSimulatorReplayDocument)ReplayService.Default.Read(UserCfn);
        Assert.Equal(PacketSimulatorLayout.ModernMetadataBlockSize, ps.MetadataBlock.Length);

        Assert.NotNull(ps.ModernMetadata);
        var parsed = ps.ModernMetadata!;
        Assert.NotNull(parsed.Header);
        Assert.Equal(2026, parsed.Header.MapId);
        Assert.Equal(512, parsed.TailBytes.Length);

        var nonZeroTail = parsed.TailBytes.Count(b => b != 0);
        _output.WriteLine($"tailNonZero={nonZeroTail} prefixOffset={parsed.RoomInfoPrefixOffset}");
        if (parsed.RoomInfoPrefix is { } room)
            _output.WriteLine($"room max={room.RoomMaxUser} mapType={room.MapType} goal={room.WinGoal}");
    }

    [Fact]
    public void TryRead_RoundTripsHeaderFields()
    {
        var block = new byte[PacketSimulatorLayout.ModernMetadataBlockSize];
        block[8] = 0xEA;
        block[9] = 0x07;
        block[4] = 0x01;

        Assert.NotNull(ModernMetadataBlockReader.TryRead(block));
        var parsed = ModernMetadataBlockReader.TryRead(block)!;
        Assert.Equal(2026, parsed.Header.MapId);
        Assert.Equal(1u, parsed.Header.GameGoal);
    }
}
