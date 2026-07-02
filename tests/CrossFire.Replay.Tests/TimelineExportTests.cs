using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using System.Text.Json;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class TimelineExportTests
{
    private const string UserCfn = @"D:\Dinho\Documents\Cross Fire\Replay\CFReplay20260701_0000.cfn";

    [Fact]
    public void UserCfn_ExportsTimelineJson()
    {
        if (!File.Exists(UserCfn))
            return;

        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(UserCfn));
        var exportPath = Path.Combine(Path.GetTempPath(), $"cf_timeline_{Guid.NewGuid():N}.json");

        try
        {
            PacketSimulatorTimelineExporter.WriteJson(exportPath, doc);
            Assert.True(new FileInfo(exportPath).Length > 0);

            using var stream = File.OpenRead(exportPath);
            var export = JsonSerializer.Deserialize<TimelineExportDocument>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            Assert.NotNull(export);
            Assert.Equal(doc.SourcePath, export!.SourcePath);
            Assert.Equal(doc.UnifiedTimeline.Count, export.PacketCount);
            Assert.Equal(doc.DeduplicatedTimeline.Count, export.DeduplicatedCount);
            Assert.True(export.Packets.Count > 0);
            Assert.Equal(1, export.BinarySnapshotCount);
            Assert.Single(export.BinarySnapshots);
            Assert.Equal(296_179, export.BinarySnapshots[0].PayloadSize);
            Assert.True(export.BinarySnapshots[0].BodySize > 200_000);
            Assert.True(export.BinarySnapshots[0].HeaderField0 > 0);
            Assert.Equal(5, export.BinarySnapshots[0].ChunkCount);
            Assert.Equal(65_536, export.BinarySnapshots[0].ChunkSize);
        }
        finally
        {
            if (File.Exists(exportPath))
                File.Delete(exportPath);
        }
    }

    [Fact]
    public void UserCfn_DeduplicatedTimeline_IsNotLargerThanUnified()
    {
        if (!File.Exists(UserCfn))
            return;

        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(UserCfn));
        Assert.True(doc.DeduplicatedTimeline.Count <= doc.UnifiedTimeline.Count);
        Assert.True(doc.DeduplicatedTimeline.Count >= 100);
        Assert.Single(doc.BinarySnapshots);
        Assert.Equal(296_179, doc.BinarySnapshots[0].PayloadSize);
    }
}
