using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol;
using CrossFire.Replay.Protocol.Lt;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class IltDecodeCoverageTests
{
    [Fact]
    public void Analyze_CountsSemanticAndUnknown()
    {
        var packets = new[]
        {
            new TimestampPacketRecord
            {
                MessageId = EMessageId.MsgScDamage,
                Decoded = new LtDamageDecoded(1, default, 50, 0, 100, false, 0),
                Payload = new byte[8],
            },
            new TimestampPacketRecord
            {
                MessageId = EMessageId.MsgScDamage,
                Decoded = new LtUnknownDecoded(EMessageId.MsgScDamage, 8),
                Payload = new byte[8],
            },
            new TimestampPacketRecord
            {
                MessageId = EMessageId.MsgScFire,
                Decoded = new LtUnknownDecoded(EMessageId.MsgScFire, 4),
                Payload = new byte[4],
            },
            new TimestampPacketRecord
            {
                MessageId = null,
                Decoded = null,
                Payload = new byte[2],
            },
        };

        var report = IltDecodeCoverage.Analyze(packets);

        Assert.Equal(4, report.TotalPackets);
        Assert.Equal(3, report.WithMessageId);
        Assert.Equal(1, report.SemanticallyDecoded);
        Assert.Equal(2, report.UnknownDecoded);
        Assert.Equal(1, report.Undecoded);
        Assert.Equal(2, report.DistinctMessageIds);
        Assert.Equal(1, report.DistinctSemanticIds);
        Assert.Equal(2, report.DistinctUnknownIds);
    }

    [Fact]
    public void IsSemanticDecode_DistinguishesUnknown()
    {
        Assert.True(IltDecodeCoverage.IsSemanticDecode(new LtFireDecoded(0, default, 0, false)));
        Assert.False(IltDecodeCoverage.IsSemanticDecode(new LtUnknownDecoded(EMessageId.MsgScFire, 4)));
        Assert.False(IltDecodeCoverage.IsSemanticDecode(null));
    }

    [Fact]
    public void UserFixture_ReportsCoverageWhenAvailable()
    {
        if (!Support.ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var ps = (PacketSimulatorReplayDocument)Core.ReplayService.Default.Read(path);
        var report = IltDecodeCoverage.Analyze(ps);

        Assert.True(report.TotalPackets > 0);
        Assert.True(report.WithMessageId > 0);
        Assert.True(report.SemanticallyDecoded > 0);
        Assert.True(report.SemanticPacketRatio >= 0.95);
    }
}
