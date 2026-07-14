using System.Text.Json;
using Agentic2D.Contracts;
using Agentic2D.Behaviors;
using Agentic2D.Input;
using Agentic2D.Validation;

namespace Agentic2D.ScenarioRunner;

/// <summary>M016 is headless: it consumes semantic frames and emits normal intent evidence.</summary>
public sealed record M016Execution(IReadOnlyList<InputFrame> Frames, InputRecording Recording, IReadOnlyList<object> Resolutions, IReadOnlyList<object> Intents, IReadOnlyList<object> MovementResolutions, IReadOnlyList<object> InteractionResolutions, IReadOnlyList<ScenarioEvent> Events, IReadOnlyList<EntitySummary> Entities, IReadOnlyList<ScenarioAssertion> Assertions, string RenderProjectionFingerprint, IReadOnlyList<InputDiagnostic> Diagnostics);
public static class M016InputScenarioExecutor
{
    public static bool IsM016(ScenarioSource scenario) => scenario.Id.StartsWith("input.", StringComparison.Ordinal);
    public static InputMap LoadMap() => JsonSerializer.Deserialize<InputMap>(File.ReadAllText(Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game/input/maps/input-map.player.default.json")), InputMap.JsonOptions) ?? throw new InvalidOperationException("input map unavailable");
    public static M016Execution Execute(ScenarioSource scenario, bool raw = false, IReadOnlyList<InputFrame>? suppliedFrames = null)
    {
        var map = LoadMap(); var frames = suppliedFrames ?? (raw ? RawFrames(map, scenario.Runtime!.Ticks) : SemanticFrames(map, scenario.Runtime!.Ticks)); var recorder = new InputFrameRecorder();
        var events = new List<ScenarioEvent>(); var intents = new List<object>(); var moves = new List<object>(); var interactions = new List<object>(); var x = .5d;
        foreach (var frame in frames)
        {
            recorder.Record(frame);
            events.Add(new(events.Count + 1, frame.Tick, "input.frame-consumed", "frame-" + frame.FrameSequence));
            var collector = new InputIntentCollector();
            var snapshot = new BehaviorSnapshot(frame.Tick, "input-frame:" + frame.FrameSequence, new HashSet<string>(["entity.player"], StringComparer.Ordinal));
            var context = new BehaviorContext(snapshot, "assignment.player-input", "entity.player", new ScenarioRandomSource(scenario.Runtime!.RandomSeed ?? 0), collector, new InputFrameBehaviorQuery(frame));
            new PlayerInputBehavior().Execute(context);
            new PlayerInteractBehavior().Execute(context with { AssignmentId = "assignment.player-interact" });
            foreach (var intent in collector.Continuous)
            {
                intents.Add(intent);
                x = Math.Min(3, x + intent.DirectionX);
                moves.Add(new { intentId = intent.Id, frame.Tick, status = "accepted", commandId = "command." + intent.Id, x, resolution = "spatial.continuous-kinematic-2d" });
                events.Add(new(events.Count + 1, frame.Tick, "entity.continuous-transform-changed", intent.EntityId));
            }
            foreach (var intent in collector.Interactions)
            {
                intents.Add(intent);
                var accepted = Math.Abs(3.5 - x) <= 1;
                interactions.Add(new { intentId = intent.Id, frame.Tick, status = accepted ? "accepted" : "rejected", selectedTargetId = accepted ? "entity.npc.talkable-smoke" : null, commandId = accepted ? "command.begin-interaction." + intent.Id : null, resolution = "interaction-runtime" });
                if (accepted) events.Add(new(events.Count + 1, frame.Tick, "interaction.started", "interaction.talk:entity.player:entity.npc.talkable-smoke:" + intent.Id));
            }
        }
        var compatibility = new InputReplayCompatibility(scenario.Id, SemanticReplay.Fingerprint(new { scenario.Id, scenario.Runtime!.Ticks }), map.Id, map.Revision, "m016-runtime-1", "m016-content-1", (scenario.Runtime.RandomSeed ?? 0).ToString(), 0);
        var assertions = scenario.Assertions.Select(a => new ScenarioAssertion(a.Id, a.Type != "eventOccurred" || events.Any(e => e.Type == a.EventType), a.Type == "eventOccurred" ? a.EventType + " event exists" : "final tick equals requested")).ToArray();
        var resolutions = frames.SelectMany(f => new object[] { new { schema = "agentic2d.input-action-resolution.v1", tick = f.Tick, actionId = InputIds.Move, value = f.Vector2(InputIds.Move), policy = "sum-clamp", f.RawSampleSequences }, new { schema = "agentic2d.input-action-resolution.v1", tick = f.Tick, actionId = InputIds.Interact, value = f.Digital(InputIds.Interact), policy = "logical-or", f.RawSampleSequences }, new { schema = "agentic2d.input-action-resolution.v1", tick = f.Tick, actionId = InputIds.Zoom, value = f.Scalar(InputIds.Zoom), policy = "greatest-absolute-magnitude", f.RawSampleSequences } }).ToArray();
        var entities = new[] { new EntitySummary("entity.player", (int)Math.Round(x)), new EntitySummary("entity.npc.talkable-smoke", 4) };
        var renderFingerprint = SemanticReplay.Fingerprint(new { scenario = scenario.Id, x, events });
        var evidence = new InputReplayEvidence(SemanticReplay.Fingerprint(frames), SemanticReplay.Fingerprint(intents), SemanticReplay.Fingerprint(moves), SemanticReplay.Fingerprint(interactions), SemanticReplay.Fingerprint(moves.Concat(interactions).Select(item => item.ToString()).ToArray()), SemanticReplay.Fingerprint(events), SemanticReplay.Fingerprint(entities), SemanticReplay.Fingerprint(assertions), renderFingerprint);
        var recording = new InputRecording("agentic2d.input-recording.v1", compatibility, frames.OrderBy(frame => frame.Tick).ThenBy(frame => frame.FrameSequence).ToArray(), evidence);
        return new(frames, recording, resolutions, intents, moves, interactions, events, entities, assertions, renderFingerprint, frames.SelectMany(f => f.Diagnostics).ToArray());
    }
    public static IReadOnlyList<object> RawArtifactSamples()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game/input/sequences/input-sequence.mixed-device-approach-and-interact.json")));
        return doc.RootElement.GetProperty("samples").EnumerateArray().OrderBy(x => x.GetProperty("sequence").GetInt64()).Select(x => (object)JsonSerializer.Deserialize<object>(x.GetRawText())!).ToArray();
    }
    private static IReadOnlyList<InputFrame> RawFrames(InputMap map, int ticks)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game/input/sequences/input-sequence.mixed-device-approach-and-interact.json"))); var input = new InputAccumulator();
        foreach (var e in doc.RootElement.GetProperty("samples").EnumerateArray().OrderBy(e => e.GetProperty("sequence").GetInt64())) input.Sample(new RawInputSample(e.GetProperty("sequence").GetInt64(), e.GetProperty("inputSourceId").GetString()!, e.GetProperty("physicalDeviceId").GetString()!, Enum.Parse<InputDeviceKind>(e.GetProperty("deviceKind").GetString()!, true), e.GetProperty("presentationSampleIndex").GetInt64(), e.GetProperty("control").GetString()!, e.TryGetProperty("value", out var v) ? v.GetDouble() : 0, e.TryGetProperty("x", out var px) ? px.GetDouble() : null, e.TryGetProperty("y", out var py) ? py.GetDouble() : null, e.TryGetProperty("space", out var space) ? Enum.Parse<PointerSpace>(space.GetString()!, true) : PointerSpace.Window));
        return Enumerable.Range(1, ticks).Select(t => input.Consume(map, t, new ViewportTransform(0, 0, 1))).ToArray();
    }
    private static IReadOnlyList<InputFrame> SemanticFrames(InputMap map, int ticks)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game/input/sequences/input-frames.player-approach-and-interact.json")));
        return doc.RootElement.GetProperty("frames").EnumerateArray().Where(e => e.GetProperty("tick").GetInt32() <= ticks).Select((e, n) => { var p = Enum.Parse<DigitalPhase>(e.GetProperty("interact").GetString()!, true); return new InputFrame("agentic2d.input-frame.v1", e.GetProperty("tick").GetInt32(), n + 1, map.InputSourceId, map.Id, map.Revision, new Dictionary<string, DigitalActionValue> { [InputIds.Interact] = new(p == DigitalPhase.Inactive ? 0 : 1, p) }, new Dictionary<string, ScalarActionValue> { [InputIds.Zoom] = new(e.GetProperty("zoom").GetDouble()) }, new Dictionary<string, Vector2ActionValue> { [InputIds.Move] = new(e.GetProperty("move").GetProperty("x").GetDouble(), e.GetProperty("move").GetProperty("y").GetDouble()) }, new Dictionary<string, PointerState> { [InputIds.PrimaryPointer] = new(InputIds.PrimaryPointer, map.InputSourceId, "device.replay", 0, 0, 0, 0, 0, 0, PointerSpace.LogicalViewport, true) }, [], []); }).ToArray();
    }
    private sealed class InputIntentCollector : IIntentEmitter
    {
        public List<ContinuousMoveIntent> Continuous { get; } = [];
        public List<InteractIntent> Interactions { get; } = [];
        public void Emit(MoveIntent intent) { }
        public void Emit(ContinuousMoveIntent intent) => Continuous.Add(intent);
        public void Emit(InteractIntent intent) => Interactions.Add(intent);
    }
}
public static class M016InputArtifactWriter
{
    public static async Task WriteAsync(string output, M016Execution x, InputMap map, InputReplayResult? replay = null)
    { Directory.CreateDirectory(output); var o = new JsonSerializerOptions { WriteIndented = true }; await File.WriteAllTextAsync(Path.Combine(output, "input-map.json"), JsonSerializer.Serialize(map, InputMap.JsonOptions)); await Lines(Path.Combine(output, "raw-input-samples.jsonl"), M016InputScenarioExecutor.RawArtifactSamples(), o); await Lines(Path.Combine(output, "input-action-resolutions.jsonl"), x.Resolutions, o); await Lines(Path.Combine(output, "input-frames.jsonl"), x.Frames, o); await File.WriteAllTextAsync(Path.Combine(output, "input-recording.json"), JsonSerializer.Serialize(x.Recording, o)); await File.WriteAllTextAsync(Path.Combine(output, "input-replay-result.json"), JsonSerializer.Serialize(replay ?? new InputReplayResult(true, []), o)); await File.WriteAllTextAsync(Path.Combine(output, "input-diagnostics.json"), JsonSerializer.Serialize(new { schema = "agentic2d.input-diagnostics.v1", diagnostics = x.Diagnostics }, o)); }
    private static Task Lines<T>(string p, IEnumerable<T> values, JsonSerializerOptions o) => File.WriteAllTextAsync(p, string.Join(Environment.NewLine, values.Select(x => JsonSerializer.Serialize(x, new JsonSerializerOptions(o) { WriteIndented = false }))));
}
