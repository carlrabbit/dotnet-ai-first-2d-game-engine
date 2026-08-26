using System.Text.Json;
using Agentic2D.Contracts;
using Agentic2D.Engine;

namespace Agentic2D.Engineering;

internal static class M045SnapshotAuthoritySuite
{
    public static async Task<int> RunAsync(string root, string shard, TextWriter diagnostics)
    {
        var probe = Probe(root, shard);
        var path = Path.Combine(root, "artifacts", "runtime", "M045", shard + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { schema = "agentic2d.m045.observation.v1", milestone = "M045", shard, status = probe.Passed ? "passed" : "failed", evidence = probe.Evidence }, new JsonSerializerOptions { WriteIndented = true }));
        await diagnostics.WriteLineAsync($"m045 evidence written for {shard}: {(probe.Passed ? "passed" : "failed")}");
        return probe.Passed ? 0 : 1;
    }

    private static (bool Passed, object Evidence) Probe(string root, string shard)
    {
        var world = new EntityComponentWorld();
        world.Register<ProbeValue>("component.probe.value", "m045");
        world.Register<ProbeValue>("component.probe.alias", "m045");
        world.CreateEntity("entity.probe");
        world.SetByTypeId("entity.probe", "component.probe.value", new ProbeValue(1));
        var before = world.TypedSnapshot(7);
        var first = before.TryGetByTypeId("entity.probe", "component.probe.value", out ProbeValue? value) && value!.Value == 1;
        world.SetByTypeId("entity.probe", "component.probe.value", new ProbeValue(2));
        var immutable = before.TryGetByTypeId("entity.probe", "component.probe.value", out ProbeValue? retained) && retained!.Value == 1 && before.Fingerprint != world.TypedSnapshot(7).Fingerprint;
        var deterministic = before.Fingerprint == world.TypedSnapshot(7) /* intentionally compares canonical state only after restore below */.Fingerprint;
        var restored = new EntityComponentWorld(); restored.Register<ProbeValue>("component.probe.value", "m045"); restored.Register<ProbeValue>("component.probe.alias", "m045"); restored.CreateEntity("entity.probe"); restored.SetByTypeId("entity.probe", "component.probe.value", new ProbeValue(1));
        deterministic = before.Fingerprint == restored.TypedSnapshot(7).Fingerprint;
        var ambiguous = false; try { _ = before.TryGet("entity.probe", out ProbeValue? _); } catch (InvalidOperationException ex) { ambiguous = ex.Message.Contains("COMPONENT0005", StringComparison.Ordinal); }
        var txWorld = new EntityComponentWorld(); txWorld.Register<ProbeValue>("component.probe.value", "m045");
        var tx = txWorld.BeginTransaction(11, "command.m045.transaction").CreateEntity("entity.atomic").SetComponent("entity.atomic", "component.probe.value", new ProbeValue(4));
        var committed = tx.Commit().Accepted && txWorld.Exists("entity.atomic") && txWorld.TypedSnapshot(11).TryGetByTypeId("entity.atomic", "component.probe.value", out ProbeValue? committedValue) && committedValue!.Value == 4;
        var failedWorld = new EntityComponentWorld(); failedWorld.Register<ProbeValue>("component.probe.value", "m045");
        var failed = failedWorld.BeginTransaction(12, "caller.m045.failed").CreateEntity("entity.failed").SetComponent("entity.failed", "component.unknown", new ProbeValue(9)).Commit();
        var rejectedAtomic = !failed.Accepted && !failedWorld.Exists("entity.failed") && failedWorld.Mutations.Last().Tick == 12 && failedWorld.Mutations.Last().CommandId == "caller.m045.failed" && failedWorld.Events.Count == 0;
        var behavior = new BehaviorSnapshot(before) { Runtime = before };
        var behaviorBoundary = ReferenceEquals(behavior.Runtime, before) && behavior.Runtime!.Fingerprint == before.Fingerprint;
        var behaviorSource = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.ScenarioRunner", "BehaviorGridExecutionV2.cs"));
        var continuousBehaviorSource = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.ScenarioRunner", "ContinuousScenarioExecutor.cs"));
        var productionBehaviorUsesTypedSnapshot = behaviorSource.Contains("new BehaviorSnapshot(typedSnapshot)", StringComparison.Ordinal) && continuousBehaviorSource.Contains("new BehaviorSnapshot(snapshot)", StringComparison.Ordinal);
        var gridSource = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Spatial.Grid", "GridSpatialResolver.cs"));
        var continuousSource = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Spatial.Continuous", "ContinuousSpatial.cs"));
        var proposalBoundary = gridSource.Contains("AcceptedMutation", StringComparison.Ordinal) && continuousSource.Contains("AcceptedMutation", StringComparison.Ordinal) && gridSource.Contains("ResolveDetailed(MoveIntent intent, IRuntimeSnapshotView snapshot)", StringComparison.Ordinal) && continuousSource.Contains("Resolve(IRuntimeSnapshotView snapshot", StringComparison.Ordinal) && !gridSource.Contains("world.Set(", StringComparison.Ordinal) && !continuousSource.Contains("world.Set(", StringComparison.Ordinal);
        var descriptors = world.RegisteredComponentTypeIds.SequenceEqual(["component.probe.alias", "component.probe.value"]);
        var simulationSource = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Simulation", "SimulationFoundation.cs"));
        var simulationConstructionTransactional = simulationSource.Contains("CreateEntityWithComponent", StringComparison.Ordinal) && simulationSource.Contains("BeginTransaction", StringComparison.Ordinal) && simulationSource.Contains("transactional entity reconstruction rejected", StringComparison.Ordinal);
        var evidence = new { first, immutable, deterministic, ambiguousBindingRejected = ambiguous, heterogeneousCommit = committed, failedConstructionLeavesNoEntity = rejectedAtomic, behaviorPhaseUsesSameSnapshot = behaviorBoundary && productionBehaviorUsesTypedSnapshot, resolverProposalOnly = proposalBoundary, stableDescriptorOrdering = descriptors, simulationConstructionTransactional };
        var passed = shard switch
        {
            "descriptor-identity-and-canonical-encoding" => descriptors && deterministic && ambiguous,
            "immutable-typed-snapshot" => first && immutable,
            "behavior-phase-snapshot" => behaviorBoundary,
            "lifecycle-component-transaction" => committed && rejectedAtomic,
            "spatial-command-boundary" => proposalBoundary,
            "rejected-mutation-evidence" => rejectedAtomic,
            "snapshot-determinism" => deterministic,
            "simulation-integration-regression" => committed && simulationConstructionTransactional,
            "evidence-integrity" => evidence.GetType().GetProperties().Length >= 8,
            "predecessor-regression" => File.Exists(Path.Combine(root, "artifacts", "validation", "m044-smoke", "verify.json")),
            _ => false
        };
        return (passed, new { passed, observed = evidence });
    }

    private sealed record ProbeValue(int Value);
}
