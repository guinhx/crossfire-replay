using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Lt;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class LtMessageSemanticTests
{
    private const string UserCfn = @"D:\Dinho\Documents\Cross Fire\Replay\CFReplay20260701_0000.cfn";

    [Fact]
    public void Score_RoundTripsThroughWriter()
    {
        var original = new LtScoreDecoded(100, 50, 12, 3, 8000);
        var bytes = LtMessageWriter.Encode(original);
        var decoded = Assert.IsType<LtScoreDecoded>(LtMessageReader.TryDecode(bytes));
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void RoundTimeLeft_RoundTripsThroughWriter()
    {
        var original = new LtRoundTimeLeftDecoded(175.5f, Enumerable.Repeat((ushort)100, 16).ToArray());
        var bytes = LtMessageWriter.Encode(original);
        var decoded = Assert.IsType<LtRoundTimeLeftDecoded>(LtMessageReader.TryDecode(bytes));
        Assert.Equal(original.RoundTimeLeftSeconds, decoded.RoundTimeLeftSeconds);
        Assert.Equal(original.TeamHealth, decoded.TeamHealth);
    }

    [Fact]
    public void ShotInfo_RoundTripsThroughWriter()
    {
        var original = new LtShotInfoDecoded(3, 30, 30, 90, 90, 7, true, false, false, true, 2, 1, 0);
        var bytes = LtMessageWriter.Encode(original);
        var decoded = Assert.IsType<LtShotInfoDecoded>(LtMessageReader.TryDecode(bytes));
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void AllScores_RoundTripsThroughWriter()
    {
        var player = new LtAllScoresPlayerDecoded(0, 1, 0, 100, 12, 3, 0, 1500, 25, 99887766UL, 0, 10, 0);
        var original = new LtAllScoresDecoded(
            2,
            new ushort[] { 5, 4 },
            1,
            new[] { player },
            99887766UL,
            0UL,
            15,
            13,
            1,
            0,
            new ushort[] { 2, 3 });

        var bytes = LtMessageWriter.Encode(original);
        var decoded = Assert.IsType<LtAllScoresDecoded>(LtMessageReader.TryDecode(bytes));
        Assert.Equal(original.TeamCount, decoded.TeamCount);
        Assert.Equal(original.PlayerCount, decoded.PlayerCount);
        Assert.Equal(original.Players[0].Kills, decoded.Players[0].Kills);
        Assert.Equal(original.Players[0].UserId, decoded.Players[0].UserId);
        Assert.Equal(original.TotalKills, decoded.TotalKills);
    }

    [Fact]
    public void UserCfn_DecodesPrioritySemanticMessages()
    {
        if (!File.Exists(UserCfn))
            return;

        var doc = ReplayService.Default.Read(UserCfn);
        var ps = Assert.IsType<PacketSimulatorReplayDocument>(doc);

        var decodedCounts = new Dictionary<EMessageId, int>();
        foreach (var packet in ps.UnifiedTimeline)
        {
            var decoded = LtMessageReader.TryDecode(packet.Payload);
            if (decoded is null or LtUnknownDecoded)
                continue;

            decodedCounts.TryGetValue(decoded.MessageId, out var count);
            decodedCounts[decoded.MessageId] = count + 1;
        }

        Assert.True(decodedCounts.GetValueOrDefault(EMessageId.MsgScRoundStart) >= 1);
        Assert.True(decodedCounts.GetValueOrDefault(EMessageId.MsgScBombSites) >= 1);
    }

    [Fact]
    public void UserReplayFolder_DecodesPhase3SemanticMessages()
    {
        const string folder = @"D:\Dinho\Documents\Cross Fire\Replay";
        if (!Directory.Exists(folder))
            return;

        var decodedCounts = new Dictionary<EMessageId, int>();
        foreach (var file in Directory.GetFiles(folder, "*.cfn"))
        {
            var doc = ReplayService.Default.Read(file);
            if (doc is not PacketSimulatorReplayDocument ps)
                continue;

            foreach (var packet in ps.UnifiedTimeline)
            {
                var decoded = LtMessageReader.TryDecode(packet.Payload);
                if (decoded is null or LtUnknownDecoded)
                    continue;

                decodedCounts.TryGetValue(decoded.MessageId, out var count);
                decodedCounts[decoded.MessageId] = count + 1;
            }
        }

        EMessageId[] phase3 =
        [
            EMessageId.MsgScScore,
            EMessageId.MsgScRoundTimeLeft,
            EMessageId.MsgScAllScores,
            EMessageId.MsgScThrowGrenade,
            EMessageId.MsgScShotInfo,
            EMessageId.MsgScSetCurWeapon,
            EMessageId.MsgScLadderArea,
            EMessageId.MsgScPlayerRespawn,
            EMessageId.MsgScPlayerIn,
        ];

        Assert.Contains(phase3, id => decodedCounts.GetValueOrDefault(id) >= 1);
    }

    [Fact]
    public void UserCfn_AllScores_DecodesWhenPresent()
    {
        if (!File.Exists(UserCfn))
            return;

        var doc = ReplayService.Default.Read(UserCfn);
        var ps = Assert.IsType<PacketSimulatorReplayDocument>(doc);

        var candidates = EnumeratePackets(ps)
            .Where(p => p.MessageId == EMessageId.MsgScAllScores
                || LtMessageReader.TryPeekMessageId(p.Payload, out var id) && id == EMessageId.MsgScAllScores)
            .ToList();

        if (candidates.Count == 0)
            return;

        Assert.Contains(candidates, p => LtMessageReader.TryDecode(p.Payload) is LtAllScoresDecoded);
    }

    [Fact]
    public void UserCfn_AllScores_HasPlayerRows()
    {
        if (!File.Exists(UserCfn))
            return;

        var doc = ReplayService.Default.Read(UserCfn);
        var ps = Assert.IsType<PacketSimulatorReplayDocument>(doc);

        LtAllScoresDecoded? allScores = null;
        foreach (var packet in EnumeratePackets(ps))
        {
            if (LtMessageReader.TryDecode(packet.Payload) is LtAllScoresDecoded decoded)
            {
                allScores = decoded;
                break;
            }
        }

        if (allScores is null)
            return;

        Assert.InRange(allScores.PlayerCount, (byte)1, (byte)16);
        Assert.Equal(allScores.PlayerCount, (byte)allScores.Players.Count);
        Assert.All(allScores.Players, p => Assert.InRange(p.CharacterIndex, (byte)0, (byte)15));
    }

    private static IEnumerable<TimestampPacketRecord> EnumeratePackets(PacketSimulatorReplayDocument ps)
    {
        foreach (var packet in ps.UnifiedTimeline)
            yield return packet;

        foreach (var packet in ps.MiddleBlobPackets.Packets)
            yield return packet;
    }
}
