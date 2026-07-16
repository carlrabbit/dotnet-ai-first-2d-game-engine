namespace Agentic2D.Gameplay;

public static class GameplayIds
{
    public const string Health = "resource.health";
    public const string Active = "active";
    public const string Defeated = "defeated";
    public const string Inactive = "inactive";
}

public sealed record ResourceHealth(string ResourceTypeId, int Current, int Minimum, int Maximum, int Revision)
{
    public bool IsValid => ResourceTypeId == GameplayIds.Health && Minimum <= Current && Current <= Maximum && Maximum > Minimum && Revision >= 0;
    public ResourceHealth ApplyDamage(int amount) => this with { Current = Math.Max(Minimum, Current - amount), Revision = Revision + 1 };
}

public sealed record DamageIntent(string IntentId, string SourceId, string TargetEntityId, string DamageKindId, int RequestedAmount, int RuntimeTick, string CorrelationId, string Provenance);
public sealed record DamageResolution(string IntentId, string CorrelationId, string Status, string? RejectionReason, int RequestedAmount, int AppliedAmount, int? PreviousHealth, int? ResultingHealth, string? LifecycleBefore, string? LifecycleAfter, string TransactionId);
public sealed record DomainEvent(string EventId, string Type, int RuntimeTick, string SourceId, string TargetId, string CorrelationId, string TransactionId, object Payload, string Provenance);
public sealed record ResourceTransition(string EntityId, ResourceHealth Before, ResourceHealth After, int RuntimeTick, string TransactionId, string CorrelationId);
public sealed record LifecycleTransition(string EntityId, string Before, string After, int RuntimeTick, string TransactionId, string CorrelationId);

public sealed class GameplayEntity
{
    public GameplayEntity(string id, ResourceHealth? health = null, string lifecycle = GameplayIds.Active)
    {
        Id = id;
        Health = health;
        Lifecycle = lifecycle;
    }

    public string Id { get; }
    public ResourceHealth? Health { get; internal set; }
    public string Lifecycle { get; internal set; }
    public bool NormalBehaviorEnabled => Lifecycle == GameplayIds.Active;
}

/// <summary>Runtime-owned authoritative state with explicit validated damage transactions.</summary>
public sealed class GameplayWorld
{
    private readonly Dictionary<string, GameplayEntity> entities = new(StringComparer.Ordinal);
    private readonly HashSet<string> damageCorrelations = new(StringComparer.Ordinal);
    private int eventOrdinal;

    public List<DomainEvent> Events { get; } = [];
    public List<ResourceTransition> ResourceTransitions { get; } = [];
    public List<LifecycleTransition> LifecycleTransitions { get; } = [];
    public IReadOnlyDictionary<string, GameplayEntity> Entities => entities;

    public void Add(GameplayEntity entity) => entities.Add(entity.Id, entity);

    public DamageResolution ApplyDamage(DamageIntent intent)
    {
        var transaction = "damage-transaction." + intent.IntentId;
        if (!IsDamageKind(intent.DamageKindId)) return Reject("invalid-damage-kind");
        if (intent.RequestedAmount <= 0) return Reject("non-positive-damage");
        if (string.IsNullOrWhiteSpace(intent.CorrelationId) || !damageCorrelations.Add(intent.CorrelationId)) return Reject("duplicate-correlation");
        if (!entities.TryGetValue(intent.TargetEntityId, out var entity)) return Reject("missing-target");
        if (entity.Health is null) return Reject("target-without-health");
        if (entity.Lifecycle != GameplayIds.Active) return Reject(entity.Lifecycle == GameplayIds.Defeated ? "already-defeated" : "invalid-lifecycle");
        if (!entity.Health.IsValid) return Reject("invalid-health-resource");

        var before = entity.Health;
        var applied = Math.Min(intent.RequestedAmount, before.Current - before.Minimum);
        var after = before.ApplyDamage(applied);
        entity.Health = after;
        var resolution = new DamageResolution(intent.IntentId, intent.CorrelationId, "accepted", null, intent.RequestedAmount, applied, before.Current, after.Current, entity.Lifecycle, after.Current == after.Minimum ? GameplayIds.Defeated : entity.Lifecycle, transaction);
        ResourceTransitions.Add(new(entity.Id, before, after, intent.RuntimeTick, transaction, intent.CorrelationId));
        Emit("resource.changed", intent.RuntimeTick, intent.SourceId, entity.Id, intent.CorrelationId, transaction, new { resourceTypeId = GameplayIds.Health, before = before.Current, after = after.Current }, intent.Provenance);
        Emit("entity.damaged", intent.RuntimeTick, intent.SourceId, entity.Id, intent.CorrelationId, transaction, new { requested = intent.RequestedAmount, applied }, intent.Provenance);
        if (after.Current == after.Minimum)
        {
            var lifecycleBefore = entity.Lifecycle;
            entity.Lifecycle = GameplayIds.Defeated;
            LifecycleTransitions.Add(new(entity.Id, lifecycleBefore, entity.Lifecycle, intent.RuntimeTick, transaction, intent.CorrelationId));
            Emit("entity.defeated", intent.RuntimeTick, intent.SourceId, entity.Id, intent.CorrelationId, transaction, new { lifecycle = entity.Lifecycle }, intent.Provenance);
        }
        return resolution;

        DamageResolution Reject(string reason) => new(intent.IntentId, intent.CorrelationId, "rejected", reason, intent.RequestedAmount, 0, null, null, null, null, transaction);
    }

    private void Emit(string type, int tick, string source, string target, string correlation, string transaction, object payload, string provenance) => Events.Add(new("domain-event." + (++eventOrdinal).ToString("D4"), type, tick, source, target, correlation, transaction, payload, provenance));
    private static bool IsDamageKind(string kind) => kind is "damage.generic" or "damage.environment";
}
