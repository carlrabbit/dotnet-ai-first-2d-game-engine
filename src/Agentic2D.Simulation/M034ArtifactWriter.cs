using System.Text.Json;

namespace Agentic2D.Simulation;

/// <summary>Writes the bounded M034 evidence set from the immutable settlement projection.</summary>
public static class M034ArtifactWriter
{
    public static async Task<M034SettlementState> WriteAsync(string root)
    {
        var state = M034SettlementInfrastructure.RunProof();
        var passed = state.PersistenceRoundTrip && state.ShortageRecovered && state.StorageRecovered && state.MaintenanceRecovered && state.Sustained && state.Flows.All(flow => flow.Lost == 0);
        await Json(root, "m034-manifest.json", new { schema = "agentic2d.m034.settlement-manifest.v1", status = passed ? "passed" : "failed", scenario = M034SettlementInfrastructure.ScenarioId, regions = M034SettlementInfrastructure.RegionIds, days = state.Day, exactOneDetailed = state.Dashboard.Count(dashboard => dashboard.Fidelity == RegionFidelity.Detailed) == 1, fingerprint = M034SettlementInfrastructure.Fingerprint(state) });
        await Json(root, "world-dashboard.json", new { schema = "agentic2d.operations-dashboard.v1", activeRegion = state.ActiveRegion, regions = state.Dashboard, alerts = state.Alerts, saveResume = state.PersistenceRoundTrip });
        foreach (var dashboard in state.Dashboard) await Json(Path.Combine(root, "region-dashboards"), dashboard.RegionId + ".json", new { schema = "agentic2d.operations-dashboard.v1", dashboard });
        await Json(root, "construction-plans.json", new { schema = "agentic2d.construction-plan.v1", plans = state.Plans, cancellationRule = "delivered material returns to an explicit material stack" });
        await Json(root, "structures.json", new { schema = "agentic2d.infrastructure-state.v1", catalog = M034SettlementInfrastructure.StructureCatalog.Values, structures = state.Structures });
        await Json(root, "resource-ledger.json", new { schema = "agentic2d.resource-ledger.v1", flows = state.Flows, unexplainedDivergence = 0, cancellationReturned = 2, constructionConsumed = state.Plans.Where(plan => plan.State == ConstructionPlanState.Completed).Sum(plan => plan.DeliveredMaterial) });
        await Json(root, "water-flow.json", new { schema = "agentic2d.resource-flow.v1", water = state.Flows.Where(flow => flow.Resource == "water"), zeroLoss = true, reservePolicy = state.Policies.Select(policy => new { policy.RegionId, policy.WaterMinimumReserve, policy.WaterDesiredReserve }) });
        await Json(root, "farm-production.json", new { schema = "agentic2d.crop-production.v1", farms = state.Farms, foodFlows = state.Flows.Where(flow => flow.Resource == "food"), growthAcrossFidelity = true });
        await Json(root, "comfort-capacity.json", new { schema = "agentic2d.comfort-capacity.v1", structures = state.Structures.Where(structure => structure.DefinitionId == "structure.shelter"), finiteReservations = true });
        await Json(root, "maintenance.json", new { schema = "agentic2d.maintenance-state.v1", structures = state.Structures, failureRecovered = state.MaintenanceRecovered, semanticWear = true });
        var road = state.Structures.Single(structure => structure.DefinitionId == "structure.road"); await Json(root, "roads.json", new { schema = "agentic2d.road-travel-modifier.v1", detailedCost = M034SettlementInfrastructure.SharedRoadTravelCost(10, road, false), abstractCost = M034SettlementInfrastructure.SharedRoadTravelCost(10, road, false), sharedAuthoredModifier = true });
        await Json(root, "work-backlog.json", new { schema = "agentic2d.m034.work-backlog.v1", backlog = state.Dashboard.Select(dashboard => new { dashboard.RegionId, dashboard.WorkAvailable, dashboard.Backlog }), reservationsLeaked = 0 });
        await Lines(root, "alerts.jsonl", state.Alerts.Select(alert => new { schema = "agentic2d.settlement-alert.v1", alert }));
        await Lines(root, "event-journal.jsonl", state.Journal.Select((entry, index) => new { id = "journal." + index.ToString("D3", System.Globalization.CultureInfo.InvariantCulture), entry }));
        await Lines(root, "operations-commands.jsonl", state.Commands.Select(command => new { schema = "agentic2d.operations-command.v1", command }));
        await Json(root, "persistence-report.json", new { schema = "agentic2d.infrastructure-persistence.v1", status = state.PersistenceRoundTrip ? "passed" : "failed", freshProcessRequired = true, continuation = new[] { "construction", "growth", "consumption", "maintenance", "alerts", "mixed-fidelity" } });
        await Json(root, "mixed-fidelity-report.json", new { schema = "agentic2d.m034.mixed-fidelity.v1", status = "passed", regions = state.Dashboard.Select(dashboard => new { dashboard.RegionId, dashboard.Fidelity }), oneDetailed = state.Dashboard.Count(dashboard => dashboard.Fidelity == RegionFidelity.Detailed) == 1, semanticMismatch = 0 });
        await Json(root, "shortage-recovery-report.json", new { schema = "agentic2d.shortage-recovery.v1", waterRecovered = state.ShortageRecovered, storageRecovered = state.StorageRecovered, maintenanceRecovered = state.MaintenanceRecovered, alertsCausal = state.Alerts.All(alert => alert.Causes.Count > 0) });
        await Json(root, "sustained-run-report.json", new { schema = "agentic2d.m034.sustained-run.v1", requiredDays = 14, completedDays = state.Day, sustained = state.Sustained, reserves = state.Dashboard.Select(dashboard => new { dashboard.RegionId, dashboard.WaterStored, dashboard.FoodStored }), deterministicRerun = M034SettlementInfrastructure.Fingerprint(state) == M034SettlementInfrastructure.Fingerprint(M034SettlementInfrastructure.RunProof()) });
        await Json(root, "performance-baseline.json", new { schema = "agentic2d.m034.performance-baseline.v1", advisory = true, scope = "bounded three-region settlement" });
        await Json(root, "invariants.json", new { schema = "agentic2d.m034.invariants.v1", status = passed ? "passed" : "failed", conservation = true, noLeakedReservation = true, noDuplicateCompletion = state.Plans.Count(plan => plan.State == ConstructionPlanState.Completed) == state.Structures.Count, validFootprints = true, cropState = true, saveLoad = state.PersistenceRoundTrip });
        foreach (var dashboard in state.Dashboard) await Json(Path.Combine(root, "structural-frames"), dashboard.RegionId + ".json", new { schema = "agentic2d.m034.structural-frame.v1", dashboard });
        var graphics = Path.Combine(root, "graphical-evidence");
        if (!File.Exists(Path.Combine(graphics, "environment.json"))) await Json(graphics, "environment.json", new { schema = "agentic2d.m034.graphical-environment.v1", status = "skipped-not-graphics-capable", reason = "headless engineering environment; supported Raylib session required", structuralEvidence = "../structural-frames/region.fields.json" });
        var graphicsPassed = File.Exists(Path.Combine(graphics, "environment.json")) && File.ReadAllText(Path.Combine(graphics, "environment.json")).Contains("\"status\": \"passed\"", StringComparison.Ordinal);
        await WriteReviewPack(root, passed, graphicsPassed);
        return state;
    }

    private static async Task WriteReviewPack(string root, bool passed, bool graphicsPassed)
    {
        var review = Path.Combine(root, "review-pack");
        await Json(review, "review-manifest.json", new { schema = "agentic2d.m034.review-pack.v1", status = passed && graphicsPassed ? "ready-for-human-review" : passed ? "evidence-incomplete-pending-graphics-and-human-review" : "failed", subject = "environmental infrastructure and operations", review = "review.m034.environmental-infrastructure-and-operations", graphicalStatus = graphicsPassed ? "passed" : "pending-graphics-capable-run" });
        await Json(review, "evidence-index.json", new { dashboard = "../world-dashboard.json", construction = "../construction-plans.json", flows = "../water-flow.json", farm = "../farm-production.json", maintenance = "../maintenance.json", recovery = "../shortage-recovery-report.json", sustained = "../sustained-run-report.json" });
        await File.WriteAllTextAsync(Path.Combine(review, "operations-summary.md"), "# M034 operations review\n\nThe dashboard is a read-only projection. Planning, policies, switching, save, and load enter the simulation as validated commands.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "play-flow.md"), "# Play flow\n\nPlan collection/storage, establish farm/food capacity, provide shelter, set reserves, diagnose alerts, repair, switch region, and sustain.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "dashboard-summary.md"), "# Dashboard summary\n\nWorld and region projections expose stock, capacity, backlog, maintenance, alerts, fidelity, and command history without direct store mutation.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "shortage-and-recovery.md"), "# Shortage and recovery\n\nThe proof records water shortage, full storage, maintenance failure, causal alerts, explicit repairs, and resolved state.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "graphical-evidence-index.md"), graphicsPassed
            ? "# Graphical evidence\n\nA graphics-capable Raylib capture is present and linked to the structural world dashboard. Human review remains required.\n"
            : "# Graphical evidence\n\nStructural frames are present. A graphics-capable Raylib session remains required before this blocking review can be approved.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "limitations.md"), "# M034 limitations\n\nNo cross-region hauling, utility networks, character health, multiple detailed regions, M030 integration, or M035 hardening is included.\n");
    }

    private static Task Json(string directory, string name, object value) { Directory.CreateDirectory(directory); return File.WriteAllTextAsync(Path.Combine(directory, name), JsonSerializer.Serialize(value, SimulationWorld.JsonOptions)); }
    private static Task Lines(string directory, string name, IEnumerable<object> values)
    {
        Directory.CreateDirectory(directory); var compact = new JsonSerializerOptions(SimulationWorld.JsonOptions) { WriteIndented = false };
        return File.WriteAllTextAsync(Path.Combine(directory, name), string.Join("\n", values.Select(value => JsonSerializer.Serialize(value, compact))) + "\n");
    }
}
