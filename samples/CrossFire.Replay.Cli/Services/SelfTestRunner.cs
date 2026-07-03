using CrossFire.Replay;
using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Compression;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.SimpleProtocol;
using CrossFire.Replay.Protocol;

namespace CrossFire.Replay.Cli.Services;

internal static class SelfTestRunner
{
    public static int RunAll(Action<string>? onError = null)
    {
        var failures = 0;
        if (!RunPlainRoundTrip(onError)) failures++;
        if (!RunChecksumRoundTrip(onError)) failures++;
        if (!RunContainerRoundTrip(ReplayContainerKind.BrotliWrapper, ".cfo", onError)) failures++;
        if (!RunContainerRoundTrip(ReplayContainerKind.EncryptedBrotliWrapper, ".cfn", onError)) failures++;
        return failures;
    }

    private static CfrDocument CreateSampleDocument()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "cfreplay_test.cfr");
        using (var stream = File.Create(tempFile))
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
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
            var bl = System.Text.Encoding.ASCII.GetBytes("TeamBL");
            writer.Write(bl.Length + 1);
            writer.Write(bl);
            writer.Write((byte)0);
            var gr = System.Text.Encoding.ASCII.GetBytes("TeamGR");
            writer.Write(gr.Length + 1);
            writer.Write(gr);
            writer.Write((byte)0);
            writer.Write(false);
            writer.Write(false);

            writer.Write((byte)SimpleProtocolId.RoundStart);
            writer.Write((byte)1);
            writer.Write(1500u);
            writer.Write(0);
        }

        var doc = CfrReader.ReadFile(tempFile);
        File.Delete(tempFile);
        return doc;
    }

    private static bool RunPlainRoundTrip(Action<string>? onError)
    {
        var doc = CreateSampleDocument();
        var roundTrip = Path.Combine(Path.GetTempPath(), "cfreplay_test_out.cfr");
        CfrWriter.WriteFile(roundTrip, doc);
        var doc2 = CfrReader.ReadFile(roundTrip);
        File.Delete(roundTrip);

        var ok = doc.Messages.Count == 2 && doc2.Messages.Count == 2;
        if (!ok)
            onError?.Invoke("Plain CFR round-trip failed.");
        return ok;
    }

    private static bool RunChecksumRoundTrip(Action<string>? onError)
    {
        var doc = CreateSampleDocument();
        var roundTrip = Path.Combine(Path.GetTempPath(), "cfreplay_test_checksum.cfr");
        CfrWriter.WriteFile(roundTrip, doc, prependChecksum: true);
        var doc2 = CfrReader.ReadFile(roundTrip);
        File.Delete(roundTrip);

        var ok = doc2.HasChecksum && doc2.Messages.Count == 2;
        if (!ok)
            onError?.Invoke("Checksum CFR round-trip failed.");
        return ok;
    }

    private static bool RunContainerRoundTrip(
        ReplayContainerKind containerKind,
        string extension,
        Action<string>? onError)
    {
        var doc = CreateSampleDocument();
        var writer = ReplayWriteService.Default;
        var options = containerKind switch
        {
            ReplayContainerKind.BrotliWrapper => ReplayWriteOptions.CfoWrapper,
            ReplayContainerKind.EncryptedBrotliWrapper => ReplayWriteOptions.CfnWrapper,
            _ => ReplayWriteOptions.PlainCfr,
        };

        var wrapped = writer.Write(doc, options);
        var decoder = new ReplayContainerDecoder();
        if (!decoder.CanDecode(wrapped))
        {
            onError?.Invoke($"Container encode failed for {extension}.");
            return false;
        }

        var decoded = decoder.Decode(wrapped);
        var roundTrip = CfrReader.Read(decoded.Payload);
        var ok = roundTrip.Messages.Count == 2;
        if (!ok)
            onError?.Invoke($"Container round-trip failed for {extension}.");
        return ok;
    }
}
