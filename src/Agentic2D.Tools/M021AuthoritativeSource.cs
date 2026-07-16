using Agentic2D.Persistence;
using Agentic2D.Presentation;

namespace Agentic2D.Tools;

/// <summary>Builds presentation input from committed M019 and M020 transactions only.</summary>
internal sealed record M021AuthoritativeSource(
    PersistentWorldSnapshot Snapshot,
    IReadOnlyList<PresentationEvent> Events,
    ConditionEvidence LockedDoorCondition,
    int HealthCurrent,
    int HealthMaximum,
    IReadOnlyList<CameraTargetEvidence> CameraTargets);

internal static class M021AuthoritativeSourceFactory
{
    public static M021AuthoritativeSource CreateJourney()
    {
        var persistent = PersistentWorldRuntime.CreateInitial();
        var targets = new List<CameraTargetEvidence> { Target(persistent) };
        persistent.AdvanceTo(1); persistent.MovePlayerTo(4, 3, "move.collect", "input.1"); targets.Add(Target(persistent)); persistent.CollectCrystal("collect.crystal", "input.1");
        persistent.AdvanceTo(2); persistent.MovePlayerTo(5, 3, "move.damage", "input.2"); targets.Add(Target(persistent)); persistent.DamagePlayer(3, "entity.hazard", "damage.player", "input.2");
        persistent.AdvanceTo(3); persistent.MovePlayerTo(12, 3, "move.locked-door", "input.3"); targets.Add(Target(persistent)); var locked = persistent.OpenDoor("door.vault.locked", "input.3");
        persistent.AdvanceTo(4); persistent.MovePlayerTo(8, 3, "move.switch", "input.4"); targets.Add(Target(persistent)); persistent.ActivateSwitch("switch.vault", "input.4");
        persistent.AdvanceTo(5); persistent.MovePlayerTo(12, 3, "move.open-door", "input.5"); targets.Add(Target(persistent)); persistent.OpenDoor("door.vault", "input.5");
        persistent.AdvanceTo(6); targets.Add(Target(persistent));

        var events = persistent.Events
            .Select(ToPresentationEvent)

            .Append(new PresentationEvent("operation.save.0001", "save.created", persistent.Tick, null, null, "screen", "successful-persistence-operation"))
            .OrderBy(x => x.Tick)
            .ThenBy(x => x.EventId, StringComparer.Ordinal)
            .ToArray();
        var snapshot = persistent.Snapshot(); var health = snapshot.Entities.Single(x => x.Id == PersistentIds.Player).Resources["resource.health"]; return new(snapshot, events, locked.Condition!, health, 10, targets);
    }

    public static M021AuthoritativeSource FromLoadedSnapshot(PersistentWorldSnapshot snapshot)
    {
        var player = snapshot.Entities.Single(x => x.Id == PersistentIds.Player);
        var health = player.Resources.TryGetValue("resource.health", out var current) ? current : 10;
        var raw = player.Components["component.presentation-position"].Split(','); return new(snapshot, [], new ConditionEvidence("load-reconstruction", true, [], "authoritative-snapshot"), health, 10, [new CameraTargetEvidence(PersistentIds.Player, snapshot.RuntimeTick, int.Parse(raw[0], System.Globalization.CultureInfo.InvariantCulture), int.Parse(raw[1], System.Globalization.CultureInfo.InvariantCulture), CanonicalJson.Fingerprint(snapshot))]);
    }

    private static CameraTargetEvidence Target(PersistentWorldRuntime runtime) { var raw = runtime.Entities[PersistentIds.Player].Components["component.presentation-position"].Split(','); return new CameraTargetEvidence(PersistentIds.Player, runtime.Tick, int.Parse(raw[0], System.Globalization.CultureInfo.InvariantCulture), int.Parse(raw[1], System.Globalization.CultureInfo.InvariantCulture), CanonicalJson.Fingerprint(runtime.Snapshot())); }

    private static PresentationEvent ToPresentationEvent(PersistentWorldEvent value) => new(value.Id, value.Type, value.Tick, value.SourceId, value.TargetId, Anchor(value.TargetId), "post-commit-m020");
    private static string Anchor(string? target) => target switch
    {
        PersistentIds.Crystal => "4,3",
        PersistentIds.Player => "5,3",
        PersistentIds.Switch => "8,3",
        PersistentIds.Door => "12,3",
        _ => "screen"
    };
}
