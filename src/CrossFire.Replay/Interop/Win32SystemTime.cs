using System.Runtime.InteropServices;

namespace CrossFire.Replay.Interop;

/// <summary>Win32 <c>SYSTEMTIME</c> (16 bytes).</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct Win32SystemTime
{
    public ushort Year;
    public ushort Month;
    public ushort DayOfWeek;
    public ushort Day;
    public ushort Hour;
    public ushort Minute;
    public ushort Second;
    public ushort Milliseconds;
}
