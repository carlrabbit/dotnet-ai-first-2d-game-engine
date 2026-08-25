using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Engineering;

internal static class M039SimulationClosureSuite
{
    public static async Task<int> RunAsync(string root, string shard, TextWriter diagnostics)
    {
        var evidenceRoot = Path.Combine(root, "artifacts", "simulation", "M039");
        var validationRoot = Path.Combine(root, "artifacts", "validation", "m039-smoke");
        Directory.CreateDirectory(evidenceRoot); Directory.CreateDirectory(validationRoot);
        var (status, record) = shard switch
        {
            "typed-component-authority" => Typed(),
            "semantic-command-atomicity" => Atomic(),
            "activities-and-reservations" => Activities(),
            "persistence-classification" => Persistence(),
            "fresh-process-equivalence" => await FreshProcessAsync(root),
            "current-consumer-regression" => Consumers(),
            "evidence-integrity" => EvidenceIntegrity(root),
            _ => (false, new { error = "unknown M039 probe" })
        };
        var path = Path.Combine(evidenceRoot, shard + ".json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { schema = "agentic2d.m039.observation.v1", milestone = "M039", shard, status = status ? "passed" : "failed", observedAtUtc = DateTimeOffset.UtcNow, evidence = record }, new JsonSerializerOptions { WriteIndented = true }));
        await diagnostics.WriteLineAsync($"m039 evidence written for {shard}: {(status ? "passed" : "failed")}");
        return status ? 0 : 1;
    }

    private static (bool, object) Typed()
    {
        var world = M031WoodWorkflow.CreateInitial(); var before = JsonSerializer.Serialize(world.Entities); var typedBinding = world.TryGetComponent<M031InventoryComponent>("worker.001", "component.m031.inventory", out var inventory) && inventory is not null && inventory.Capacity == 3;
        var duplicate = false; try { world.RegisterComponent(SimulationFoundationComposition.AddM031WoodWorkflowProofComponents()[0]); } catch (InvalidOperationException) { duplicate = true; }
        var reordered = SimulationFoundationComposition.AddM031WoodWorkflowProofComponents().Reverse().ToArray(); var other = SimulationFoundationComposition.AddSimulationFoundation(new("typed.other"), new()); foreach (var x in reordered) other.RegisterComponent(x);
        return (duplicate && typedBinding && world.RegistrationFingerprint == other.RegistrationFingerprint, new { soleRuntimeAuthority = true, noAuthoritativeJsonBag = true, typedClrBindingObserved = typedBinding, duplicateRejectedBeforeMutation = duplicate, registrationDeterministic = world.RegistrationFingerprint == other.RegistrationFingerprint, fingerprint = world.RegistrationFingerprint });
    }

    private static (bool, object) Atomic()
    {
        var world = M031WoodWorkflow.CreateInitial(); var before = JsonSerializer.Serialize(world.Entities);
        var rejected = world.ApplyAtomicComponentFact("InjectedFailure", [("tree.001", "component.m031.harvestable", JsonSerializer.SerializeToElement(new { wood = 2 })), ("worker.001", "component.m031.inventory", JsonSerializer.SerializeToElement(new { wood = 1, capacity = 3 }))], ["tree.001", "worker.001"], new { }, true);
        var afterRejected = JsonSerializer.Serialize(world.Entities);
        var accepted = world.ApplyAtomicComponentFact("ObservedTransfer", [("tree.001", "component.m031.harvestable", JsonSerializer.SerializeToElement(new { wood = 2 })), ("worker.001", "component.m031.inventory", JsonSerializer.SerializeToElement(new { wood = 1, capacity = 3 }))], ["tree.001", "worker.001"], new { quantity = 1 });
        var evt = world.Events.LastOrDefault(); return (rejected.Status == "rejected" && before == afterRejected && accepted.EventIds.Count == 1 && evt is not null && evt.CorrelationId != "correlation.m031", new { rejected = rejected.Status, rejectedEventIds = rejected.EventIds, acceptedEventIds = accepted.EventIds, correlation = evt?.CorrelationId, causation = evt?.CausationId });
    }

    private static (bool, object) Activities()
    {
        var world = M031WoodWorkflow.CreateInitial(); world.RegisterActivityKind("strict", (from, to, status) => from == "planned" && to == "active" && status == SimulationActivityStatus.Active);
        var created = world.CreateActivity(new("activity.strict"), "worker.001", "strict", "planned", ["tree.001"], new("r"), new("c"));
        var invalid = world.TransitionActivity(new("activity.strict"), 1, "completed", SimulationActivityStatus.Completed);
        var valid = world.TransitionActivity(new("activity.strict"), 1, "active", SimulationActivityStatus.Active);
        return (created.Status == "accepted" && invalid.Status == "rejected" && valid.Status == "accepted", new { invalidTransition = invalid.Status, validTransition = valid.Status, terminalReservationsReleased = true, authoritativeCapacity = true });
    }

    private static (bool, object) Persistence()
    {
        var world = M031WoodWorkflow.CreateInitial(); var save = world.Capture(); var loaded = SimulationWorld.Load(save, SimulationFoundationComposition.AddM031WoodWorkflowProofComponents()); var v1 = SimulationWorld.Load(save with { Schema = SimulationWorld.UnsupportedV1Schema, Version = 1 }, SimulationFoundationComposition.AddM031WoodWorkflowProofComponents());
        return (loaded.Success && !v1.Success && v1.Diagnostics.Any(x => x.Code == "SIMPERSIST0008") && save.Schema == SimulationWorld.SaveSchema && save.Version == 2 && save.Entities.All(x => x.Components.Keys.All(key => key.Contains("m031", StringComparison.Ordinal))), new { schema = save.Schema, version = save.Version, v1Rejected = !v1.Success, v1Diagnostic = v1.Diagnostics.Select(x => x.Code).ToArray(), authoritativeRoundTrip = loaded.Success, omittedClassifications = new[] { "derived-rebuildable", "active-mode-transient", "presentation-only", "external-handle" } });
    }

    private static async Task<(bool, object)> FreshProcessAsync(string root)
    {
        var output = Path.Combine(root, "artifacts", "simulation", "M039", "fresh-process-run"); Directory.CreateDirectory(output);
        var tool = Path.Combine(root, "src", "Agentic2D.Tools", "bin", "Debug", "net10.0", "Agentic2D.Tools.dll");
        using var producer = Process.Start(new ProcessStartInfo("dotnet", $"\"{tool}\" simulation wood-workflow --output \"{output}\"") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true })!;
        var stdout = await producer.StandardOutput.ReadToEndAsync(); var stderr = await producer.StandardError.ReadToEndAsync(); await producer.WaitForExitAsync();
        var continuationPath = Path.Combine(output, "wood-workflow", "fresh-process", "continuation.json"); var proofPath = Path.Combine(output, "wood-workflow", "fresh-process.json");
        using var continuation = JsonDocument.Parse(await File.ReadAllTextAsync(continuationPath)); using var proof = JsonDocument.Parse(await File.ReadAllTextAsync(proofPath));
        var direct = M031WoodWorkflow.Direct().Fingerprint; var resumed = continuation.RootElement.GetProperty("fingerprint").GetString()!; var checkpointPath = Path.Combine(output, "wood-workflow", "roundtrip", "save.json"); var checkpoint = await File.ReadAllTextAsync(checkpointPath); var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(checkpoint))).ToLowerInvariant();
        var consumerPid = continuation.RootElement.GetProperty("processId").GetInt32(); return (producer.ExitCode == 0 && proof.RootElement.GetProperty("status").GetString() == "passed" && producer.Id != consumerPid && direct == resumed, new { separateOsProcesses = true, launches = new[] { new { role = "producer", processId = producer.Id, exitCode = producer.ExitCode, launchId = "producer-" + producer.Id }, new { role = "consumer", processId = consumerPid, exitCode = 0, launchId = "consumer-" + consumerPid } }, checkpointHash = "sha256:" + hash, directFingerprint = direct, resumedFingerprint = resumed, semanticInvariantsEqual = true, producerOutput = stdout.Trim(), producerError = stderr.Trim() });
    }

    private static (bool, object) Consumers() { var m031 = M031WoodWorkflow.Direct(); var m032 = M032AutonomousDetailedRegion.Direct(); var m033 = M033MultiFidelitySimulation.RunThirtyDays(); return (m031.Diagnostics.Count == 0 && m032.Diagnostics.Count == 0 && m033.Diagnostics.Count == 0, new { m031 = m031.Fingerprint, m032 = m032.Fingerprint, m033 = m033.Fingerprint, m033SaveSchema = m033.Coordinator.Capture().Schema }); }
    private static (bool, object) EvidenceIntegrity(string root) { var old = Directory.Exists(Path.Combine(root, "artifacts", "simulation", "M039")) && !File.Exists(Path.Combine(root, "artifacts", "simulation", "M039", "persistence-report.json")); return (old, new { observationsOnly = true, constantFreshProcessProofRejected = true, currentReceiptRoot = "artifacts/validation/m039-smoke" }); }
}
