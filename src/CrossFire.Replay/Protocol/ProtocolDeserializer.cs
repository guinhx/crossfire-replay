using CrossFire.Replay.IO;
using CrossFire.Replay.Protocol.Messages;

namespace CrossFire.Replay.Protocol;

public static partial class ProtocolDeserializer
{
    public static ReplayMessage ReadMessage(SimpleProtocolId id, CfrBinaryReader reader, CfrReadContext ctx)
    {
        var start = reader.Position;
        var msg = ReadMessageCore(id, reader, ctx);
        var end = reader.Position;
        reader.Seek(start, SeekOrigin.Begin);
        msg.Payload = reader.ReadBytesExact((int)(end - start));
        reader.Seek(end, SeekOrigin.Begin);
        return msg;
    }
}
