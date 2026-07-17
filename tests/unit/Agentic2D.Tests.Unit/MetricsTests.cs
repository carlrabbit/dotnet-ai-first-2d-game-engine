using System.Text.Json;
using Agentic2D.Engine;
using Agentic2D.Metrics;
using Agentic2D.Tools;

namespace Agentic2D.Tests.Unit;

public sealed class MetricsTests
{
    [Test]
    public async Task MetricVocabularyIsFiniteAndHasOnlySupportedKinds()
    {
        var ids = Enum.GetValues<RuntimeMetricId>();
        await Assert.That(ids.Length).IsEqualTo(21);
        await Assert.That(RuntimeMetricVocabulary.Id(RuntimeMetricId.RuntimeTickDuration)).IsEqualTo("runtime.tick.duration");
        await Assert.That(RuntimeMetricVocabulary.Id(RuntimeMetricId.PersistenceLoadDuration)).IsEqualTo("persistence.load.duration");
        await Assert.That(ids.Select(RuntimeMetricVocabulary.Kind).Distinct().Order()).IsEquivalentTo(new[] { RuntimeMetricKind.Counter, RuntimeMetricKind.Gauge, RuntimeMetricKind.Duration }.Order());
    }

    [Test]
    public async Task MetricsDoNotChangeRuntimeSemanticResult()
    {
        var off = RuntimeSmokeScenario.Run(8, MetricsCollectionMode.Off);
        var summary = RuntimeSmokeScenario.Run(8, MetricsCollectionMode.Summary);
        var perTick = RuntimeSmokeScenario.Run(8, MetricsCollectionMode.PerTick);
        var options = new JsonSerializerOptions { WriteIndented = false };

        await Assert.That(JsonSerializer.Serialize(off, options)).IsEqualTo(JsonSerializer.Serialize(summary, options));
        await Assert.That(JsonSerializer.Serialize(off, options)).IsEqualTo(JsonSerializer.Serialize(perTick, options));
    }

    [Test]
    public async Task PerTickStorageIsAFixedCapacityRecentWindow()
    {
        var metrics = new RuntimeMetrics(MetricsCollectionMode.PerTick, recentTickCapacity: 3);
        for (var tick = 1; tick <= 5; tick++)
        {
            metrics.BeginTick(tick);
            metrics.Increment(RuntimeMetricId.RuntimeCommandsAccepted);
            metrics.EndTick();
        }
        var snapshot = metrics.Snapshot();

        await Assert.That(snapshot.RecentTicks.Count).IsEqualTo(3);
        await Assert.That(snapshot.RecentTicks.Select(x => x.Tick)).IsEquivalentTo(new long[] { 3, 4, 5 });
        await Assert.That(snapshot.Summary.Single(x => x.Id == "runtime.commands.accepted").Total).IsEqualTo(5d);
    }

    [Test]
    public async Task PerTickCliWritesOnlyBoundedMetricsArtifacts()
    {
        var output = Path.Combine(Path.GetTempPath(), "agentic2d-metrics-tests", Guid.NewGuid().ToString("N"));
        var code = await ToolsCli.RunAsync(["runtime", "smoke", "--ticks", "4", "--metrics", "per-tick", "--output", output], new StringWriter(), new StringWriter());

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(output, "metrics-summary.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(output, "metrics-ticks.jsonl"))).IsTrue();
        await Assert.That(File.ReadAllLines(Path.Combine(output, "metrics-ticks.jsonl")).Length).IsEqualTo(4);
    }
}
