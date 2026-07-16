using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic2D.Gameplay;

namespace Agentic2D.Persistence;

public static class PersistentIds
{
    public const string Schema = "agentic2d.canonical-save.v1", Project = "project.agentic2d-smoke", World = "world.persistent-smoke";
    public const string Player = "entity.player", Crystal = "entity.world-item.crystal", Switch = "entity.switch.vault-power", Door = "entity.door.vault-access";
    public const string VaultPower = "flag.switch.vault-power", VaultAccess = "flag.door.vault-access";
}
public static class CanonicalJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    public static string Serialize<T>(T x) => JsonSerializer.Serialize(x, Options).Replace("\r\n", "\n", StringComparison.Ordinal);
    public static string Fingerprint<T>(T x) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Serialize(x)))).ToLowerInvariant();
}
public interface IPersistenceContributor
{
    string Id { get; }
    int SchemaVersion { get; }
    bool Required { get; }
    bool SafeToIgnoreWhenOptional { get; }
    PersistenceContributorCapture Capture(PersistentWorldSnapshot snapshot);
    IReadOnlyList<string> ValidateCompatibility(PersistenceContributorDescriptor stored);
    IReadOnlyList<string> ValidateReferences(PersistentWorldSnapshot snapshot);
    PersistenceLoadPlanStep CreateLoadPlan(PersistentWorldSnapshot snapshot);
    void Apply(PersistentWorldSnapshot snapshot, PersistentWorldLoadTransaction transaction);
}
public sealed record PersistenceContributorDescriptor(string Id, int SchemaVersion, bool Required, bool SafeToIgnoreWhenOptional, string Fingerprint);
public sealed record PersistenceContributorCapture(object CanonicalRecords, string Fingerprint);
public sealed record PersistenceLoadPlanStep(string ContributorId, int SchemaVersion, int RecordCount);
public sealed record PersistenceLoadPlan(IReadOnlyList<PersistenceLoadPlanStep> Steps, bool FreshRuntimeOnly, bool DoorCollisionRestoredBeforeMovement);
public sealed class PersistenceContributorRegistry
{
    public static readonly string[] RequiredIds = ["persistence.runtime", "persistence.entities", "persistence.components", "persistence.resources", "persistence.lifecycle", "persistence.inventory", "persistence.removed-entities", "persistence.interaction-state", "persistence.trigger-state", "persistence.animation-continuity", "persistence.flags", "persistence.switches", "persistence.doors"];
    private readonly SortedDictionary<string, IPersistenceContributor> contributors = new(StringComparer.Ordinal);
    public PersistenceContributorRegistry(IEnumerable<IPersistenceContributor>? values = null) { foreach (var x in values ?? RequiredIds.Select(x => new Contributor(x))) Register(x); }
    public IReadOnlyList<IPersistenceContributor> Contributors => contributors.Values.ToArray();
    public void Register(IPersistenceContributor x) { if (string.IsNullOrWhiteSpace(x.Id) || x.SchemaVersion < 1) throw new ArgumentException("A contributor needs stable ID and schema version."); if (!contributors.TryAdd(x.Id, x)) throw new InvalidOperationException("Duplicate contributor: " + x.Id); }
    public IReadOnlyList<PersistenceContributorDescriptor> Describe(PersistentWorldSnapshot s) => Contributors.Select(x => { var c = x.Capture(s); return new PersistenceContributorDescriptor(x.Id, x.SchemaVersion, x.Required, x.SafeToIgnoreWhenOptional, c.Fingerprint); }).ToArray();
    public IReadOnlyList<string> Validate(PersistentWorldSnapshot s, IReadOnlyList<PersistenceContributorDescriptor> stored)
    {
        var d = new List<string>();
        foreach (var x in Contributors.Where(x => x.Required)) { var y = stored.SingleOrDefault(y => y.Id == x.Id); if (y is null) d.Add("SAVE0205: missing required contributor " + x.Id); else d.AddRange(x.ValidateCompatibility(y)); }
        foreach (var x in stored) if (!contributors.TryGetValue(x.Id, out var known)) { if (x.Required || !x.SafeToIgnoreWhenOptional) d.Add("SAVE0207: unknown required or unsafe optional contributor " + x.Id); } else { d.AddRange(known.ValidateCompatibility(x)); if (known.Capture(s).Fingerprint != x.Fingerprint) d.Add("SAVE0209: contributor fingerprint mismatch " + x.Id); }
        d.AddRange(Contributors.SelectMany(x => x.ValidateReferences(s))); return d.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }
    public PersistenceLoadPlan CreateLoadPlan(PersistentWorldSnapshot snapshot, IReadOnlyList<PersistenceContributorDescriptor> stored) => new(stored.OrderBy(x => x.Id, StringComparer.Ordinal).Where(x => contributors.ContainsKey(x.Id)).Select(x => contributors[x.Id].CreateLoadPlan(snapshot)).ToArray(), true, true);
    public PersistentWorldRuntime Apply(PersistenceLoadPlan plan, PersistentWorldSnapshot snapshot, IEnumerable<FlagDefinition>? definitions) { var transaction = new PersistentWorldLoadTransaction(); foreach (var step in plan.Steps) contributors[step.ContributorId].Apply(snapshot, transaction); return transaction.Commit(Contributors.Where(x => x.Required).Select(x => x.Id), definitions); }
    private sealed class Contributor(string id) : IPersistenceContributor
    {
        public string Id { get; } = id; public int SchemaVersion => 1; public bool Required => true; public bool SafeToIgnoreWhenOptional => false;
        public PersistenceContributorCapture Capture(PersistentWorldSnapshot s) { var records = s.RecordsFor(Id); return new(records, CanonicalJson.Fingerprint(new { Id, records })); }
        public IReadOnlyList<string> ValidateCompatibility(PersistenceContributorDescriptor stored) => stored.SchemaVersion == SchemaVersion ? [] : ["SAVE0206: unsupported contributor schema " + Id];
        public IReadOnlyList<string> ValidateReferences(PersistentWorldSnapshot s) => Id switch
        {
            "persistence.removed-entities" when s.RemovedEntities.Any(x => s.Entities.Any(y => y.Id == x)) => ["SAVE0210: removed entity is present"],
            "persistence.switches" when s.Switches.Any(x => !s.Entities.Any(y => y.Id == x.EntityId)) => ["SAVE0211: switch references missing entity"],
            "persistence.doors" when s.Doors.Any(x => !s.Entities.Any(y => y.Id == x.EntityId)) => ["SAVE0212: door references missing entity"],
            _ => []
        };
        public PersistenceLoadPlanStep CreateLoadPlan(PersistentWorldSnapshot s) => new(Id, SchemaVersion, Count(s.RecordsFor(Id)));
        public void Apply(PersistentWorldSnapshot s, PersistentWorldLoadTransaction t)
        {
            switch (Id) { case "persistence.runtime": t.ApplyRuntime(s.RuntimeTick, s.DeterministicSeed, s.Continuation); break; case "persistence.entities": t.ApplyEntityIdentities(s.Entities.Select(x => x.Id)); break; case "persistence.components": t.ApplyComponents(s.Entities); break; case "persistence.resources": t.ApplyResources(s.Entities); break; case "persistence.lifecycle": t.ApplyLifecycle(s.Entities); break; case "persistence.inventory": t.ApplyInventory(s.Entities); break; case "persistence.removed-entities": t.ApplyRemovedEntities(s.RemovedEntities); break; case "persistence.interaction-state": t.ApplyInteractionState(s.InteractionState); break; case "persistence.trigger-state": t.ApplyTriggerState(s.TriggerState); break; case "persistence.animation-continuity": t.ApplyAnimationContinuity(s.AnimationContinuity); break; case "persistence.flags": t.ApplyFlags(s.Flags); break; case "persistence.switches": t.ApplySwitches(s.Switches); break; case "persistence.doors": t.ApplyDoors(s.Doors); break; }
            t.MarkApplied(Id);
        }
        private static int Count(object records) => records is System.Collections.ICollection collection ? collection.Count : records is System.Collections.IEnumerable sequence ? sequence.Cast<object>().Count() : 1;
    }
}
public sealed record SaveIdentity(string SaveId, string ProjectId, string ProjectFingerprint, string WorldId, string WorldFingerprint, string ContentFingerprint);
public sealed record SaveManifest(string Schema, int SchemaVersion, SaveIdentity Identity, int RuntimeTick, string DetermininisticSeed, string Continuation, IReadOnlyList<PersistenceContributorDescriptor> Contributors, string SnapshotFingerprint);
public sealed record SaveDocument(SaveManifest Manifest, PersistentWorldSnapshot Snapshot)
{ [JsonIgnore] public string Canonical => CanonicalJson.Serialize(this); [JsonIgnore] public string Fingerprint => CanonicalJson.Fingerprint(this); }
public sealed record PersistentEntity(string Id, string Lifecycle, IReadOnlyDictionary<string, string> Components, IReadOnlyList<InventoryEntry> Inventory, IReadOnlyDictionary<string, int> Resources);
public sealed record PersistentFlag(string Id, string Type, string Value, int Revision, string LastTransitionId, int RuntimeTick);
public sealed record FlagDefinition(string Id, string Type, IReadOnlyList<string> EnumValues)
{
    public bool Accepts(string value) => Type == "boolean" ? value is "true" or "false" : Type == "enum" && EnumValues.Contains(value, StringComparer.Ordinal);
}
public sealed record PersistentSwitch(string EntityId, string State, string FlagId, bool OneShot);
public sealed record PersistentDoor(string EntityId, string State, bool CollisionEnabled, string ConditionId);
public sealed record PersistentWorldEvent(string Id, string Type, int Tick, string TransactionId, string CorrelationId, string SourceId, string TargetId);
public sealed record FlagTransition(string Id, string FlagId, string Before, string After, int Revision, int Tick, string TransactionId, bool NoOp);
public sealed record ProjectionInvalidation(string Projection, string EntityId, int Tick, string Reason);
public sealed record PersistentWorldSnapshot(int RuntimeTick, string DeterministicSeed, string Continuation, IReadOnlyList<PersistentEntity> Entities, IReadOnlyList<string> RemovedEntities, IReadOnlyList<PersistentFlag> Flags, IReadOnlyList<PersistentSwitch> Switches, IReadOnlyList<PersistentDoor> Doors, IReadOnlyDictionary<string, string> InteractionState, IReadOnlyDictionary<string, string> TriggerState, IReadOnlyDictionary<string, string> AnimationContinuity)
{
    public object RecordsFor(string id) => id switch
    {
        "persistence.runtime" => new { RuntimeTick, DeterministicSeed, Continuation },
        "persistence.entities" => Entities.Select(x => x.Id).ToArray(),
        "persistence.components" => Entities.Select(x => new { x.Id, components = x.Components.OrderBy(y => y.Key, StringComparer.Ordinal) }).ToArray(),
        "persistence.resources" => Entities.Select(x => new { x.Id, resources = x.Resources.OrderBy(y => y.Key, StringComparer.Ordinal) }).ToArray(),
        "persistence.lifecycle" => Entities.Select(x => new { x.Id, x.Lifecycle }).ToArray(),
        "persistence.inventory" => Entities.Select(x => new { x.Id, x.Inventory }).ToArray(),
        "persistence.removed-entities" => RemovedEntities,
        "persistence.interaction-state" => InteractionState.OrderBy(x => x.Key, StringComparer.Ordinal),
        "persistence.trigger-state" => TriggerState.OrderBy(x => x.Key, StringComparer.Ordinal),
        "persistence.animation-continuity" => AnimationContinuity.OrderBy(x => x.Key, StringComparer.Ordinal),
        "persistence.flags" => Flags,
        "persistence.switches" => Switches,
        "persistence.doors" => Doors,
        _ => Array.Empty<object>()
    };
}
public sealed record ConditionEvidence(string Kind, bool Result, IReadOnlyList<ConditionEvidence> Children, string? Detail = null);
public abstract record PersistentCondition { public abstract ConditionEvidence Evaluate(PersistentWorldRuntime runtime); }
public sealed record FlagEqualsCondition(string FlagId, string Value) : PersistentCondition { public override ConditionEvidence Evaluate(PersistentWorldRuntime r) => new("flag-equals", r.Flags.TryGetValue(FlagId, out var x) && x.Value == Value, [], FlagId + "=" + Value); }
public sealed record InventoryContainsCondition(string EntityId, string ItemDefinitionId, int Quantity) : PersistentCondition { public override ConditionEvidence Evaluate(PersistentWorldRuntime r) { var n = r.Entities.TryGetValue(EntityId, out var x) ? x.Inventory.Where(y => y.ItemDefinitionId == ItemDefinitionId).Sum(y => y.Quantity) : 0; return new("inventory-contains", n >= Quantity, [], ItemDefinitionId + "=" + n); } }
public sealed record LifecycleEqualsCondition(string EntityId, string Value) : PersistentCondition { public override ConditionEvidence Evaluate(PersistentWorldRuntime r) => new("entity-lifecycle-equals", r.Entities.TryGetValue(EntityId, out var x) && x.Lifecycle == Value, [], EntityId + "=" + Value); }
public sealed record AllCondition(IReadOnlyList<PersistentCondition> Conditions) : PersistentCondition { public override ConditionEvidence Evaluate(PersistentWorldRuntime r) { var x = Conditions.Select(y => y.Evaluate(r)).ToArray(); return new("all", x.All(y => y.Result), x); } }
public sealed record AnyCondition(IReadOnlyList<PersistentCondition> Conditions) : PersistentCondition { public override ConditionEvidence Evaluate(PersistentWorldRuntime r) { var x = Conditions.Select(y => y.Evaluate(r)).ToArray(); return new("any", x.Any(y => y.Result), x); } }
public sealed record NotCondition(PersistentCondition Condition) : PersistentCondition { public override ConditionEvidence Evaluate(PersistentWorldRuntime r) { var x = Condition.Evaluate(r); return new("not", !x.Result, [x]); } }
public sealed record StateResolution(string IntentId, string Status, string? RejectionReason, string TransactionId, ConditionEvidence? Condition = null);

/// <summary>Explicit authoritative state only; no native resources, command buffers, render/sound state, caches, or paths.</summary>
public sealed class PersistentWorldRuntime
{
    private int ordinal; public int Tick { get; private set; }
    public string Seed { get; private set; } = "seed.m020"; public string Continuation { get; private set; } = "continuation.0";
    public SortedDictionary<string, PersistentEntity> Entities { get; } = new(StringComparer.Ordinal); public SortedSet<string> RemovedEntities { get; } = new(StringComparer.Ordinal); public SortedDictionary<string, PersistentFlag> Flags { get; } = new(StringComparer.Ordinal); public SortedDictionary<string, PersistentSwitch> Switches { get; } = new(StringComparer.Ordinal); public SortedDictionary<string, PersistentDoor> Doors { get; } = new(StringComparer.Ordinal); public SortedDictionary<string, string> InteractionState { get; } = new(StringComparer.Ordinal); public SortedDictionary<string, string> TriggerState { get; } = new(StringComparer.Ordinal); public SortedDictionary<string, string> AnimationContinuity { get; } = new(StringComparer.Ordinal);
    public List<PersistentWorldEvent> Events { get; } = []; public List<FlagTransition> FlagTransitions { get; } = []; public List<ProjectionInvalidation> Invalidations { get; } = []; public List<ConditionEvidence> ConditionEvaluations { get; } = [];
    public SortedDictionary<string, FlagDefinition> FlagDefinitions { get; } = new(StringComparer.Ordinal);
    public static PersistentWorldRuntime CreateInitial()
    {
        var r = new PersistentWorldRuntime(); r.Entities.Add(PersistentIds.Player, E(PersistentIds.Player, "active", new() { ["component.position"] = "before-door" }, [], new() { ["resource.health"] = 10 }));
        r.Entities.Add(PersistentIds.Crystal, E(PersistentIds.Crystal, "active", new() { ["world-item"] = "item.collectible-crystal" }, [], new())); r.Entities.Add(PersistentIds.Switch, E(PersistentIds.Switch, "active", new() { ["switch.state"] = "inactive" }, [], new())); r.Entities.Add(PersistentIds.Door, E(PersistentIds.Door, "active", new() { ["collision"] = "enabled", ["door.state"] = "locked" }, [], new()));
        r.RegisterFlag(new(PersistentIds.VaultPower, "boolean", []), "false"); r.RegisterFlag(new(PersistentIds.VaultAccess, "boolean", []), "false");
        r.Switches.Add(PersistentIds.Switch, new(PersistentIds.Switch, "inactive", PersistentIds.VaultPower, true)); r.Doors.Add(PersistentIds.Door, new(PersistentIds.Door, "locked", true, "condition.door.vault-access")); return r;
    }
    public void AdvanceTo(int tick) { if (tick < Tick) throw new ArgumentOutOfRangeException(nameof(tick)); Tick = tick; Continuation = "continuation." + tick; }
    public StateResolution CollectCrystal(string intent, string correlation)
    {
        var tx = "collection-transaction." + intent; if (!Entities.ContainsKey(PersistentIds.Crystal)) return new(intent, "rejected", "missing-world-item", tx); var p = Entities[PersistentIds.Player]; Entities[PersistentIds.Player] = p with { Inventory = p.Inventory.Append(new InventoryEntry("item.collectible-crystal", 1)).OrderBy(x => x.ItemDefinitionId, StringComparer.Ordinal).ToArray() }; Entities.Remove(PersistentIds.Crystal); RemovedEntities.Add(PersistentIds.Crystal); Emit("item.collected", tx, correlation, PersistentIds.Player, PersistentIds.Crystal); Emit("entity.removed", tx, correlation, PersistentIds.Player, PersistentIds.Crystal); return new(intent, "accepted", null, tx);
    }
    public StateResolution SetFlag(string id, string value, string tx, string correlation)
    {
        if (!Flags.TryGetValue(id, out var before)) return new(tx, "rejected", "unknown-flag", tx); if (!FlagDefinitions.TryGetValue(id, out var definition) || definition.Type != before.Type || !definition.Accepts(value)) return new(tx, "rejected", "invalid-flag-value", tx); if (before.Value == value) { FlagTransitions.Add(new("flag-transition." + (FlagTransitions.Count + 1).ToString("D4"), id, value, value, before.Revision, Tick, tx, true)); return new(tx, "accepted", null, tx); }
        var transition = "flag-transition." + (FlagTransitions.Count + 1).ToString("D4"); Flags[id] = before with { Value = value, Revision = before.Revision + 1, LastTransitionId = transition, RuntimeTick = Tick }; FlagTransitions.Add(new(transition, id, before.Value, value, before.Revision + 1, Tick, tx, false)); Emit("flag.changed", tx, correlation, id, id); return new(tx, "accepted", null, tx);
    }
    public void RegisterFlag(FlagDefinition definition, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(definition.Id) || definition.Type is not ("boolean" or "enum") || (definition.Type == "enum" && (definition.EnumValues.Count == 0 || definition.EnumValues.Distinct(StringComparer.Ordinal).Count() != definition.EnumValues.Count)) || !definition.Accepts(defaultValue))
            throw new ArgumentException("Flag definition must be boolean or a non-empty closed enum with a valid default.", nameof(definition));
        if (Flags.ContainsKey(definition.Id)) return;
        FlagDefinitions.Add(definition.Id, definition);
        Flags.Add(definition.Id, new PersistentFlag(definition.Id, definition.Type, defaultValue, 0, string.Empty, Tick));
    }

    public StateResolution ActivateSwitch(string intent, string correlation)
    {
        var tx = "switch-transaction." + intent; var s = Switches[PersistentIds.Switch]; if (s.State == "activated") return new(intent, "rejected", "already-activated", tx); if (!Flags.TryGetValue(s.FlagId, out var before) || before.Type != "boolean") return new(intent, "rejected", "invalid-flag-transition", tx); var transition = "flag-transition." + (FlagTransitions.Count + 1).ToString("D4"); Switches[s.EntityId] = s with { State = "activated" }; Flags[s.FlagId] = before with { Value = "true", Revision = before.Revision + 1, LastTransitionId = transition, RuntimeTick = Tick }; FlagTransitions.Add(new(transition, s.FlagId, before.Value, "true", before.Revision + 1, Tick, tx, false)); Emit("flag.changed", tx, correlation, s.EntityId, s.FlagId); Emit("switch.activated", tx, correlation, PersistentIds.Player, s.EntityId); return new(intent, "accepted", null, tx);
    }
    public StateResolution OpenDoor(string intent, string correlation)
    {
        var tx = "door-transaction." + intent; var door = Doors[PersistentIds.Door]; var proof = new AllCondition([new FlagEqualsCondition(PersistentIds.VaultPower, "true"), new InventoryContainsCondition(PersistentIds.Player, "item.collectible-crystal", 1)]).Evaluate(this); ConditionEvaluations.Add(proof); if (!proof.Result) return new(intent, "rejected", "condition-failed", tx, proof); if (door.State == "open") return new(intent, "rejected", "already-open", tx, proof); Doors[door.EntityId] = door with { State = "open", CollisionEnabled = false }; var e = Entities[door.EntityId]; Entities[door.EntityId] = e with { Components = new SortedDictionary<string, string>(e.Components.ToDictionary(x => x.Key, x => x.Value), StringComparer.Ordinal) { ["collision"] = "disabled", ["door.state"] = "open" } }; foreach (var x in new[] { "spatial", "interaction", "render" }) Invalidations.Add(new(x, door.EntityId, Tick, "door-opened")); Emit("door.unlocked", tx, correlation, PersistentIds.Player, door.EntityId); Emit("door.opened", tx, correlation, PersistentIds.Player, door.EntityId); return new(intent, "accepted", null, tx, proof);
    }
    public StateResolution MoveThroughDoor(string intent, string correlation) { var tx = "move-transaction." + intent; if (Doors[PersistentIds.Door].CollisionEnabled) return new(intent, "rejected", "door-collision-active", tx); var p = Entities[PersistentIds.Player]; Entities[PersistentIds.Player] = p with { Components = new SortedDictionary<string, string>(p.Components.ToDictionary(x => x.Key, x => x.Value), StringComparer.Ordinal) { ["component.position"] = "through-door" } }; Emit("player.moved", tx, correlation, PersistentIds.Player, PersistentIds.Door); return new(intent, "accepted", null, tx); }
    public PersistentWorldSnapshot Snapshot() => new(Tick, Seed, Continuation, Entities.Values.OrderBy(x => x.Id, StringComparer.Ordinal).Select(x => x with { Components = new SortedDictionary<string, string>(x.Components.ToDictionary(y => y.Key, y => y.Value), StringComparer.Ordinal), Inventory = x.Inventory.OrderBy(y => y.ItemDefinitionId, StringComparer.Ordinal).ToArray(), Resources = new SortedDictionary<string, int>(x.Resources.ToDictionary(y => y.Key, y => y.Value), StringComparer.Ordinal) }).ToArray(), RemovedEntities.ToArray(), Flags.Values.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray(), Switches.Values.OrderBy(x => x.EntityId, StringComparer.Ordinal).ToArray(), Doors.Values.OrderBy(x => x.EntityId, StringComparer.Ordinal).ToArray(), new SortedDictionary<string, string>(InteractionState, StringComparer.Ordinal), new SortedDictionary<string, string>(TriggerState, StringComparer.Ordinal), new SortedDictionary<string, string>(AnimationContinuity, StringComparer.Ordinal));
    internal static PersistentWorldRuntime From(PersistentWorldSnapshot s, IEnumerable<FlagDefinition>? definitions = null) { var r = new PersistentWorldRuntime { Tick = s.RuntimeTick, Seed = s.DeterministicSeed, Continuation = s.Continuation }; foreach (var x in definitions ?? s.Flags.Select(x => new FlagDefinition(x.Id, x.Type, x.Type == "enum" ? [x.Value] : []))) r.FlagDefinitions.TryAdd(x.Id, x); foreach (var x in s.Entities) r.Entities.Add(x.Id, x); foreach (var x in s.RemovedEntities) r.RemovedEntities.Add(x); foreach (var x in s.Flags) r.Flags.Add(x.Id, x); foreach (var x in s.Switches) r.Switches.Add(x.EntityId, x); foreach (var x in s.Doors) r.Doors.Add(x.EntityId, x); foreach (var x in s.InteractionState) r.InteractionState.Add(x.Key, x.Value); foreach (var x in s.TriggerState) r.TriggerState.Add(x.Key, x.Value); foreach (var x in s.AnimationContinuity) r.AnimationContinuity.Add(x.Key, x.Value); return r; }
    private static PersistentEntity E(string id, string life, Dictionary<string, string> c, IReadOnlyList<InventoryEntry> i, Dictionary<string, int> r) => new(id, life, new SortedDictionary<string, string>(c, StringComparer.Ordinal), i, new SortedDictionary<string, int>(r, StringComparer.Ordinal));
    private void Emit(string type, string tx, string correlation, string source, string target) => Events.Add(new("domain-event.m020." + (++ordinal).ToString("D4"), type, Tick, tx, correlation, source, target));
}
public sealed record SaveLoadResult(bool Success, PersistentWorldRuntime? Runtime, IReadOnlyList<string> Diagnostics, object? LoadPlan);
public sealed class CanonicalSaveService
{
    private readonly PersistenceContributorRegistry registry; public CanonicalSaveService(PersistenceContributorRegistry? registry = null) => this.registry = registry ?? new();
    public static SaveIdentity DefaultIdentity(string id) => new(id, PersistentIds.Project, CanonicalJson.Fingerprint(PersistentIds.Project), PersistentIds.World, CanonicalJson.Fingerprint(PersistentIds.World), CanonicalJson.Fingerprint(new { content = "m020" }));
    public SaveDocument Capture(PersistentWorldRuntime r, SaveIdentity identity) { var s = r.Snapshot(); return new(new(PersistentIds.Schema, 1, identity, s.RuntimeTick, s.DeterministicSeed, s.Continuation, registry.Describe(s), CanonicalJson.Fingerprint(s)), s); }
    public IReadOnlyList<string> Validate(SaveDocument save, SaveIdentity expected) { var d = new List<string>(); if (save.Manifest.Schema != PersistentIds.Schema || save.Manifest.SchemaVersion != 1) d.Add("SAVE0201: unsupported save schema/version"); if (save.Manifest.Identity.ProjectId != expected.ProjectId) d.Add("SAVE0202: project ID mismatch"); if (save.Manifest.Identity.ProjectFingerprint != expected.ProjectFingerprint) d.Add("SAVE0215: project fingerprint mismatch"); if (save.Manifest.Identity.WorldId != expected.WorldId) d.Add("SAVE0203: scenario/world ID mismatch"); if (save.Manifest.Identity.WorldFingerprint != expected.WorldFingerprint) d.Add("SAVE0216: world fingerprint mismatch"); if (save.Manifest.Identity.ContentFingerprint != expected.ContentFingerprint) d.Add("SAVE0204: content fingerprint mismatch"); if (save.Manifest.SnapshotFingerprint != CanonicalJson.Fingerprint(save.Snapshot)) d.Add("SAVE0208: snapshot fingerprint mismatch"); d.AddRange(registry.Validate(save.Snapshot, save.Manifest.Contributors)); return d.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(); }
    public SaveDocument Capture(PersistentWorldSnapshot snapshot, SaveIdentity identity) => new(new(PersistentIds.Schema, 1, identity, snapshot.RuntimeTick, snapshot.DeterministicSeed, snapshot.Continuation, registry.Describe(snapshot), CanonicalJson.Fingerprint(snapshot)), snapshot);
    public SaveLoadResult Load(SaveDocument save, SaveIdentity expected, IEnumerable<FlagDefinition>? flagDefinitions = null) { var d = Validate(save, expected).ToList(); if (flagDefinitions is not null) { var definitions = flagDefinitions.ToDictionary(x => x.Id, StringComparer.Ordinal); foreach (var flag in save.Snapshot.Flags) if (!definitions.TryGetValue(flag.Id, out var definition) || definition.Type != flag.Type || !definition.Accepts(flag.Value)) d.Add("SAVE0214: authored flag definition mismatch " + flag.Id); } if (d.Count != 0) return new(false, null, d.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(), null); var plan = registry.CreateLoadPlan(save.Snapshot, save.Manifest.Contributors); try { var fresh = registry.Apply(plan, save.Snapshot, flagDefinitions); if (fresh.Doors.Values.Any(x => x.State == "open" && x.CollisionEnabled)) return new(false, null, ["SAVE0213: open door must restore with collision disabled"], plan); return new(true, fresh, [], plan); } catch (Exception exception) { return new(false, null, ["SAVE0217: transactional reconstruction failed: " + exception.Message], plan); } }
}
