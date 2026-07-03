using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Tests.Support;
using System.Text.Json;
using Xunit;

namespace CrossFire.Replay.Tests;

public sealed class TimelineExportTests
{
    private static bool TryFixture(out string path) => ReplayFixturePaths.TryGetPrimaryModernCfn(out path);

    [Fact]
    public void UserCfn_ExportsTimelineJson()
    {
        if (!TryFixture(out var path))
            return;

        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
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
            Assert.Equal(1, export.SchemaVersion);
            Assert.False(string.IsNullOrEmpty(export.ToolkitVersion));
            Assert.Equal(doc.BinarySnapshots.Count, export.BinarySnapshotCount);
            Assert.Equal(export.BinarySnapshots.Count, export.BinarySnapshotCount);
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
        if (!TryFixture(out var path))
            return;

        var doc = Assert.IsType<PacketSimulatorReplayDocument>(ReplayService.Default.Read(path));
        Assert.True(doc.DeduplicatedTimeline.Count <= doc.UnifiedTimeline.Count);
        Assert.True(doc.DeduplicatedTimeline.Count >= 100);
        if (doc.BinarySnapshots.Count > 0)
        {
            Assert.True(doc.BinarySnapshots[0].PayloadSize > 0);
            Assert.NotEmpty(doc.BinarySnapshots[0].EmbeddedPackets);
        }
    }
}
