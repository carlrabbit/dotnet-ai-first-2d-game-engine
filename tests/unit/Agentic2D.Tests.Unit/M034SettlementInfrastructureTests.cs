using Agentic2D.Simulation;

namespace Agentic2D.Tests.Unit;

public sealed class M034SettlementInfrastructureTests
{
    [Test]
    public async Task ConstructionPlanLifecycleConservesDeliveredMaterialsAndCompletesOnce()
    {
        var run = M034SettlementInfrastructure.RunProof();
        await Assert.That(run.Plans.Count(plan => plan.State == ConstructionPlanState.Cancelled)).IsEqualTo(1);
        await Assert.That(run.Plans.Count(plan => plan.State == ConstructionPlanState.Completed)).IsEqualTo(run.Structures.Count);
        await Assert.That(run.Journal).Contains("BUILD-CANCEL:plan.river.00:returned=2");
    }

    [Test]
    public async Task EnvironmentalResourceWaterFlowHasNoImplicitLossAndRecoversShortage()
    {
        var run = M034SettlementInfrastructure.RunProof();
        await Assert.That(run.Flows.All(flow => flow.Lost == 0)).IsTrue();
        await Assert.That(run.ShortageRecovered).IsTrue();
        await Assert.That(run.Alerts.Any(alert => alert.Key == "water-unavailable" && alert.Status == SettlementAlertStatus.Resolved)).IsTrue();
    }

    [Test]
    public async Task CropProductionComfortInfrastructureAndMaintenanceArePersistentDomainState()
    {
        var run = M034SettlementInfrastructure.RunProof();
        await Assert.That(run.Farms.Single().Harvestable).IsEqualTo(30);
        await Assert.That(run.Structures.Single(structure => structure.DefinitionId == "structure.shelter").State).IsEqualTo(InfrastructureState.Operational);
        await Assert.That(run.MaintenanceRecovered && run.PersistenceRoundTrip).IsTrue();
    }

    [Test]
    public async Task OperationsProjectionIsReadOnlyAndRoadModifierIsShared()
    {
        var run = M034SettlementInfrastructure.RunProof();
        var road = run.Structures.Single(structure => structure.DefinitionId == "structure.road");
        await Assert.That(M034SettlementInfrastructure.SharedRoadTravelCost(10, road, false)).IsEqualTo(7);
        await Assert.That(run.Dashboard.Count(dashboard => dashboard.Fidelity == RegionFidelity.Detailed)).IsEqualTo(1);
        await Assert.That(run.Commands.Select(command => command.Command)).Contains("save");
    }

    [Test]
    public async Task InfrastructurePersistenceAndFourteenDayProofAreDeterministic()
    {
        var first = M034SettlementInfrastructure.RunProof(); var second = M034SettlementInfrastructure.RunProof();
        await Assert.That(first.Sustained && first.Day == 14).IsTrue();
        await Assert.That(M034SettlementInfrastructure.Fingerprint(first)).IsEqualTo(M034SettlementInfrastructure.Fingerprint(second));
    }

    [Test] public async Task InfrastructureLifecycleHasExplicitFailureAndRepair() => await Assert.That(M034SettlementInfrastructure.RunProof().MaintenanceRecovered).IsTrue();
    [Test] public async Task WaterFlowUsesIntegerConservedQuantities() => await Assert.That(M034SettlementInfrastructure.RunProof().Flows.All(flow => flow.Lost == 0)).IsTrue();
    [Test] public async Task ComfortInfrastructureHasFiniteCapacity() => await Assert.That(M034SettlementInfrastructure.RunProof().Structures.Any(structure => structure.DefinitionId == "structure.shelter" && structure.Capacity == 4)).IsTrue();
    [Test] public async Task MaintenanceProducesCausalAlerts() => await Assert.That(M034SettlementInfrastructure.RunProof().Alerts.Any(alert => alert.Key == "maintenance-due" && alert.Causes.Count > 0)).IsTrue();
    [Test] public async Task SettlementAlertHasStableKeyAndResolution() => await Assert.That(M034SettlementInfrastructure.RunProof().Alerts.All(alert => !string.IsNullOrWhiteSpace(alert.Key))).IsTrue();
    [Test] public async Task OperationsProjectionReportsOneDetailedRegion() => await Assert.That(M034SettlementInfrastructure.RunProof().Dashboard.Count(item => item.Fidelity == RegionFidelity.Detailed)).IsEqualTo(1);
}
