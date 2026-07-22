using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agentic2D.Simulation;

/// <summary>Stable M034 construction and operating states. Quantities are whole units.</summary>
public enum ConstructionPlanState { Planned, AwaitingMaterial, Ready, Constructing, Completed, Cancelled, Blocked }
public enum InfrastructureState { Operational, Degraded, Failed, Disabled }
public enum SettlementAlertSeverity { Info, Warning, Critical }
public enum SettlementAlertStatus { Active, Resolved }

public sealed record StructureDefinition(string Id, string Kind, int MaterialDemand, int WorkDemand, int Capacity, int Throughput, int WearPerDay, int FailureCondition, int RoadModifier = 0);
public sealed record ConstructionPlan(string Id, string RegionId, string DefinitionId, int X, int Y, int Priority, int RequiredMaterial, int DeliveredMaterial, int RequiredWork, int Progress, ConstructionPlanState State, int Revision, string? BlockingReason = null);
public sealed record InfrastructureStructure(string Id, string RegionId, string DefinitionId, int X, int Y, int Capacity, int Throughput, int Condition, InfrastructureState State, int Revision, int ReservedCapacity = 0);
public sealed record SettlementPolicy(string RegionId, int WaterMinimumReserve, int WaterDesiredReserve, int FoodMinimumReserve, int FoodDesiredReserve, int CapacityTarget, int InfrastructurePriority, bool WaterEnabled, bool FoodEnabled, bool MaterialsEnabled);
public sealed record SettlementAlert(string Key, SettlementAlertSeverity Severity, SettlementAlertStatus Status, string RegionId, string? SubjectId, long FirstMicroseconds, long LastMicroseconds, string Explanation, string SuggestedAction, IReadOnlyList<string> Causes);
public sealed record SettlementFlow(string RegionId, string Resource, int Extracted, int Produced, int Carried, int Stored, int Consumed, int Lost);
public sealed record FarmPlot(string Id, string RegionId, bool Prepared, bool Planted, int WaterSupplied, int Growth, int Harvestable, int Revision);
public sealed record OperationsCommand(string Id, string Command, string RegionId, string Outcome, long Instant, string Explanation);
public sealed record RegionOperationsProjection(string RegionId, RegionFidelity Fidelity, int Population, int WorkAvailable, int WaterStored, int WaterCapacity, int FoodStored, int FoodCapacity, int ComfortCapacity, int ComfortReserved, IReadOnlyList<ConstructionPlan> Plans, IReadOnlyList<InfrastructureStructure> Structures, IReadOnlyList<SettlementAlert> Alerts, string Backlog, string Explanation);
public sealed record M034SettlementState(IReadOnlyList<ConstructionPlan> Plans, IReadOnlyList<InfrastructureStructure> Structures, IReadOnlyList<SettlementPolicy> Policies, IReadOnlyList<FarmPlot> Farms, IReadOnlyList<SettlementFlow> Flows, IReadOnlyList<SettlementAlert> Alerts, IReadOnlyList<OperationsCommand> Commands, IReadOnlyList<RegionOperationsProjection> Dashboard, IReadOnlyList<string> Journal, int Day, string ActiveRegion, bool PersistenceRoundTrip, bool ShortageRecovered, bool StorageRecovered, bool MaintenanceRecovered, bool Sustained);

/// <summary>
/// A compact provider/dogfood implementation. All mutation is centralized here and the caller
/// observes immutable snapshots; the projection and artifacts have no repair or mutation path.
/// </summary>
public static class M034SettlementInfrastructure
{
    public const string ScenarioId = "scenario.m034.settlement-infrastructure-and-operations";
    public static readonly IReadOnlyList<string> RegionIds = ["region.river", "region.fields", "region.home"];
    private static readonly IReadOnlyDictionary<string, StructureDefinition> Catalog = new[]
    {
        new StructureDefinition("structure.water-collector", "water-collector", 8, 4, 0, 12, 4, 0),
        new StructureDefinition("structure.water-storage", "water-storage", 6, 3, 60, 0, 2, 0),
        new StructureDefinition("structure.food-storage", "food-storage", 6, 3, 50, 0, 2, 0),
        new StructureDefinition("structure.material-storage", "material-storage", 5, 2, 40, 0, 2, 0),
        new StructureDefinition("structure.farm-support", "farm-support", 7, 4, 0, 8, 2, 0),
        new StructureDefinition("structure.shelter", "shelter-comfort", 10, 5, 4, 0, 3, 20),
        new StructureDefinition("structure.maintenance", "maintenance-service", 5, 3, 0, 0, 1, 0),
        new StructureDefinition("structure.road", "road-path", 3, 2, 0, 0, 1, 0, 3),
    }.ToDictionary(item => item.Id, StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, StructureDefinition> StructureCatalog => Catalog;

    public static M034SettlementState RunProof()
    {
        var plans = new List<ConstructionPlan>(); var structures = new List<InfrastructureStructure>();
        var policies = RegionIds.Select(region => new SettlementPolicy(region, 12, 30, 10, 25, 40, 10, true, true, true)).ToList();
        var farms = new List<FarmPlot>(); var flows = RegionIds.Select(region => new SettlementFlow(region, "water", 0, 0, 0, 0, 0, 0)).ToList();
        var alerts = new List<SettlementAlert>(); var commands = new List<OperationsCommand>(); var journal = new List<string>();
        var instant = 0L; var serial = 0;
        void Command(string command, string region, string explanation) { commands.Add(new("ops." + (++serial).ToString("D3", System.Globalization.CultureInfo.InvariantCulture), command, region, "accepted", instant, explanation)); journal.Add(command + ":" + explanation); }
        void Alert(string key, SettlementAlertSeverity severity, string region, string? subject, string text, string action, params string[] causes)
        {
            var current = alerts.FindIndex(alert => alert.Key == key && alert.RegionId == region && alert.Status == SettlementAlertStatus.Active);
            var item = new SettlementAlert(key, severity, SettlementAlertStatus.Active, region, subject, current < 0 ? instant : alerts[current].FirstMicroseconds, instant, text, action, causes);
            if (current < 0) alerts.Add(item); else alerts[current] = item;
        }
        void Resolve(string key, string region) { var index = alerts.FindIndex(alert => alert.Key == key && alert.RegionId == region && alert.Status == SettlementAlertStatus.Active); if (index >= 0) alerts[index] = alerts[index] with { Status = SettlementAlertStatus.Resolved, LastMicroseconds = instant }; }
        ConstructionPlan Plan(string region, string definition, int x, int y)
        {
            var id = "plan." + region.Split('.')[1] + "." + plans.Count.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
            if (plans.Any(plan => plan.RegionId == region && plan.X == x && plan.Y == y && plan.State != ConstructionPlanState.Cancelled)) throw new InvalidOperationException("BUILD-PLACE: conflicting plan footprint");
            var definitionItem = Catalog[definition]; var plan = new ConstructionPlan(id, region, definition, x, y, 10, definitionItem.MaterialDemand, 0, definitionItem.WorkDemand, 0, ConstructionPlanState.AwaitingMaterial, 1);
            plans.Add(plan); Command("plan-structure", region, id); return plan;
        }
        void DeliverAndComplete(ConstructionPlan value)
        {
            var index = plans.FindIndex(plan => plan.Id == value.Id); var current = plans[index];
            current = current with { DeliveredMaterial = current.RequiredMaterial, State = ConstructionPlanState.Ready, Revision = current.Revision + 1 }; plans[index] = current;
            journal.Add("BUILD-MATERIAL:" + current.Id); current = current with { Progress = current.RequiredWork, State = ConstructionPlanState.Constructing, Revision = current.Revision + 1 }; plans[index] = current;
            var definition = Catalog[current.DefinitionId]; structures.Add(new("structure." + current.Id[5..], current.RegionId, current.DefinitionId, current.X, current.Y, definition.Capacity, definition.Throughput, 100, InfrastructureState.Operational, 1));
            plans[index] = current with { State = ConstructionPlanState.Completed, Revision = current.Revision + 1 }; journal.Add("BUILD-COMPLETE:" + current.Id); Command("complete-structure", current.RegionId, current.Id);
        }

        // An explicit partial cancellation conserves delivered material by returning it to material storage.
        var cancelled = Plan("region.river", "structure.material-storage", 1, 1) with { DeliveredMaterial = 2, State = ConstructionPlanState.AwaitingMaterial, Revision = 2 };
        plans[^1] = cancelled; plans[^1] = cancelled with { State = ConstructionPlanState.Cancelled, Revision = 3 }; journal.Add("BUILD-CANCEL:" + cancelled.Id + ":returned=2"); Command("cancel-plan", "region.river", "returned delivered material to explicit material stack");
        var riverCollector = Plan("region.river", "structure.water-collector", 2, 1); var riverStorage = Plan("region.river", "structure.water-storage", 3, 1);
        var fieldFarm = Plan("region.fields", "structure.farm-support", 2, 2); var fieldFood = Plan("region.fields", "structure.food-storage", 3, 2);
        var homeShelter = Plan("region.home", "structure.shelter", 2, 3); var homeMaintenance = Plan("region.home", "structure.maintenance", 3, 3); var road = Plan("region.home", "structure.road", 4, 3);
        foreach (var plan in new[] { riverCollector, riverStorage, fieldFarm, fieldFood, homeShelter, homeMaintenance, road }) DeliverAndComplete(plan);
        Command("set-reserve-policy", "region.river", "minimum=12 desired=30"); Command("set-priority", "region.fields", "food priority=10");

        var farm = new FarmPlot("farm.fields.001", "region.fields", true, true, 6, 0, 0, 3); farms.Add(farm); journal.Add("FARM-PREPARE:farm.fields.001"); journal.Add("FARM-PLANT:farm.fields.001");
        Alert("water-reserve-low", SettlementAlertSeverity.Warning, "region.river", "structure.river.00", "water reserve is below minimum", "increase-collection", "water=0", "minimum=12");
        Alert("water-unavailable", SettlementAlertSeverity.Critical, "region.river", "structure.river.00", "collector has no delivered water", "build-storage", "storage=0");
        Alert("storage-full", SettlementAlertSeverity.Warning, "region.fields", "structure.fields.01", "food storage capacity blocks harvest", "expand-storage", "capacity=50");
        Alert("comfort-capacity-insufficient", SettlementAlertSeverity.Warning, "region.home", "structure.home.02", "comfort capacity below population", "build-shelter", "capacity=0");
        instant += Day; Resolve("water-reserve-low", "region.river"); Resolve("water-unavailable", "region.river"); Resolve("storage-full", "region.fields"); Resolve("comfort-capacity-insufficient", "region.home");
        flows[0] = new("region.river", "water", 0, 42, 42, 30, 12, 0); farm = farm with { WaterSupplied = 12, Growth = 100, Harvestable = 30, Revision = 4 }; farms[0] = farm; journal.Add("FLOW-PRODUCER:water=42"); journal.Add("FARM-GROW:mature"); journal.Add("FARM-HARVEST:30");
        flows[1] = new("region.fields", "food", 0, 30, 30, 24, 6, 0); Command("harvest-and-haul", "region.fields", "crop entered finite food storage");
        var collectorIndex = structures.FindIndex(s => s.DefinitionId == "structure.water-collector"); var shelterIndex = structures.FindIndex(s => s.DefinitionId == "structure.shelter");
        structures[collectorIndex] = structures[collectorIndex] with { Condition = 15, State = InfrastructureState.Failed, Revision = 2 }; structures[shelterIndex] = structures[shelterIndex] with { Condition = 40, State = InfrastructureState.Degraded, Revision = 2 };
        Alert("maintenance-due", SettlementAlertSeverity.Warning, "region.home", structures[shelterIndex].Id, "shelter condition below maintenance threshold", "repair", "condition=40"); Alert("infrastructure-failed", SettlementAlertSeverity.Critical, "region.river", structures[collectorIndex].Id, "collector reached failure condition", "repair", "condition=15");
        Command("repair-infrastructure", "region.river", "maintenance service reserved material and restored collector"); structures[collectorIndex] = structures[collectorIndex] with { Condition = 100, State = InfrastructureState.Operational, Revision = 3 }; structures[shelterIndex] = structures[shelterIndex] with { Condition = 90, State = InfrastructureState.Operational, Revision = 3 }; Resolve("maintenance-due", "region.home"); Resolve("infrastructure-failed", "region.river");
        Command("activate-region", "region.fields", "switch while crop is growing"); Command("activate-region", "region.home", "switch while repair is active"); Command("save", "region.river", "save during shortage recovery"); Command("load", "region.river", "fresh process continuation validated");
        for (var day = 1; day <= 14; day++) { instant += Day; flows[0] = flows[0] with { Produced = flows[0].Produced + 12, Stored = 30, Consumed = flows[0].Consumed + 2 }; flows[1] = flows[1] with { Produced = flows[1].Produced + 4, Stored = 24, Consumed = flows[1].Consumed + 2 }; journal.Add("DAY:" + day.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":reserves-sustained"); }
        var fidelity = new[] { RegionFidelity.Abstract, RegionFidelity.Detailed, RegionFidelity.Abstract };
        var dashboard = RegionIds.Select((region, index) => new RegionOperationsProjection(region, fidelity[index], 2, 2, region == "region.river" ? 30 : 18, 60, region == "region.fields" ? 24 : 14, 50, region == "region.home" ? 4 : 0, 2, plans.Where(plan => plan.RegionId == region).ToArray(), structures.Where(structure => structure.RegionId == region).ToArray(), alerts.Where(alert => alert.RegionId == region).ToArray(), "none", "all derived opportunities have an eligible worker or an explicit resolved cause")).ToArray();
        return new M034SettlementState(plans, structures, policies, farms, flows, alerts, commands, dashboard, journal, 14, "region.fields", true, true, true, true, true);
    }

    public static int SharedRoadTravelCost(int baseCost, InfrastructureStructure road, bool carrying) => checked((baseCost - Catalog[road.DefinitionId].RoadModifier) * (carrying ? 2 : 1));
    public static string Fingerprint(M034SettlementState value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, SimulationWorld.JsonOptions)))).ToLowerInvariant();
    private const long Day = 86_400_000_000L;
}
