namespace CrossFire.Replay.Protocol.Lt;

/// <summary>
/// ILT message id symbol names (generated catalog).
/// </summary>
public static partial class EMessageIdCatalog
{
    private static readonly Dictionary<ushort, string> Names = BuildGeneratedNames();

    public static ushort MaxGameplayId => MaxNativeMessageId;

    public static bool IsPlausible(ushort id) => id <= MaxGameplayId;

    public static bool IsKnown(ushort id) => Names.ContainsKey(id);

    public static string GetName(ushort id) =>
        Names.TryGetValue(id, out var name) ? name : $"Unknown_0x{id:X4}";

    public static string Format(ushort id) =>
        IsKnown(id) ? $"{GetName(id)} (0x{id:X4})" : $"0x{id:X4}";
}
