namespace CrossFire.Replay.Protocol;

/// <summary>Message IDs present in the enum but skipped when reading legacy .cfr samples.</summary>
public static class SimpleProtocolSupport
{
    public static bool IsRejectedByClient(SimpleProtocolId id) =>
        id switch
        {
            SimpleProtocolId.ObjSfxMessage => true,
            SimpleProtocolId.GameEnd => true,
            SimpleProtocolId.Sprint => true,
            SimpleProtocolId.StealthGaugeInfo => true,
            SimpleProtocolId.Escape2TeamMatchMapRegion => true,
            SimpleProtocolId.Escape2TeamMatchStage => true,
            _ => false,
        };

    public static IReadOnlyList<SimpleProtocolId> ClientRejected { get; } =
    [
        SimpleProtocolId.ObjSfxMessage,
        SimpleProtocolId.GameEnd,
        SimpleProtocolId.Sprint,
        SimpleProtocolId.StealthGaugeInfo,
        SimpleProtocolId.Escape2TeamMatchMapRegion,
        SimpleProtocolId.Escape2TeamMatchStage,
    ];
}
