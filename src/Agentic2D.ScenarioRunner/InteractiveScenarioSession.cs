using System.Text.Json;
using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.Validation;

namespace Agentic2D.ScenarioRunner;

/// <summary>Runtime-owned, deterministic presentation session. It exposes immutable snapshots only.</summary>
public sealed class InteractiveScenarioSession
{
    private readonly ScenarioSource scenario;
    private readonly string mapId;
    private ScenarioPresentationSnapshot? latest;
    private bool completed;

    public InteractiveScenarioSession(string scenarioReference)
    {
        var resolution = ScenarioSourceResolver.Resolve(scenarioReference);
        if (!resolution.IsSuccess) throw new InvalidOperationException("Scenario unavailable: " + scenarioReference);
        var loaded = ScenarioSourceLoader.Load(resolution.Source, resolution.Path);
        if (!loaded.IsSuccess || loaded.SourceScenario.Runtime is null) throw new InvalidOperationException("Scenario is invalid for interactive presentation.");
        scenario = loaded.SourceScenario;
        mapId = scenario.Runtime.MapId ?? string.Empty;
        ResetScenario();
    }

    public bool IsCompleted => completed;
    public int Tick => latest?.Tick ?? 0;
    public void ResetScenario() { latest = null; completed = false; }
    public void RunOneTick()
    {
        if (completed) return;
        var execution = M014ScenarioExecutor.Execute(scenario);
        if (execution.World is null) throw new InvalidOperationException("Scenario runtime did not produce an inspectable world.");
        latest = Project(execution.World.Snapshot(scenario.Runtime!.Ticks), execution.Provenance ?? new Dictionary<string, RuntimeEntityProvenance>(StringComparer.Ordinal));
        completed = true;
    }
    public void RunTicks(int count) { if (count < 0) throw new ArgumentOutOfRangeException(nameof(count)); for (var i = 0; i < count && !completed; i++) RunOneTick(); }
    public ScenarioPresentationSnapshot GetLatestSnapshot()
    {
        // The scenario has no mutable render state until its first fixed tick; callers receive a stable empty initial snapshot.
        return latest ?? new ScenarioPresentationSnapshot(scenario.Id, mapId, 0, "initial:" + scenario.Id, []);
    }
    private ScenarioPresentationSnapshot Project(EntityComponentSnapshot snapshot, IReadOnlyDictionary<string, RuntimeEntityProvenance> provenance)
    {
        var transforms = snapshot.Components.FirstOrDefault(x => x.TypeId == "component.continuous-transform-2d")?.Values ?? [];
        return new ScenarioPresentationSnapshot(scenario.Id, mapId, snapshot.Tick, snapshot.Fingerprint, transforms.Select(value =>
        {
            using var json = JsonDocument.Parse(value.Value);
            return new ScenarioPresentationEntity(value.EntityId, provenance.TryGetValue(value.EntityId, out var source) ? source.DefinitionId : string.Empty, json.RootElement.GetProperty("X").GetDouble(), json.RootElement.GetProperty("Y").GetDouble());
        }).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray());
    }
}
public sealed record ScenarioPresentationSnapshot(string ScenarioId, string MapId, int Tick, string Fingerprint, IReadOnlyList<ScenarioPresentationEntity> Entities);
public sealed record ScenarioPresentationEntity(string Id, string DefinitionId, double X, double Y);
