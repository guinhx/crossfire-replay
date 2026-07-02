using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Lt;
using CrossFire.Replay.Protocol.Mm;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class EMessageIdCatalogTests
{
    [Theory]
    [InlineData(0x00, "MSG_SC_BOMBSITES")]
    [InlineData(0x5F, "MSG_CS_AI_LANDINGSTATE")]
    [InlineData(0x71, "MSG_CS_1SECOND_PASSED")]
    [InlineData(0x7B, "MSG_SC_HIDDEN_TEAM_INDEX")]
    [InlineData(0x93, "MSG_SC_FORCELEAVE_POLLSTART")]
    [InlineData(0x100, "MSG_SC_DAMAGESITE")]
    [InlineData(0x00FF, "MSG_SC_AMMOSUPPLYSITE")]
    [InlineData(0x0200, "MSG_SC_NJ_AI_FIRE_START")]
    public void KnownIds_HaveNativeNames(ushort id, string expected)
    {
        Assert.True(EMessageIdCatalog.IsKnown(id));
        Assert.Equal(expected, EMessageIdCatalog.GetName(id));
    }

    [Theory]
    [InlineData(0x100)]
    [InlineData(0x12D)]
    [InlineData(0x1FE)]
    [InlineData(0x0200)]
    public void TryPeekMessageId_AcceptsIdsAboveLegacyCap(ushort id)
    {
        var payload = new byte[] { (byte)(id & 0xFF), (byte)(id >> 8) };
        Assert.True(LtMessageReader.TryPeekMessageId(payload, out var messageId));
        Assert.Equal((EMessageId)id, messageId);
    }

    [Fact]
    public void UnknownDecoded_ExposesCatalogName()
    {
        var unknown = new LtUnknownDecoded((EMessageId)0x71, 4);
        Assert.Equal("MSG_CS_1SECOND_PASSED", unknown.NativeName);
    }
}
