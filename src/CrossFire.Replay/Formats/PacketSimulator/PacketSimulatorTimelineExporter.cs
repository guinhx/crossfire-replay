using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrossFire.Replay.Protocol.Lt;

using CrossFire.Replay.Abstractions;

namespace CrossFire.Replay.Formats.PacketSimulator;

public sealed class TimelineExportDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string ToolkitVersion { get; init; } = ReplayToolkitVersion.Current;
    public string SourcePath { get; init; } = string.Empty;
    public int PacketCount { get; init; }
    public int DeduplicatedCount { get; init; }
    public int BinarySnapshotCount { get; init; }
    public IReadOnlyList<TimelineExportEntry> Packets { get; init; } = Array.Empty<TimelineExportEntry>();
    public IReadOnlyList<BinarySnapshotExportEntry> BinarySnapshots { get; init; } = Array.Empty<BinarySnapshotExportEntry>();
}

public sealed class TimelineExportEntry
{
    public uint Timestamp { get; init; }
    public string Source { get; init; } = string.Empty;
    public string PayloadKind { get; init; } = string.Empty;
    public string? MessageId { get; init; }
    public string? DecodedType { get; init; }
    public int PayloadSize { get; init; }
    public Dictionary<string, object?>? Decoded { get; init; }
}

public sealed class BinarySnapshotExportEntry
{
    public uint Timestamp { get; init; }
    public string Source { get; init; } = string.Empty;
    public int PayloadSize { get; init; }
    public uint HeaderField0 { get; init; }
    public uint HeaderField1 { get; init; }
    public uint HeaderField2 { get; init; }
    public int BodySize { get; init; }
    public int ChunkCount { get; init; }
    public int ChunkSize { get; init; }
    public int EmbeddedPacketCount { get; init; }
    public LtWorldPropsDecoded? WorldProps { get; init; }
    public IReadOnlyList<BinarySnapshotChunk> Chunks { get; init; } = Array.Empty<BinarySnapshotChunk>();
}

public static class PacketSimulatorTimelineExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static TimelineExportDocument CreateExport(PacketSimulatorReplayDocument document, bool deduplicated = true)
    {
        var packets = deduplicated && document.DeduplicatedTimeline.Count > 0
            ? document.DeduplicatedTimeline
            : document.UnifiedTimeline;

        return new TimelineExportDocument
        {
            SourcePath = document.SourcePath,
            PacketCount = document.UnifiedTimeline.Count,
            DeduplicatedCount = packets.Count,
            BinarySnapshotCount = document.BinarySnapshots.Count,
            Packets = packets.Select(ToEntry).ToArray(),
            BinarySnapshots = document.BinarySnapshots.Select(ToSnapshotEntry).ToArray(),
        };
    }

    public static void WriteJson(Stream output, PacketSimulatorReplayDocument document, bool deduplicated = true)
    {
        var export = CreateExport(document, deduplicated);
        JsonSerializer.Serialize(output, export, JsonOptions);
    }

    public static void WriteJson(string path, PacketSimulatorReplayDocument document, bool deduplicated = true)
    {
        using var stream = File.Create(path);
        WriteJson(stream, document, deduplicated);
    }

    public static void WriteCsv(TextWriter writer, PacketSimulatorReplayDocument document, bool deduplicated = true)
    {
        var packets = deduplicated && document.DeduplicatedTimeline.Count > 0
            ? document.DeduplicatedTimeline
            : document.UnifiedTimeline;

        writer.WriteLine("timestamp,source,payloadKind,messageId,decodedType,payloadSize");
        foreach (var packet in packets)
        {
            writer.Write(packet.Timestamp);
            writer.Write(',');
            writer.Write(packet.Source ?? PacketTimelineSource.MiddleBlob);
            writer.Write(',');
            writer.Write(packet.PayloadKind);
            writer.Write(',');
            writer.Write(packet.MessageId?.ToString() ?? string.Empty);
            writer.Write(',');
            writer.Write(packet.Decoded?.GetType().Name ?? string.Empty);
            writer.Write(',');
            writer.WriteLine(Math.Max(packet.Payload.Length, (int)packet.PayloadSize));
        }
    }

    public static void WriteCsv(string path, PacketSimulatorReplayDocument document, bool deduplicated = true)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        WriteCsv(writer, document, deduplicated);
    }

    private static TimelineExportEntry ToEntry(TimestampPacketRecord packet) =>
        new()
        {
            Timestamp = packet.Timestamp,
            Source = (packet.Source ?? PacketTimelineSource.MiddleBlob).ToString(),
            PayloadKind = packet.PayloadKind.ToString(),
            MessageId = packet.MessageId?.ToString(),
            DecodedType = packet.Decoded?.GetType().Name,
            PayloadSize = Math.Max(packet.Payload.Length, (int)packet.PayloadSize),
            Decoded = packet.Decoded is null ? null : SummarizeDecoded(packet.Decoded),
        };

    private static BinarySnapshotExportEntry ToSnapshotEntry(BinarySnapshotRecord snapshot) =>
        new()
        {
            Timestamp = snapshot.Timestamp,
            Source = snapshot.Source.ToString(),
            PayloadSize = snapshot.PayloadSize,
            HeaderField0 = snapshot.Header.Field0,
            HeaderField1 = snapshot.Header.Field1,
            HeaderField2 = snapshot.Header.Field2,
            BodySize = snapshot.Body.Length,
            ChunkCount = snapshot.Chunks.Count,
            ChunkSize = BinarySnapshotBodyReader.GuessChunkSize(snapshot.Header),
            EmbeddedPacketCount = snapshot.EmbeddedPackets.Count,
            WorldProps = snapshot.WorldProps,
            Chunks = snapshot.Chunks,
        };

    private static Dictionary<string, object?> SummarizeDecoded(LtDecodedMessage decoded) =>
        decoded switch
        {
            LtDamageDecoded damage => new()
            {
                ["attackerIndex"] = damage.AttackerIndex,
                ["damage"] = damage.Damage,
                ["damageType"] = damage.DamageType,
                ["weaponType"] = damage.WeaponType,
                ["hitNodeType"] = damage.HitNodeType,
            },
            LtSetWeaponSlotDecoded slot => new()
            {
                ["selectedSlotIndex"] = slot.SelectedSlotIndex,
                ["weaponCount"] = slot.WeaponTypes.Count,
                ["changeBagNum"] = slot.ChangeBagNum,
                ["bagNum"] = slot.BagNum,
                ["customBagCount"] = slot.CustomSetInfo.Count,
                ["hasExtendedTrailingData"] = slot.HasExtendedTrailingData,
            },
            LtCsVelAndRotDecoded vel => new()
            {
                ["includeVelocity"] = vel.IncludeVelocity,
                ["teleport"] = vel.Teleport,
                ["stand"] = vel.Stand,
                ["elapsedTime"] = vel.ElapsedTime,
                ["duck"] = vel.Duck,
                ["jump"] = vel.Jump,
            },
            LtCsReqDropWeaponDecoded drop => new()
            {
                ["slot"] = drop.Slot,
                ["ammoMag"] = drop.AmmoLeftInMagazine,
                ["ammoTotal"] = drop.AmmoLeftInTotal,
            },
            LtCsChangeWeaponDecoded change => new()
            {
                ["slot"] = change.Slot,
                ["curSlot"] = change.CurSlot,
                ["ammoMag"] = change.AmmoLeftInMagazine,
                ["wireMessageId"] = change.WireMessageId.ToString(),
            },
            LtCsLinkWeaponDecoded link => new()
            {
                ["curSlot"] = link.CurSlot,
                ["weaponSelect"] = link.CurWeaponSelect,
                ["wireMessageId"] = link.WireMessageId.ToString(),
            },
            LtCsFrogJumpDecoded => new() { ["marker"] = true },
            LtCsLandingStateDecoded landing => new()
            {
                ["velocityY"] = landing.VelocityY,
                ["landType"] = landing.LandType,
            },
            LtHitInfoDecoded hitInfo => new()
            {
                ["hitInfoBlobBytes"] = hitInfo.HitInfoBlobBytes,
                ["hitRates"] = hitInfo.HitRates,
                ["headKillRates"] = hitInfo.HeadKillRates,
                ["summingDamages"] = hitInfo.SummingDamages,
            },
            LtScoreDecoded score => new()
            {
                ["health"] = score.Health,
                ["kills"] = score.NumKill,
                ["deaths"] = score.NumDeath,
            },
            LtAllScoresDecoded allScores => new()
            {
                ["teamCount"] = allScores.TeamCount,
                ["playerCount"] = allScores.PlayerCount,
            },
            LtPlayerInDecoded playerIn => new()
            {
                ["characterIndex"] = playerIn.CharacterIndex,
                ["teamIndex"] = playerIn.TeamIndex,
                ["userName"] = playerIn.UserName,
            },
            LtPlayerDieDecoded die => new()
            {
                ["characterIndex"] = die.CharacterIndex,
                ["attackerIndex"] = die.AttackerIndex,
                ["weaponType"] = die.WeaponType,
            },
            LtShotInfoDecoded shot => new()
            {
                ["weaponType"] = shot.WeaponType,
                ["currentAmmo"] = shot.CurrentAmmo,
                ["magazineAmmo"] = shot.MagazineAmmo,
            },
            LtSetCurWeaponDecoded weapon => new()
            {
                ["characterIndex"] = weapon.CharacterIndex,
                ["weaponType"] = weapon.WeaponType,
            },
            LtThrowGrenadeDecoded grenade => new()
            {
                ["weaponType"] = grenade.WeaponType,
                ["characterIndex"] = grenade.CharacterIndex,
            },
            LtVelocityDecoded velocity => new()
            {
                ["characterIndex"] = velocity.CharacterIndex,
                ["teleport"] = velocity.Teleport,
            },
            LtAnimDecoded animCs => new()
            {
                ["animIndex"] = animCs.AnimIndex,
                ["characterIndex"] = animCs.CharacterIndex,
            },
            LtScAnimDecoded animSc => new()
            {
                ["animIndex"] = animSc.AnimIndex,
                ["characterIndex"] = animSc.CharacterIndex,
            },
            LtPlayerScoreDecoded playerScore => new()
            {
                ["characterIndex"] = playerScore.CharacterIndex,
                ["score"] = playerScore.Score,
            },
            LtFireDecoded fire => new()
            {
                ["characterIndex"] = fire.CharacterIndex,
                ["gunRotX"] = fire.GunRot.X,
                ["gunRotY"] = fire.GunRot.Y,
                ["gunRotZ"] = fire.GunRot.Z,
            },
            LtRoundStartDecoded round => new() { ["autoSideChangeState"] = round.AutoSideChangeState },
            LtRoundEndDecoded round => new()
            {
                ["winTeamIndex"] = round.WinTeamIndex,
                ["clanWinTeamIndex"] = round.ClanWinTeamIndex,
            },
            LtSendC4ObjectDecoded c4 => new()
            {
                ["planted"] = c4.Planted,
                ["bombSiteIndex"] = c4.BombSiteIndex,
            },
            LtWorldPropsDecoded world => new()
            {
                ["farZ"] = world.FarZ,
                ["fogEnable"] = world.FogEnable,
                ["fogNearZ"] = world.FogNearZ,
                ["fogFarZ"] = world.FogFarZ,
                ["skyScale"] = world.SkyScale,
            },
            LtBombSitesDecoded sites => new() { ["siteCount"] = sites.Sites.Count },
            LtScCheatScaleDownDecoded scale => new()
            {
                ["scale"] = scale.Scale,
                ["sendIndex"] = scale.SendIndex,
            },
            LtScAddTimeItemDecoded addTime => new()
            {
                ["hasBody"] = addTime.HasBody,
                ["characterIndex"] = addTime.CharacterIndex,
                ["itemType"] = addTime.ItemType,
            },
            LtScDamageSiteStateDecoded damageSite => new()
            {
                ["objectHandle"] = damageSite.ObjectHandle,
                ["isOn"] = damageSite.IsOn,
            },
            LtScDefenceTowerChangeStateDecoded tower => new()
            {
                ["towerIndex"] = tower.TowerIndex,
                ["stateA"] = tower.StateA,
                ["stateB"] = tower.StateB,
            },
            LtScBossReviveDecoded boss => new() { ["entryCount"] = boss.Entries.Count },
            LtScArcadiaCoreSwitchStateDecoded arcadia => new()
            {
                ["coreObjectId"] = arcadia.CoreObjectId,
                ["eventKind"] = arcadia.EventKind,
            },
            LtScIngameItemDroppedDecoded drop => new()
            {
                ["itemId"] = drop.ItemId,
                ["dropKind"] = drop.DropKind,
            },
            LtScDefenceTowerFireDecoded fireTower => new()
            {
                ["towerIndex"] = fireTower.TowerIndex,
                ["state"] = fireTower.State,
            },
            LtUnknownDecoded unknown => new()
            {
                ["nativeName"] = unknown.NativeName,
                ["payloadBytes"] = unknown.PayloadBytes,
            },
            _ => new() { ["messageId"] = decoded.MessageId.ToString() },
        };
}
