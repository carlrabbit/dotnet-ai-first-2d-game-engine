using System.Diagnostics;

namespace Agentic2D.Metrics;

/// <summary>Finite, allocation-free-on-the-hot-path runtime observation vocabulary.</summary>
public enum RuntimeMetricId
{
    RuntimeTickDuration,
    RuntimePhaseBehaviorDuration,
    RuntimePhaseSpatialDuration,
    RuntimePhaseMutationDuration,
    RuntimePhasePresentationDuration,
    RuntimePhaseRenderProjectionDuration,
    RuntimeEntitiesActive,
    RuntimeCommandsSubmitted,
    RuntimeCommandsAccepted,
    RuntimeCommandsRejected,
    RuntimeEventsEmitted,
    SpatialQueries,
    SpatialCollisionChecks,
    PresentationRenderItems,
    PresentationEffectsActive,
    PresentationParticlesActive,
    PresentationUiElements,
    PresentationTextCommands,
    PresentationSoundCommands,
    PersistenceSaveDuration,
    PersistenceLoadDuration,
}

public enum RuntimeMetricKind { Counter, Gauge, Duration }
public enum MetricsCollectionMode { Off, Summary, PerTick }

public static class RuntimeMetricVocabulary
{
    public static int Count => Enum.GetValues<RuntimeMetricId>().Length;

    public static string Id(RuntimeMetricId id) => id switch
    {
        RuntimeMetricId.RuntimeTickDuration => "runtime.tick.duration",
        RuntimeMetricId.RuntimePhaseBehaviorDuration => "runtime.phase.behavior.duration",
        RuntimeMetricId.RuntimePhaseSpatialDuration => "runtime.phase.spatial.duration",
        RuntimeMetricId.RuntimePhaseMutationDuration => "runtime.phase.mutation.duration",
        RuntimeMetricId.RuntimePhasePresentationDuration => "runtime.phase.presentation.duration",
        RuntimeMetricId.RuntimePhaseRenderProjectionDuration => "runtime.phase.render-projection.duration",
        RuntimeMetricId.RuntimeEntitiesActive => "runtime.entities.active",
        RuntimeMetricId.RuntimeCommandsSubmitted => "runtime.commands.submitted",
        RuntimeMetricId.RuntimeCommandsAccepted => "runtime.commands.accepted",
        RuntimeMetricId.RuntimeCommandsRejected => "runtime.commands.rejected",
        RuntimeMetricId.RuntimeEventsEmitted => "runtime.events.emitted",
        RuntimeMetricId.SpatialQueries => "spatial.queries",
        RuntimeMetricId.SpatialCollisionChecks => "spatial.collision-checks",
        RuntimeMetricId.PresentationRenderItems => "presentation.render-items",
        RuntimeMetricId.PresentationEffectsActive => "presentation.effects.active",
        RuntimeMetricId.PresentationParticlesActive => "presentation.particles.active",
        RuntimeMetricId.PresentationUiElements => "presentation.ui-elements",
        RuntimeMetricId.PresentationTextCommands => "presentation.text-commands",
        RuntimeMetricId.PresentationSoundCommands => "presentation.sound-commands",
        RuntimeMetricId.PersistenceSaveDuration => "persistence.save.duration",
        RuntimeMetricId.PersistenceLoadDuration => "persistence.load.duration",
        _ => throw new ArgumentOutOfRangeException(nameof(id)),
    };

    public static RuntimeMetricKind Kind(RuntimeMetricId id) => id switch
    {
        RuntimeMetricId.RuntimeEntitiesActive or RuntimeMetricId.PresentationRenderItems or RuntimeMetricId.PresentationEffectsActive or RuntimeMetricId.PresentationParticlesActive or RuntimeMetricId.PresentationUiElements => RuntimeMetricKind.Gauge,
        RuntimeMetricId.RuntimeTickDuration or RuntimeMetricId.RuntimePhaseBehaviorDuration or RuntimeMetricId.RuntimePhaseSpatialDuration or RuntimeMetricId.RuntimePhaseMutationDuration or RuntimeMetricId.RuntimePhasePresentationDuration or RuntimeMetricId.RuntimePhaseRenderProjectionDuration or RuntimeMetricId.PersistenceSaveDuration or RuntimeMetricId.PersistenceLoadDuration => RuntimeMetricKind.Duration,
        _ => RuntimeMetricKind.Counter,
    };
}

public sealed class RuntimeMetrics
{
    public const int DefaultRecentTickCapacity = 300;
    private readonly MetricsCollectionMode mode;
    private readonly double[] current = new double[RuntimeMetricVocabulary.Count];
    private readonly double[] totals = new double[RuntimeMetricVocabulary.Count];
    private readonly double[] minima = Enumerable.Repeat(double.PositiveInfinity, RuntimeMetricVocabulary.Count).ToArray();
    private readonly double[] maxima = new double[RuntimeMetricVocabulary.Count];
    private readonly TickMetricSnapshot[] recent;
    private long currentTick;
    private long tickStart;
    private int recentCount;
    private int recentNext;
    private long ticks;

    public RuntimeMetrics(MetricsCollectionMode mode, int recentTickCapacity = DefaultRecentTickCapacity)
    {
        if (recentTickCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(recentTickCapacity));
        this.mode = mode;
        recent = mode == MetricsCollectionMode.PerTick ? new TickMetricSnapshot[recentTickCapacity] : [];
    }

    public MetricsCollectionMode Mode => mode;
    public bool IsEnabled => mode != MetricsCollectionMode.Off;

    public void Reset()
    {
        Array.Clear(current); Array.Clear(totals); Array.Fill(minima, double.PositiveInfinity); Array.Clear(maxima);
        currentTick = 0; tickStart = 0; recentCount = 0; recentNext = 0; ticks = 0;
    }

    public void BeginTick(long tick)
    {
        if (!IsEnabled) return;
        // Setup observations (entity creation and submitted commands) belong to tick one.
        if (ticks > 0) Array.Clear(current);
        currentTick = tick;
        tickStart = Stopwatch.GetTimestamp();
    }

    public MetricDurationScope Measure(RuntimeMetricId id) => IsEnabled ? new(this, id, Stopwatch.GetTimestamp()) : default;
    public void Increment(RuntimeMetricId id, double value = 1) { if (IsEnabled) current[(int)id] += value; }
    public void SetGauge(RuntimeMetricId id, double value) { if (IsEnabled) current[(int)id] = value; }

    public void EndTick()
    {
        if (!IsEnabled) return;
        current[(int)RuntimeMetricId.RuntimeTickDuration] += Stopwatch.GetElapsedTime(tickStart).TotalMilliseconds;
        for (var i = 0; i < current.Length; i++)
        {
            totals[i] += current[i]; minima[i] = Math.Min(minima[i], current[i]); maxima[i] = Math.Max(maxima[i], current[i]);
        }
        ticks++;
        if (mode == MetricsCollectionMode.PerTick)
        {
            recent[recentNext] = new TickMetricSnapshot(currentTick, Values(current));
            recentNext = (recentNext + 1) % recent.Length;
            recentCount = Math.Min(recentCount + 1, recent.Length);
        }
    }

    internal void AddDuration(RuntimeMetricId id, long started) => current[(int)id] += Stopwatch.GetElapsedTime(started).TotalMilliseconds;

    public RuntimeMetricsSnapshot Snapshot()
    {
        if (!IsEnabled) return RuntimeMetricsSnapshot.Off;
        var values = Values(current);
        var summary = new List<MetricSummary>(RuntimeMetricVocabulary.Count);
        for (var i = 0; i < RuntimeMetricVocabulary.Count; i++)
        {
            var id = (RuntimeMetricId)i;
            var average = ticks == 0 ? 0 : totals[i] / ticks;
            summary.Add(new MetricSummary(RuntimeMetricVocabulary.Id(id), RuntimeMetricVocabulary.Kind(id), totals[i], average, ticks == 0 ? 0 : minima[i], maxima[i]));
        }
        var ordered = new List<TickMetricSnapshot>(recentCount);
        for (var i = 0; i < recentCount; i++) ordered.Add(recent[(recentNext - recentCount + i + recent.Length) % recent.Length]);
        var durations = ordered.Select(x => x.Values[RuntimeMetricVocabulary.Id(RuntimeMetricId.RuntimeTickDuration)]).Order().ToArray();
        var p95 = durations.Length == 0 ? (double?)null : durations[(int)Math.Ceiling(durations.Length * .95) - 1];
        var totalDuration = totals[(int)RuntimeMetricId.RuntimeTickDuration];
        return new RuntimeMetricsSnapshot(mode, ticks, recent.Length, new TickMetricSnapshot(currentTick, values), summary, ordered, p95, totalDuration <= 0 ? 0 : ticks / (totalDuration / 1000d));
    }

    private static IReadOnlyDictionary<string, double> Values(double[] source)
    {
        var values = new Dictionary<string, double>(RuntimeMetricVocabulary.Count, StringComparer.Ordinal);
        for (var i = 0; i < RuntimeMetricVocabulary.Count; i++) values.Add(RuntimeMetricVocabulary.Id((RuntimeMetricId)i), source[i]);
        return values;
    }
}

public readonly struct MetricDurationScope(RuntimeMetrics? metrics, RuntimeMetricId id, long started) : IDisposable
{
    public void Dispose() { if (metrics is not null) metrics.AddDuration(id, started); }
}

public sealed record MetricSummary(string Id, RuntimeMetricKind Kind, double Total, double Average, double Minimum, double Maximum);
public sealed record TickMetricSnapshot(long Tick, IReadOnlyDictionary<string, double> Values);
public sealed record RuntimeMetricsSnapshot(MetricsCollectionMode Mode, long TickCount, int RecentCapacity, TickMetricSnapshot CurrentTick, IReadOnlyList<MetricSummary> Summary, IReadOnlyList<TickMetricSnapshot> RecentTicks, double? RecentP95TickDurationMilliseconds, double EffectiveTicksPerSecond)
{
    public static RuntimeMetricsSnapshot Off { get; } = new(MetricsCollectionMode.Off, 0, 0, new TickMetricSnapshot(0, new Dictionary<string, double>()), [], [], null, 0);
}
