using Agentic2D.Contracts;

namespace Agentic2D.Engine;

public sealed class MinimalRuntime
{
    private readonly Dictionary<EntityId, int> entityPositions = [];
    private readonly List<RuntimeEvent> events = [];
    private readonly List<Diagnostic> diagnostics = [];

    private Tick currentTick = Tick.Zero;
    private bool hasStarted;
    private bool hasCompleted;

    public Tick CurrentTick => currentTick;

    public IReadOnlyList<RuntimeEvent> Events => events;

    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

    public void CreateEntity(EntityId entityId, int position)
    {
        entityPositions[entityId] = position;
        events.Add(new RuntimeEvent("entity.created", currentTick.Value, $"{entityId} created at {position}"));
    }

    public RuntimeCommandResult Submit(MoveCommand command)
    {
        const string commandId = "command.move";

        if (hasCompleted)
        {
            var diagnostic = new Diagnostic("error", "runtime.completed", "runtime cannot accept commands after completion");
            diagnostics.Add(diagnostic);

            return new RuntimeCommandResult(commandId, "rejected", diagnostic.Message);
        }

        if (!entityPositions.ContainsKey(command.EntityId))
        {
            var diagnostic = new Diagnostic("error", "runtime.entityNotFound", $"{command.EntityId} does not exist");
            diagnostics.Add(diagnostic);

            return new RuntimeCommandResult(commandId, "rejected", diagnostic.Message);
        }

        if (command.Amount == 0)
        {
            var diagnostic = new Diagnostic("error", "runtime.invalidMoveAmount", "move amount must be non-zero");
            diagnostics.Add(diagnostic);

            return new RuntimeCommandResult(commandId, "rejected", diagnostic.Message);
        }

        events.Add(new RuntimeEvent("command.accepted", currentTick.Value, $"{commandId} accepted for {command.EntityId}"));

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

            if (tick == 1)
            {
                Apply(acceptedCommand);
            }
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
    }

    private void Apply(MoveCommand command)
    {
        var previous = entityPositions[command.EntityId];
        var next = checked(previous + command.Amount);

        entityPositions[command.EntityId] = next;
        events.Add(new RuntimeEvent("entity.moved", currentTick.Value, $"{command.EntityId} moved from {previous} to {next}"));
    }

    private void Complete()
    {
        hasCompleted = true;
        events.Add(new RuntimeEvent("runtime.completed", currentTick.Value, "runtime completed"));
    }
}
