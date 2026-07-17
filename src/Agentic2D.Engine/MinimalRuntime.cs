using Agentic2D.Contracts;
using Agentic2D.Metrics;

namespace Agentic2D.Engine;

public sealed class MinimalRuntime
{
    private readonly Dictionary<EntityId, int> entityPositions = [];
    private readonly List<RuntimeEvent> events = [];
    private readonly List<Diagnostic> diagnostics = [];

    private Tick currentTick = Tick.Zero;
    private bool hasStarted;
    private bool hasCompleted;

    public MinimalRuntime(MetricsCollectionMode metricsMode = MetricsCollectionMode.Off)
    {
        Metrics = metricsMode == MetricsCollectionMode.Off ? null : new RuntimeMetrics(metricsMode);
    }

    public Tick CurrentTick => currentTick;

    public IReadOnlyList<RuntimeEvent> Events => events;

    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

    /// <summary>Observational metrics only; null in off mode to avoid hot-path work.</summary>
    public RuntimeMetrics? Metrics { get; }

    public void CreateEntity(EntityId entityId, int position)
    {
        entityPositions[entityId] = position;
        Emit(new RuntimeEvent("entity.created", currentTick.Value, $"{entityId} created at {position}"));
    }

    public RuntimeCommandResult Submit(MoveCommand command)
    {
        const string commandId = "command.move";
        Metrics?.Increment(RuntimeMetricId.RuntimeCommandsSubmitted);

        if (hasCompleted)
        {
            var diagnostic = new Diagnostic("error", "runtime.completed", "runtime cannot accept commands after completion");
            diagnostics.Add(diagnostic);
            Metrics?.Increment(RuntimeMetricId.RuntimeCommandsRejected);

            return new RuntimeCommandResult(commandId, "rejected", diagnostic.Message);
        }

        if (!entityPositions.ContainsKey(command.EntityId))
        {
            var diagnostic = new Diagnostic("error", "runtime.entityNotFound", $"{command.EntityId} does not exist");
            diagnostics.Add(diagnostic);
            Metrics?.Increment(RuntimeMetricId.RuntimeCommandsRejected);

            return new RuntimeCommandResult(commandId, "rejected", diagnostic.Message);
        }

        if (command.Amount == 0)
        {
            var diagnostic = new Diagnostic("error", "runtime.invalidMoveAmount", "move amount must be non-zero");
            diagnostics.Add(diagnostic);
            Metrics?.Increment(RuntimeMetricId.RuntimeCommandsRejected);

            return new RuntimeCommandResult(commandId, "rejected", diagnostic.Message);
        }

        Emit(new RuntimeEvent("command.accepted", currentTick.Value, $"{commandId} accepted for {command.EntityId}"));
        Metrics?.Increment(RuntimeMetricId.RuntimeCommandsAccepted);

        return new RuntimeCommandResult(commandId, "accepted", "move command accepted");
    }

    public void Run(int ticksRequested, MoveCommand acceptedCommand)
    {
        if (ticksRequested <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticksRequested), ticksRequested, "Tick count must be positive.");
        }

        Start();

        for (var tick = 1; tick <= ticksRequested; tick++)
        {
            currentTick = new Tick(tick);
            Metrics?.BeginTick(tick);
            Metrics?.SetGauge(RuntimeMetricId.RuntimeEntitiesActive, entityPositions.Count);

            using (Metrics?.Measure(RuntimeMetricId.RuntimePhaseBehaviorDuration) ?? default)
            {
                // Behavior is intentionally empty in this minimal deterministic slice.
            }
            using (Metrics?.Measure(RuntimeMetricId.RuntimePhaseSpatialDuration) ?? default)
            {
                // No spatial query is required by this scenario.
            }
            using (Metrics?.Measure(RuntimeMetricId.RuntimePhaseMutationDuration) ?? default)
            {
                if (tick == 1) Apply(acceptedCommand);
            }
            using (Metrics?.Measure(RuntimeMetricId.RuntimePhasePresentationDuration) ?? default) { }
            using (Metrics?.Measure(RuntimeMetricId.RuntimePhaseRenderProjectionDuration) ?? default) { }
            Metrics?.EndTick();
        }

        Complete();
    }

    public int? TryGetEntityPosition(EntityId entityId)
    {
        return entityPositions.TryGetValue(entityId, out var position) ? position : null;
    }

    public IReadOnlyList<EntitySummary> QueryEntities()
    {
        return entityPositions
            .OrderBy(static entity => entity.Key.Value, StringComparer.Ordinal)
            .Select(static entity => new EntitySummary(entity.Key.Value, entity.Value))
            .ToArray();
    }

    private void Start()
    {
        if (hasStarted)
        {
            return;
        }

        hasStarted = true;
        events.Insert(0, new RuntimeEvent("runtime.started", Tick.Zero.Value, "runtime started"));
        Metrics?.Increment(RuntimeMetricId.RuntimeEventsEmitted);
    }

    private void Apply(MoveCommand command)
    {
        var previous = entityPositions[command.EntityId];
        var next = checked(previous + command.Amount);

        entityPositions[command.EntityId] = next;
        Emit(new RuntimeEvent("entity.moved", currentTick.Value, $"{command.EntityId} moved from {previous} to {next}"));
    }

    private void Complete()
    {
        hasCompleted = true;
        Emit(new RuntimeEvent("runtime.completed", currentTick.Value, "runtime completed"));
    }

    private void Emit(RuntimeEvent runtimeEvent)
    {
        events.Add(runtimeEvent);
        Metrics?.Increment(RuntimeMetricId.RuntimeEventsEmitted);
    }
}
