using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Compression;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Tests.Support;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class ModernRoundTripTests
{
    [Fact]
    public void UserCfn_ModernInnerPayload_RoundTrips()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var original = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        var inner = PacketSimulatorModernWriter.BuildInnerPayload(original);
        Assert.Equal(original.InnerPayload.Length, inner.Length);
        Assert.Equal(original.InnerPayload, inner);

        var wrapped = ReplayWriteService.Default.Write(original, ReplayWriteOptions.CfnWrapper);
        var decoded = new ReplayContainerDecoder().Decode(wrapped).Payload;
        Assert.Equal(original.InnerPayload.Length, decoded.Length);
        Assert.Equal(original.InnerPayload, decoded);
    }

    [Fact]
    public void UserCfn_BinarySnapshot_HasChunkedBody()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        if (doc.BinarySnapshots.Count == 0)
            return;

        var snapshot = Assert.Single(doc.BinarySnapshots);
        Assert.Equal(65_536, BinarySnapshotBodyReader.GuessChunkSize(snapshot.Header));
        Assert.Equal(5, snapshot.Chunks.Count);
        Assert.Equal(65_536, snapshot.Chunks[0].Size);
        Assert.Equal(65_536, snapshot.Chunks[1].Size);
        Assert.Equal(65_536, snapshot.Chunks[2].Size);
        Assert.Equal(65_536, snapshot.Chunks[3].Size);
        Assert.Equal(34_023, snapshot.Chunks[4].Size);
    }

    [Fact]
    public void UserCfn_ExportsTimelineCsv()
    {
        if (!ReplayFixturePaths.TryGetPrimaryModernCfn(out var path))
            return;

        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        var exportPath = Path.Combine(Path.GetTempPath(), $"cf_timeline_{Guid.NewGuid():N}.csv");

        try
        {
            PacketSimulatorTimelineExporter.WriteCsv(exportPath, doc);
            var lines = File.ReadAllLines(exportPath);
            Assert.True(lines.Length > 100);
            Assert.Equal("timestamp,source,payloadKind,messageId,decodedType,payloadSize", lines[0]);
        }
        finally
        {
            if (File.Exists(exportPath))
                File.Delete(exportPath);
        }
    }
}
