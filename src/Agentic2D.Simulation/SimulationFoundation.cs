using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic2D.Engine;

namespace Agentic2D.Simulation;

public readonly record struct WorldId(string Value) { public override string ToString() => Value; }
public readonly record struct RegionId(string Value) { public override string ToString() => Value; }
public readonly record struct ActivityId(string Value) { public override string ToString() => Value; }
public readonly record struct ReservationId(string Value) { public override string ToString() => Value; }
public readonly record struct SimulationCommandId(string Value) { public override string ToString() => Value; }
public readonly record struct SimulationEventId(string Value) { public override string ToString() => Value; }
public readonly record struct CorrelationId(string Value) { public override string ToString() => Value; }
public readonly record struct CausationId(string Value) { public override string ToString() => Value; }

public readonly record struct SimulationInstant(long Microseconds) : IComparable<SimulationInstant>
{
    public int CompareTo(SimulationInstant other) => Microseconds.CompareTo(other.Microseconds);
    public static SimulationInstant operator +(SimulationInstant value, SimulationDuration duration) => checked(new(value.Microseconds + duration.Microseconds));
    public override string ToString() => Microseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "us";
}

public readonly record struct SimulationDuration
{
    public SimulationDuration(long microseconds)
    {
        if (microseconds < 0) throw new ArgumentOutOfRangeException(nameof(microseconds));
        Microseconds = microseconds;
    }

    public long Microseconds { get; }
    public static SimulationDuration FromSeconds(long seconds) => new(checked(seconds * 1_000_000));
}

public enum PersistenceClassification { AuthoritativePersistent, DerivedRebuildable, ActiveModeTransient, PresentationOnly, ExternalHandle }
public enum SimulationEntityScope { RegionOwned, WorldScoped }
public enum SimulationLifecycle { Created, Active, Inactive, Destroyed }
public enum SimulationActivityStatus { Planned, Active, Interrupted, Cancelled, Completed, Failed }
public enum SimulationReservationStatus { Active, Released, Invalidated }

public sealed record SimulationComponentRegistration(string Key, int SchemaVersion, PersistenceClassification Classification, string Owner);
public sealed record SimulationRegion(string Id, string Name, bool Active = true);
public sealed record SimulationEntity(string Id, SimulationEntityScope Scope, string? RegionId, SimulationLifecycle Lifecycle, SortedDictionary<string, JsonElement> Components);
public sealed record SimulationActivity(string Id, string ActorEntityId, string Kind, string Stage, IReadOnlyList<string> Targets, SimulationInstant StartedAt, SimulationInstant LastTransitionAt, long Progress, int Revision, SimulationActivityStatus Status, string CorrelationId, string CausationId, string? Reason = null, string? CompletionResult = null);
public sealed record SimulationReservation(string Id, string ActivityId, string ReservingEntityId, string SubjectId, string Kind, int Quantity, SimulationInstant AcquiredAt, int SubjectRevision, int Revision, SimulationReservationStatus Status, string? ReleaseReason = null);
public sealed record SimulationReservationRequest(ReservationId Id, string SubjectId, string Kind, int Quantity, int SubjectCapacity);
public sealed record SimulationDiagnostic(string Code, string Severity, string Message, IReadOnlyList<string> RelatedIds);
public sealed record SimulationCommandResult(string CommandId, string Type, string Status, SimulationInstant IssuedAt, SimulationInstant CompletedAt, int? ExpectedRevision, int? CurrentRevision, IReadOnlyList<string> EventIds, IReadOnlyList<SimulationDiagnostic> Diagnostics);
public sealed record SimulationDomainEvent(string Id, string Type, SimulationInstant Instant, long Sequence, IReadOnlyList<string> AffectedIds, string CorrelationId, string CausationId, object Payload);
public sealed record SimulationChangeObservation(long Sequence, string EntityId, string? ComponentKey, string Kind);
public sealed record SimulationClock(SimulationInstant Now);
public sealed record SimulationSave(string Schema, int Version, string WorldId, long NowMicroseconds, long Sequence, IReadOnlyList<SimulationRegion> Regions, IReadOnlyList<SimulationEntity> Entities, IReadOnlyList<string> Tombstones, IReadOnlyList<SimulationActivity> Activities, IReadOnlyList<SimulationReservation> Reservations, string RegistrationFingerprint);
public sealed record SimulationLoadResult(bool Success, SimulationWorld? World, IReadOnlyList<SimulationDiagnostic> Diagnostics);

/// <summary>Optional first-class simulation capability. It composes the existing component world but owns simulation semantics.</summary>
public sealed class SimulationWorld
{
    public const string SaveSchema = "agentic2d.simulation-world-save.v1";
    private readonly EntityComponentWorld runtimeWorld = new();
    private readonly SortedDictionary<string, SimulationComponentRegistration> registrations = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, SimulationRegion> regions = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, SimulationActivity> activities = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, SimulationReservation> reservations = new(StringComparer.Ordinal);
    private readonly SortedSet<string> tombstones = new(StringComparer.Ordinal);
    private readonly List<SimulationDomainEvent> events = [];
    private readonly List<SimulationChangeObservation> observations = [];
    private long sequence;

    public SimulationWorld(WorldId id, SimulationInstant? initial = null) { Id = id; Clock = new(initial ?? new SimulationInstant(0)); runtimeWorld.Register<SimulationEntity>("component.simulation-entity", "simulation.foundation"); }
    public WorldId Id { get; }
    public SimulationClock Clock { get; private set; }
    public IReadOnlyList<SimulationDomainEvent> Events => events;
    public IReadOnlyList<SimulationChangeObservation> ChangeObservations => observations;
    public IReadOnlyList<SimulationEntity> Entities => runtimeWorld.Query<SimulationEntity>().Select(id => { runtimeWorld.TryGet<SimulationEntity>(id, out var entity); return entity!; }).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
    public IReadOnlyList<SimulationActivity> Activities => activities.Values.ToArray();
    public IReadOnlyList<SimulationReservation> Reservations => reservations.Values.ToArray();
    public IReadOnlyList<SimulationRegion> Regions => regions.Values.ToArray();
    public string RegistrationFingerprint => Fingerprint(registrations.Values.Select(x => new { x.Key, x.SchemaVersion, classification = x.Classification.ToString(), x.Owner }).ToArray());

    public void RegisterComponent(SimulationComponentRegistration registration)
    {
        if (string.IsNullOrWhiteSpace(registration.Key) || registration.SchemaVersion < 1) throw new ArgumentException("SIMCOMP0001: component registration requires stable key and schema version.");
        if (!registrations.TryAdd(registration.Key, registration)) throw new InvalidOperationException("SIMCOMP0002: duplicate component key " + registration.Key);
    }

    public SimulationCommandResult CreateRegion(RegionId id, string name) => Execute("region.create", null, () =>
    {
        if (string.IsNullOrWhiteSpace(id.Value) || !regions.TryAdd(id.Value, new(id.Value, name))) return Fail("SIMREGION0001", "duplicate or invalid region ID", id.Value);
        Emit("RegionCreated", [id.Value], new { id = id.Value, name }); return Success();
    });

    public SimulationCommandResult CreateEntity(string id, SimulationEntityScope scope, RegionId? region = null)
        => Execute("entity.create", null, () =>
        {
            if (string.IsNullOrWhiteSpace(id) || runtimeWorld.Exists(id) || tombstones.Contains(id)) return Fail("SIMENTITY0001", "duplicate, destroyed, or invalid entity ID", id);
            if (scope == SimulationEntityScope.RegionOwned && (region is null || !regions.ContainsKey(region.Value.Value))) return Fail("SIMREGION0002", "region-owned entity requires an existing region", id);
            if (scope == SimulationEntityScope.WorldScoped && region is not null) return Fail("SIMREGION0003", "world-scoped entity cannot have a region", id);
            var entity = new SimulationEntity(id, scope, region is null ? null : region.Value.Value, SimulationLifecycle.Created, new(StringComparer.Ordinal));
            runtimeWorld.CreateEntity(id); runtimeWorld.Set(id, entity); observations.Add(new(++sequence, id, null, "created")); Emit("EntityCreated", [id], new { id, scope = scope.ToString(), region = entity.RegionId }); return Success();
        });

    /// <summary>Atomically creates an entity with its first persistent component.</summary>
    public SimulationCommandResult CreateEntityWithComponent(string id, SimulationEntityScope scope, RegionId? region, string componentKey, JsonElement value, string? semanticEventType = null, object? semanticPayload = null)
        => Execute("entity.create-with-component", null, () =>
        {
            if (string.IsNullOrWhiteSpace(id) || runtimeWorld.Exists(id) || tombstones.Contains(id)) return Fail("SIMENTITY0001", "duplicate, destroyed, or invalid entity ID", id);
            if (scope == SimulationEntityScope.RegionOwned && (region is null || !regions.ContainsKey(region.Value.Value))) return Fail("SIMREGION0002", "region-owned entity requires an existing region", id);
            if (scope == SimulationEntityScope.WorldScoped && region is not null) return Fail("SIMREGION0003", "world-scoped entity cannot have a region", id);
            if (!registrations.ContainsKey(componentKey)) return Fail("SIMCOMP0003", "unknown component key", componentKey);
            var entity = new SimulationEntity(id, scope, region is null ? null : region.Value.Value, SimulationLifecycle.Created, new(StringComparer.Ordinal) { [componentKey] = value.Clone() });
            runtimeWorld.CreateEntity(id); runtimeWorld.Set(id, entity); observations.Add(new(++sequence, id, componentKey, "created-with-component")); Emit("EntityCreated", [id], new { id, scope = scope.ToString(), region = entity.RegionId, componentKey });
            if (!string.IsNullOrWhiteSpace(semanticEventType)) Emit(semanticEventType, [id], semanticPayload ?? new { id });
            return Success();
        });

    public SimulationCommandResult RejectCommand(string type, string diagnosticCode, string message, IReadOnlyList<string> relatedIds) => Execute(type, null, () => Fail(diagnosticCode, message, string.Join(",", relatedIds.Order(StringComparer.Ordinal))));

    public SimulationCommandResult ActivateEntity(string id) => TransitionLifecycle(id, SimulationLifecycle.Created, SimulationLifecycle.Active, "EntityActivated");
    public SimulationCommandResult DeactivateEntity(string id) => TransitionLifecycle(id, SimulationLifecycle.Active, SimulationLifecycle.Inactive, "EntityDeactivated");
    public SimulationCommandResult DestroyEntity(string id) => Execute("entity.destroy", null, () =>
    {
        if (!TryEntity(id, out var entity) || entity.Lifecycle == SimulationLifecycle.Destroyed) return Fail("SIMENTITY0002", "entity not found", id);
        var held = reservations.Values.Where(x => x.Status == SimulationReservationStatus.Active && (x.SubjectId == id || x.ReservingEntityId == id)).ToArray();
        foreach (var reservation in held) reservations[reservation.Id] = reservation with { Status = SimulationReservationStatus.Invalidated, ReleaseReason = "subject-destroyed", Revision = reservation.Revision + 1 };
        tombstones.Add(id); runtimeWorld.DestroyEntity(id); observations.Add(new(++sequence, id, null, "destroyed")); Emit("EntityDestroyed", [id], new { id, invalidatedReservations = held.Select(x => x.Id).ToArray() }); return Success();
    });

    public SimulationCommandResult TransferRegion(string entityId, RegionId destination) => Execute("entity.transfer-region", null, () =>
    {
        if (!TryEntity(entityId, out var entity) || entity.Scope != SimulationEntityScope.RegionOwned || entity.Lifecycle is SimulationLifecycle.Destroyed) return Fail("SIMREGION0004", "entity is not an active region-owned entity", entityId);
        if (!regions.ContainsKey(destination.Value)) return Fail("SIMREGION0005", "destination region is unknown", destination.Value);
        if (entity.RegionId == destination.Value) return Fail("SIMREGION0006", "entity already belongs to destination", entityId);
        Put(entity with { RegionId = destination.Value }); observations.Add(new(++sequence, entityId, null, "region-transferred")); Emit("EntityTransferredRegion", [entityId, destination.Value], new { entityId, from = entity.RegionId, to = destination.Value }); return Success();
    });

    public SimulationCommandResult SetComponent(string entityId, string key, JsonElement value) => Execute("component.set", null, () =>
    {
        if (!TryEntity(entityId, out var entity) || entity.Lifecycle == SimulationLifecycle.Destroyed) return Fail("SIMENTITY0002", "entity not found", entityId);
        if (!registrations.ContainsKey(key)) return Fail("SIMCOMP0003", "unknown component key", key);
        var components = new SortedDictionary<string, JsonElement>(entity.Components, StringComparer.Ordinal) { [key] = value.Clone() };
        Put(entity with { Components = components }); observations.Add(new(++sequence, entityId, key, "component-changed")); return Success();
    });

    public IReadOnlyList<SimulationEntity> QueryRegion(RegionId region, bool includeWorldScoped = false) => Entities.Where(x => x.Lifecycle == SimulationLifecycle.Active && (x.RegionId == region.Value || (includeWorldScoped && x.Scope == SimulationEntityScope.WorldScoped))).ToArray();
    public void Advance(SimulationDuration duration) => Clock = new(Clock.Now + duration);

    public SimulationCommandResult CreateActivity(ActivityId id, string actor, string kind, string initialStage, IReadOnlyList<string> targets, CorrelationId correlation, CausationId causation) => Execute("activity.create", null, () =>
    {
        if (activities.ContainsKey(id.Value) || !IsActive(actor) || targets.Any(target => !runtimeWorld.Exists(target))) return Fail("SIMACTIVITY0001", "invalid or duplicate activity", id.Value);
        activities.Add(id.Value, new(id.Value, actor, kind, initialStage, targets.Order(StringComparer.Ordinal).ToArray(), Clock.Now, Clock.Now, 0, 1, SimulationActivityStatus.Planned, correlation.Value, causation.Value)); Emit("ActivityCreated", [id.Value, actor], new { id = id.Value, kind, stage = initialStage }); return Success();
    });

    /// <summary>
    /// Atomically starts an activity and acquires all of its initial reservations.  Domain
    /// coordinators must use this instead of separately creating an activity and claiming its
    /// contested targets, so a failed reservation cannot leave an assignable activity behind.
    /// </summary>
    public SimulationCommandResult CreateActivityWithReservations(ActivityId id, string actor, string kind, string initialStage, IReadOnlyList<string> targets, IReadOnlyList<SimulationReservationRequest> requests, CorrelationId correlation, CausationId causation) => Execute("activity.start-with-reservations", null, () =>
    {
        if (activities.ContainsKey(id.Value) || !IsActive(actor) || targets.Any(target => !runtimeWorld.Exists(target)) || requests.Count == 0 || requests.GroupBy(x => x.Id.Value, StringComparer.Ordinal).Any(x => x.Count() != 1)) return Fail("SIMACTIVITY0001", "invalid activity assignment request", id.Value);
        foreach (var request in requests.OrderBy(x => x.Id.Value, StringComparer.Ordinal))
        {
            if (request.Quantity <= 0 || request.SubjectCapacity <= 0 || reservations.ContainsKey(request.Id.Value) || !TryEntity(request.SubjectId, out var subject) || subject.Lifecycle == SimulationLifecycle.Destroyed) return Fail("SIMRESERVE0001", "invalid assignment reservation", request.Id.Value);
            var held = reservations.Values.Where(x => x.SubjectId == request.SubjectId && x.Kind == request.Kind && x.Status == SimulationReservationStatus.Active).Sum(x => x.Quantity);
            if (checked(held + request.Quantity) > request.SubjectCapacity) return Fail("SIMRESERVE0003", "assignment reservation capacity conflict", request.SubjectId);
        }
        var activity = new SimulationActivity(id.Value, actor, kind, initialStage, targets.Order(StringComparer.Ordinal).ToArray(), Clock.Now, Clock.Now, 0, 1, SimulationActivityStatus.Planned, correlation.Value, causation.Value);
        activities.Add(id.Value, activity);
        foreach (var request in requests.OrderBy(x => x.Id.Value, StringComparer.Ordinal))
        {
            reservations.Add(request.Id.Value, new(request.Id.Value, id.Value, actor, request.SubjectId, request.Kind, request.Quantity, Clock.Now, 1, 1, SimulationReservationStatus.Active));
            Emit("ReservationAcquired", [request.Id.Value, id.Value, request.SubjectId], new { id = request.Id.Value, subject = request.SubjectId, kind = request.Kind, quantity = request.Quantity });
        }
        Emit("ActivityStarted", [id.Value, actor], new { id = id.Value, kind, stage = initialStage, reservationCount = requests.Count });
        return Success();
    });

    public SimulationCommandResult TransitionActivity(ActivityId id, int expectedRevision, string stage, SimulationActivityStatus status, long? progress = null, string? reason = null) => Execute("activity.transition", expectedRevision, () =>
    {
        if (!activities.TryGetValue(id.Value, out var activity)) return Fail("SIMACTIVITY0002", "unknown activity", id.Value);
        if (activity.Revision != expectedRevision) return Fail("SIMACTIVITY0003", "stale activity revision", id.Value, activity.Revision);
        if (activity.Status is SimulationActivityStatus.Completed or SimulationActivityStatus.Cancelled or SimulationActivityStatus.Failed) return Fail("SIMACTIVITY0004", "activity is terminal", id.Value, activity.Revision);
        if (status == SimulationActivityStatus.Completed && reservations.Values.Any(x => x.ActivityId == id.Value && x.Status == SimulationReservationStatus.Active)) return Fail("SIMACTIVITY0005", "completed activity retains reservation", id.Value, activity.Revision);
        activities[id.Value] = activity with { Stage = stage, Status = status, Progress = progress ?? activity.Progress, Revision = activity.Revision + 1, LastTransitionAt = Clock.Now, Reason = reason, CompletionResult = status == SimulationActivityStatus.Completed ? "completed" : activity.CompletionResult };
        Emit(status == SimulationActivityStatus.Completed ? "ActivityCompleted" : "ActivityStageChanged", [id.Value], new { id = id.Value, stage, status = status.ToString() }); return Success();
    });

    public SimulationCommandResult AcquireReservation(ReservationId id, ActivityId activity, string subject, string kind, int quantity, int subjectCapacity, int expectedActivityRevision) => Execute("reservation.acquire", expectedActivityRevision, () =>
    {
        if (!activities.TryGetValue(activity.Value, out var owner)) return Fail("SIMRESERVE0001", "invalid reservation request", id.Value);
        if (quantity <= 0 || reservations.ContainsKey(id.Value) || owner.Revision != expectedActivityRevision || owner.Status is SimulationActivityStatus.Completed or SimulationActivityStatus.Cancelled) return Fail("SIMRESERVE0001", "invalid reservation request", id.Value, owner.Revision);
        if (!TryEntity(subject, out var target) || target.Lifecycle == SimulationLifecycle.Destroyed) return Fail("SIMRESERVE0002", "reservation subject missing", subject);
        var occupied = reservations.Values.Where(x => x.SubjectId == subject && x.Kind == kind && x.Status == SimulationReservationStatus.Active).Sum(x => x.Quantity);
        if (checked(occupied + quantity) > subjectCapacity) return Fail("SIMRESERVE0003", "reservation capacity conflict", subject);
        reservations.Add(id.Value, new(id.Value, activity.Value, owner.ActorEntityId, subject, kind, quantity, Clock.Now, 1, 1, SimulationReservationStatus.Active)); Emit("ReservationAcquired", [id.Value, activity.Value, subject], new { id = id.Value, subject, kind, quantity }); return Success();
    });

    public SimulationCommandResult ReleaseReservation(ReservationId id, string reason) => Execute("reservation.release", null, () =>
    {
        if (!reservations.TryGetValue(id.Value, out var reservation)) return Fail("SIMRESERVE0004", "unknown reservation", id.Value);
        if (reservation.Status != SimulationReservationStatus.Active) return Success(); // explicitly idempotent
        reservations[id.Value] = reservation with { Status = SimulationReservationStatus.Released, ReleaseReason = reason, Revision = reservation.Revision + 1 }; Emit("ReservationReleased", [id.Value, reservation.ActivityId], new { id = id.Value, reason }); return Success();
    });

    public SimulationCommandResult RecordFact(string type, IReadOnlyList<string> affected, object payload) => Execute("domain.fact." + type, null, () => { Emit(type, affected, payload); return Success(); });

    public SimulationSave Capture() => new(SaveSchema, 1, Id.Value, Clock.Now.Microseconds, sequence, Regions, Entities.Where(x => x.Lifecycle != SimulationLifecycle.Destroyed).Select(CloneEntity).ToArray(), tombstones.ToArray(), Activities, Reservations, RegistrationFingerprint);
    public string CanonicalJson() => JsonSerializer.Serialize(Capture(), JsonOptions);
    public string Fingerprint() => Fingerprint(Capture());

    public static SimulationLoadResult Load(SimulationSave save, IEnumerable<SimulationComponentRegistration> componentRegistrations)
    {
        var diagnostics = new List<SimulationDiagnostic>();
        if (save.Schema != SaveSchema || save.Version != 1) diagnostics.Add(Diagnostic("SIMPERSIST0001", "unsupported save schema/version"));
        var registrations = componentRegistrations.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray();
        if (registrations.GroupBy(x => x.Key, StringComparer.Ordinal).Any(x => x.Count() != 1)) diagnostics.Add(Diagnostic("SIMCOMP0002", "duplicate component key"));
        var keys = registrations.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        if (save.Entities.SelectMany(x => x.Components.Keys).Any(key => !keys.Contains(key))) diagnostics.Add(Diagnostic("SIMPERSIST0002", "unknown persisted component key"));
        if (save.Entities.GroupBy(x => x.Id, StringComparer.Ordinal).Any(x => x.Count() != 1) || save.Regions.GroupBy(x => x.Id, StringComparer.Ordinal).Any(x => x.Count() != 1)) diagnostics.Add(Diagnostic("SIMPERSIST0003", "duplicate persisted identity"));
        if (save.Entities.Any(x => x.Scope == SimulationEntityScope.RegionOwned && (x.RegionId is null || !save.Regions.Any(r => r.Id == x.RegionId)))) diagnostics.Add(Diagnostic("SIMPERSIST0004", "invalid region ownership"));
        if (save.Activities.Any(x => !save.Entities.Any(e => e.Id == x.ActorEntityId)) || save.Reservations.Any(x => !save.Activities.Any(a => a.Id == x.ActivityId))) diagnostics.Add(Diagnostic("SIMPERSIST0005", "broken activity/reservation reference"));
        if (diagnostics.Count != 0) return new(false, null, diagnostics);
        try
        {
            var world = new SimulationWorld(new(save.WorldId), new(save.NowMicroseconds)); foreach (var registration in registrations) world.RegisterComponent(registration);
            if (world.RegistrationFingerprint != save.RegistrationFingerprint) return new(false, null, [Diagnostic("SIMPERSIST0006", "registration fingerprint mismatch")]);
            foreach (var region in save.Regions) world.regions.Add(region.Id, region);
            foreach (var entity in save.Entities) { world.runtimeWorld.CreateEntity(entity.Id); world.runtimeWorld.Set(entity.Id, CloneEntity(entity)); }
            foreach (var id in save.Tombstones) world.tombstones.Add(id); foreach (var activity in save.Activities) world.activities.Add(activity.Id, activity); foreach (var reservation in save.Reservations) world.reservations.Add(reservation.Id, reservation); world.sequence = save.Sequence;
            return new(true, world, []);
        }
        catch (Exception exception) { return new(false, null, [Diagnostic("SIMPERSIST0007", "transactional load rejected: " + exception.Message)]); }
    }

    private SimulationCommandResult TransitionLifecycle(string id, SimulationLifecycle from, SimulationLifecycle to, string eventType) => Execute("entity.lifecycle", null, () =>
    {
        if (!TryEntity(id, out var entity) || entity.Lifecycle != from) return Fail("SIMENTITY0003", "invalid lifecycle transition", id);
        Put(entity with { Lifecycle = to }); observations.Add(new(++sequence, id, null, to.ToString().ToLowerInvariant())); Emit(eventType, [id], new { id, lifecycle = to.ToString() }); return Success();
    });
    private bool IsActive(string id) => TryEntity(id, out var entity) && entity.Lifecycle == SimulationLifecycle.Active;
    private bool TryEntity(string id, out SimulationEntity entity) { var found = runtimeWorld.TryGet<SimulationEntity>(id, out var value); entity = value!; return found; }
    private void Put(SimulationEntity entity) { if (!runtimeWorld.Set(entity.Id, entity).Accepted) throw new InvalidOperationException("Simulation entity mutation rejected: " + entity.Id); }
    private SimulationCommandResult Execute(string type, int? expectedRevision, Func<SimulationCommandResult> action)
    {
        var commandId = "command." + (++sequence).ToString("D4", System.Globalization.CultureInfo.InvariantCulture); var result = action();
        return result with { CommandId = commandId, Type = type, IssuedAt = Clock.Now, CompletedAt = Clock.Now, ExpectedRevision = expectedRevision };
    }
    private SimulationCommandResult Success() => new("", "", "accepted", Clock.Now, Clock.Now, null, null, [], []);
    private SimulationCommandResult Fail(string code, string message, string id, int? current = null) => new("", "", "rejected", Clock.Now, Clock.Now, null, current, [], [Diagnostic(code, message, id)]);
    private void Emit(string type, IReadOnlyList<string> affected, object payload) => events.Add(new("event." + (++sequence).ToString("D4", System.Globalization.CultureInfo.InvariantCulture), type, Clock.Now, sequence, affected.Order(StringComparer.Ordinal).ToArray(), "correlation.m031", "causation.m031", payload));
    private static SimulationDiagnostic Diagnostic(string code, string message, params string[] ids) => new(code, "error", message, ids);
    private static SimulationEntity CloneEntity(SimulationEntity entity) => entity with { Components = new(entity.Components.ToDictionary(x => x.Key, x => x.Value.Clone(), StringComparer.Ordinal), StringComparer.Ordinal) };
    internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, Converters = { new JsonStringEnumConverter() } };
    internal static string Fingerprint(object value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions)))).ToLowerInvariant();
}

public static class SimulationFoundationComposition
{
    public static SimulationWorld AddSimulationFoundation(WorldId id, SimulationInstant initial) => new(id, initial);
    public static IReadOnlyList<SimulationComponentRegistration> AddM031WoodWorkflowProofComponents() =>
    [
        new("component.m031.inventory", 1, PersistenceClassification.AuthoritativePersistent, "m031.wood-proof"),
        new("component.m031.harvestable", 1, PersistenceClassification.AuthoritativePersistent, "m031.wood-proof"),
        new("component.m031.storage", 1, PersistenceClassification.AuthoritativePersistent, "m031.wood-proof"),
        new("component.m031.path-preview", 1, PersistenceClassification.ActiveModeTransient, "m031.wood-proof")
    ];
}
