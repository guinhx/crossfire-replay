using System.Runtime.InteropServices;

namespace CrossFire.Replay.Cli.Interop;

/// <summary>First 8 bytes of a wrapped .cfo / .cfn outer container.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ReplayContainerHeader
{
    public uint HeaderWord;
    public uint DeclaredOutputSize;

    public byte KindByte => (byte)(HeaderWord & 0xFF);
    public int KeyIndex => (int)((HeaderWord >> 16) & 0xFF);
    public int IvIndex => (int)((HeaderWord >> 24) & 0xFF);
}
