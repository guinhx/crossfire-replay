namespace CrossFire.Replay.Formats.PacketSimulator;

/// <summary>On-disk PacketSimulator inner layout constants (legacy and modern).</summary>
public static class PacketSimulatorLayout
{
    public const uint ContainerVersion = 6;
    public const uint ContainerMagic = 1020107; // 0xF90CB
    public const int HeaderBlockSize = 512;
    public const int MaxSpectatingRoomInfoSize = 47963; // 0xBB5B

    /// <summary>Jul/2026 .cfn inner signature (observed in CFReplay20260701_0000.cfn).</summary>
    public const uint ModernSignature = 0x7FFFFFFE;

    public const int ModernMetadataBlockSize = 1024;
    public const int ModernExtraBlockSize = 4;
    public const int ModernDescriptorOffset = ModernMetadataBlockSize + ModernExtraBlockSize + 12; // after sig + sizes
}

public enum PacketSimulatorInnerFormat
{
    /// <summary>Unrecognized layout.</summary>
    Unknown = 0,

    /// <summary>Legacy inner header: u32(6), u32(1020107), u32(headerLen), header block...</summary>
    LegacyV2022 = 1,

    /// <summary>Modern .cfn: u32(0x7FFFFFFE), metadata(1024), extra(4), descriptor, middle blob, 3 raw archives.</summary>
    ModernV2026 = 2,
}
