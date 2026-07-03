using System.Runtime.InteropServices;

namespace CrossFire.Replay.Cli.Interop;

/// <summary>Fixed-prefix fields of the 512-byte PacketSimulator match header block.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MatchHeaderFixedLayout
{
    public uint DeathMatchTypeRaw;
    public uint GameGoal;
    public short MapId;
    public byte ClanFlag;
}
