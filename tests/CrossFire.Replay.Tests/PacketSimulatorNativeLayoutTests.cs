using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Lt;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class PacketSimulatorNativeLayoutTests
{
    private const string UserCfn = @"D:\Dinho\Documents\Cross Fire\Replay\CFReplay20260701_0000.cfn";

    [Fact]
    public void NativeLayout_LegacyConstants_MatchObservedLayout()
    {
        Assert.Equal(6u, PacketSimulatorNativeLayout.LegacyContainerVersion);
        Assert.Equal(1020107u, PacketSimulatorNativeLayout.LegacyInnerMagic);
        Assert.Equal(47963, PacketSimulatorNativeLayout.LegacySpectatingRoomInfoDeclaredSize);
    }

    [Fact]
    public void UserCfn_ModernRoomInfo_ComesFromDescriptorAndMiddleHeader()
    {
        if (!File.Exists(UserCfn))
            return;

        var ps = (PacketSimulatorReplayDocument)ReplayService.Default.Read(UserCfn);
        Assert.Equal(PacketSimulatorInnerFormat.ModernV2026, ps.InnerFormat);
        Assert.Empty(ps.SpectatingRoomInfo);
        Assert.Null(ps.SpectatingRoomInfoDeclaredSize);

        Assert.NotNull(ps.ModernMetadata);
        var meta = ps.ModernMetadata!;
        Assert.Null(meta.RoomInfoPrefix);
        Assert.Contains(ModernReplayRoomInfoSource.MetadataHeaderBlock, meta.RoomInfoSources);
        Assert.Contains(ModernReplayRoomInfoSource.ModernDescriptor, meta.RoomInfoSources);
        Assert.Contains(ModernReplayRoomInfoSource.MiddleBlobHeader, meta.RoomInfoSources);
        Assert.DoesNotContain(ModernReplayRoomInfoSource.MetadataTailPrefix, meta.RoomInfoSources);

        Assert.Equal("BRASILEIRAO", ps.ModernDescriptor!.MapLabel);
        Assert.StartsWith("Allan", ps.ModernDescriptor.HostLabel);
        Assert.Equal("BRASILEIRAO", ps.MiddleBlobHeader!.MapLabel);
    }
}

public sealed class EMessageIdCatalogExtendedTests
{
    [Theory]
    [InlineData(0x03B2, "MSG_CS_AIBOT_PATHFINDERSAVE")]
    [InlineData(0x037A, "MSG_SC_AI2MODE_DEFENCETOWER_FIRE")]
    [InlineData(0x03DE, "MSG_SC_SHEEP_DELETE")]
    [InlineData(0x03FC, "MSG_SC_KINGS_RANDOM_MISSION_CHOICE")]
    [InlineData(0x0260, "MSG_SC_SPECIALWEAPON_LASERSIGHT")]
    [InlineData(0x012D, "MSG_SC_HUMANBOSS_TRANSFORM_INFO")]
    public void GeneratedCatalog_CoversFrequentReplayIds(ushort id, string expected)
    {
        Assert.True(EMessageIdCatalog.IsKnown(id));
        Assert.Equal(expected, EMessageIdCatalog.GetName(id));
    }

    [Fact]
    public void MaxGameplayId_MatchesNativeEnum()
    {
        Assert.Equal(0x082C, EMessageIdCatalog.MaxGameplayId);
        Assert.True(EMessageIdCatalog.IsPlausible(0x082C));
        Assert.False(EMessageIdCatalog.IsPlausible(0x082D));
    }
}
