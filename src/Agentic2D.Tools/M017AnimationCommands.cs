using System.Text.Json;
using Agentic2D.Animation;
using Agentic2D.Input;
using Agentic2D.Rendering;
using Agentic2D.ScenarioRunner;
using Agentic2D.Validation;

namespace Agentic2D.Tools;

internal static class M017AnimationCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length >= 3 && args[0] == "content" && args[1] == "validate" && args[2] == "animations") return await ValidateAsync(args, output, error);
        if (args.Length >= 2 && args[0] == "animation" && args[1] == "inspect") return await InspectAsync(args, output, error);
        if (args.Length >= 2 && args[0] == "animation" && args[1] == "project") return await ProjectAsync(args, output, error);
        if (args.Length >= 3 && args[0] == "render" && args[1] == "project" && Option(args, "--scenario")?.StartsWith("animation-", StringComparison.Ordinal) == true) return await ProjectAsync(args, output, error);
        if (args.Length >= 3 && args[0] == "scenario" && args[1] == "run" && args[2].StartsWith("animation-", StringComparison.Ordinal)) return await ProjectAsync(args, output, error);
        return -1;
    }
    private static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private static async Task<int> ValidateAsync(string[] args, TextWriter output, TextWriter error)
    {
        var dir = Option(args, "--output"); if (dir is null) { await error.WriteLineAsync("missing required --output <directory>"); return 2; }
        var run = new AnimationCompiler().LoadAndCompileAll(); Directory.CreateDirectory(dir); var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(Path.Combine(dir, "result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.content-validation.result.v1", command = "content validate", scope = "animations", status = run.Passed ? "passed" : "failed", exitCode = run.Passed ? 0 : 1 }, options));
        await File.WriteAllTextAsync(Path.Combine(dir, "diagnostics.json"), JsonSerializer.Serialize(new { schema = "agentic2d.animation.diagnostics.v1", diagnostics = run.Diagnostics }, options));
        await File.WriteAllTextAsync(Path.Combine(dir, "validated-items.json"), JsonSerializer.Serialize(new { schema = "agentic2d.content-validation.items.v1", items = run.Animations.Select(x => new { id = x.Id, status = "passed" }) }, options));
        await output.WriteLineAsync($"content validate: {(run.Passed ? "passed" : "failed")}; result: {Path.Combine(dir, "result.json")}"); return run.Passed ? 0 : 1;
    }
    private static async Task<int> InspectAsync(string[] args, TextWriter output, TextWriter error)
    {
        var target = args.Length > 2 ? args[2] : null; var dir = Option(args, "--output"); if (target is null || dir is null) { await error.WriteLineAsync("animation inspect requires <animation-id-or-path> and --output"); return 2; }
        var run = new AnimationCompiler().LoadAndCompileAll(); var animation = run.Animations.SingleOrDefault(x => x.Id == target || target.EndsWith(x.Id + ".json", StringComparison.Ordinal)); if (animation is null) { await error.WriteLineAsync("animation definition was not found or failed validation"); return 1; }
        await AnimationArtifactWriter.WriteAsync(dir, animation, [], [], [], [], [], run.Diagnostics, "inspect", animation.Fingerprint); await output.WriteLineAsync("animation inspect: passed; output: " + dir); return 0;
    }
    private static async Task<int> ProjectAsync(string[] args, TextWriter output, TextWriter error)
    {
        var scenario = Option(args, "--scenario") ?? (args.Length >= 3 && args[0] == "scenario" ? args[2] : "animation-semantic-replay-smoke"); var dir = Option(args, "--output"); if (dir is null) { await error.WriteLineAsync("animation project requires --scenario and --output"); return 2; }
        var run = new AnimationCompiler().LoadAndCompileAll(); if (!run.Passed) { await error.WriteLineAsync("animation content validation failed"); return 1; }
        var semanticReplay = AnimationReplay.RecordAndReplay(scenario);
        var clearTicks = AnimationPolicy.ExplicitClearTicks(scenario);
        var execution = AnimationExecution.Run(run.Animations, scenario, semanticReplay.Recorded, clearTicks);
        var replay = scenario == "animation-semantic-replay-smoke" ? AnimationReplay.Run(run.Animations, scenario, execution, semanticReplay, clearTicks) : null;
        if (replay is not null && !replay.Equivalent) { await error.WriteLineAsync("semantic animation replay evidence differed"); return 1; }
        await AnimationArtifactWriter.WriteAsync(dir, execution.Primary, execution.Selections, execution.Playback, execution.Samples, execution.Markers, execution.Items, run.Diagnostics, scenario, execution.Fingerprint);
        if (replay is not null) await File.WriteAllTextAsync(Path.Combine(dir, "animation-replay.json"), JsonSerializer.Serialize(replay, new JsonSerializerOptions { WriteIndented = true }));
        if (args.Length >= 2 && args[0] == "scenario") { await File.WriteAllTextAsync(Path.Combine(dir, "result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.scenario-result.v1", status = "passed", scenarioId = scenario, animationFingerprint = execution.Fingerprint }, new JsonSerializerOptions { WriteIndented = true })); await File.WriteAllTextAsync(Path.Combine(dir, "events.jsonl"), string.Join(Environment.NewLine, execution.Markers.Select(x => JsonSerializer.Serialize(x)))); await File.WriteAllTextAsync(Path.Combine(dir, "diagnostics.json"), JsonSerializer.Serialize(new { diagnostics = run.Diagnostics })); }
        if (args.Length >= 2 && args[0] == "render") { var projection = new RenderProjectionService(); var baseFrame = projection.ProjectScenario("game/scenarios/smoke/" + scenario + ".json"); await RenderArtifactWriter.WriteAsync(dir, projection.WithAnimatedItems(baseFrame, execution.RenderItems.Where(x => x.Id.EndsWith(".animation.6", StringComparison.Ordinal)).ToArray())); }
        await output.WriteLineAsync("animation project: passed; output: " + dir); return 0;
    }
}

internal sealed record AnimationExecution(CompiledAnimation Primary, IReadOnlyList<object> Selections, IReadOnlyList<object> Playback, IReadOnlyList<object> Samples, IReadOnlyList<PresentationMarkerOccurrence> Markers, IReadOnlyList<object> Items, IReadOnlyList<RenderItem> RenderItems, string Fingerprint)
{
    public static AnimationExecution Run(IReadOnlyList<CompiledAnimation> definitions, string scenario, IReadOnlyList<InputFrame>? frames = null, IReadOnlySet<int>? explicitClearTicks = null)
    {
        var semanticFrames = (frames ?? []).ToDictionary(x => x.Tick);
        var player = definitions.Single(x => x.Id == "animation-definition.player.basic"); var npc = definitions.Single(x => x.Id == "animation-definition.npc.talkable-smoke"); var selections = new AnimationSelections(); var sampler = new AnimationSampler();
        var selectionEvidence = new List<object>(); var playback = new List<object>(); var samples = new List<object>(); var markers = new List<PresentationMarkerOccurrence>(); var items = new List<object>(); var renderItems = new List<RenderItem>(); AnimationSelection? previousBase = null; AnimationSelection? previousOverlay = null;
        for (var tick = 0; tick <= 6; tick++)
        {
            if (tick == 0) selections.SelectBaseClip("clip.idle.east", "selection.idle-east.initial", "initial-facing-east", tick);
            if (semanticFrames.TryGetValue(tick, out var frame))
            {
                var movingEast = frame.Vector2(InputIds.Move).X > 0;
                selections.SelectBaseClip(movingEast ? "clip.walk.east" : "clip.idle.east", movingEast ? "selection.walk-east.semantic" : "selection.idle-east.semantic", movingEast ? "semantic-action.move-east" : "semantic-action.move-stopped", tick);
                if (frame.Digital(InputIds.Interact).Phase == DigitalPhase.Pressed) selections.SelectOverlayClip("clip.interaction-pulse", "selection.interaction-pulse.frame-" + frame.FrameSequence, "semantic-action.interact-pressed", tick);
            }
            if (explicitClearTicks?.Contains(tick) == true) selections.ClearOverlayClip();
            var baseSample = sampler.Sample(player, selections.Base!, tick); SampledLayer? overlaySample = selections.Overlay is null ? null : sampler.Sample(npc, selections.Overlay, tick); selectionEvidence.Add(new { tick, @base = baseSample.Selection, overlay = overlaySample?.Selection, scenario }); playback.Add(new { tick, @base = baseSample.Playback, overlay = overlaySample?.Playback }); var composed = AnimationComposition.Compose(baseSample, overlaySample); samples.Add(new { tick, @base = baseSample.Patches, overlay = overlaySample?.Patches, composed });
            markers.AddRange(sampler.Markers(player, baseSample.Selection, previousBase is null ? null : tick - 1, tick, "entity.player")); if (overlaySample is not null) markers.AddRange(sampler.Markers(npc, overlaySample.Selection, previousOverlay is null ? null : tick - 1, tick, "entity.npc.talkable-smoke"));
            items.Add(new { schema = "agentic2d.animated-render-item.v1", sourceId = "entity.player", selectionInputFrame = semanticFrames.GetValueOrDefault(tick), visualDefinitionId = player.VisualDefinitionId, animationDefinitionId = player.Id, baseSelection = baseSample.Selection, overlaySelection = overlaySample?.Selection, playback = new { @base = baseSample.Playback, overlay = overlaySample?.Playback }, finalProperties = composed, runtimeTick = tick, fingerprint = AnimationCompiler.Fingerprint(new { tick, composed }) }); renderItems.AddRange(ToRenderItems(composed, tick)); previousBase = baseSample.Selection; previousOverlay = overlaySample?.Selection;
        }
        markers.Sort((a, b) => Comparer<(int, int, string)>.Default.Compare((a.LoopIteration, a.LocalMarkerTick, a.MarkerId), (b.LoopIteration, b.LocalMarkerTick, b.MarkerId))); var fingerprint = AnimationCompiler.Fingerprint(new { scenario, selections = selectionEvidence, playback, samples, markers, items }); return new(player, selectionEvidence, playback, samples, markers, items, renderItems, fingerprint);
    }
    private static IReadOnlyList<RenderItem> ToRenderItems(IReadOnlyList<SampledPresentationPatch> patches, int tick)
    {
        RenderItem Item(string entityId, string visualId, string partId, string defaultRegion, double baseX)
        {
            double Scalar(string property, double fallback) => patches.LastOrDefault(x => x.PartId == partId && x.Property == property)?.Scalar ?? fallback;
            var region = patches.LastOrDefault(x => x.PartId == partId && x.Property == "visual.region")?.RegionId ?? defaultRegion;
            var x = Scalar("visual.offset.x", 0); var y = Scalar("visual.offset.y", 0); var scaleX = Scalar("visual.scale.x", 1); var scaleY = Scalar("visual.scale.y", 1);
            var tint = new RenderColor((byte)Math.Round(255 * Scalar("visual.tint.red", 1)), (byte)Math.Round(255 * Scalar("visual.tint.green", 1)), (byte)Math.Round(255 * Scalar("visual.tint.blue", 1)), (byte)Math.Round(255 * Scalar("visual.opacity", 1)));
            return new RenderItem("render.runtime-entity." + entityId + "." + partId + ".animation." + tick, "runtime-entity", entityId, visualId, partId, "asset.render-atlas-smoke", region, new RenderRect(new RenderPoint(baseX + x, 0.5 + y), new RenderSize(0.8 * scaleX, 0.8 * scaleY)), "bottom-center", "entities", 0, "y", 0.5 + y, "map.interaction-smoke", "animation-presentation", tint);
        }
        return [Item("entity.player", "visual-definition.player.basic", "part.player", "region.player", 0.5), Item("entity.npc.talkable-smoke", "visual-definition.npc.talkable-smoke", "part.npc", "region.npc", 4)];
    }
}

internal sealed record AnimationReplayResult(bool Equivalent, string RecordedFramesFingerprint, string ReplayedFramesFingerprint, string SelectionsFingerprint, string SamplesFingerprint, string MarkersFingerprint, string AnimatedRenderFingerprint, string FinalRenderProjectionFingerprint);

internal sealed record M017SemanticReplay(IReadOnlyList<InputFrame> Recorded, IReadOnlyList<InputFrame> Replayed);

internal static class AnimationPolicy
{
    public static IReadOnlySet<int> ExplicitClearTicks(string scenario)
    {
        var path = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game/scenarios/smoke", scenario + ".json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("animationOperations", out var operations)
            ? operations.EnumerateArray().Where(x => x.GetProperty("operation").GetString() == "ClearOverlayClip").Select(x => x.GetProperty("tick").GetInt32()).ToHashSet()
            : [];
    }
}

internal static class AnimationReplay
{
    public static M017SemanticReplay RecordAndReplay(string scenario)
    {
        var source = new ScenarioSource { Id = scenario, Category = "smoke", Runtime = new ScenarioRuntimeSource(3, "spatial.continuous-kinematic-2d", "map.interaction-smoke", 16), Assertions = [new ScenarioAssertionSource("assert.input", "eventOccurred", EventType: "interaction.started")] };
        var recorded = M016InputScenarioExecutor.Execute(source);
        var replayed = M016InputScenarioExecutor.Execute(source, suppliedFrames: recorded.Recording.Frames);
        return new(recorded.Recording.Frames, replayed.Recording.Frames);
    }
    public static AnimationReplayResult Run(IReadOnlyList<CompiledAnimation> definitions, string scenario, AnimationExecution original, M017SemanticReplay semantic, IReadOnlySet<int> clearTicks)
    {
        var replayAnimation = AnimationExecution.Run(definitions, scenario, semantic.Replayed, clearTicks);
        var recordedFrames = AnimationCompiler.Fingerprint(semantic.Recorded); var replayedFrames = AnimationCompiler.Fingerprint(semantic.Replayed);
        var selections = AnimationCompiler.Fingerprint(original.Selections); var samples = AnimationCompiler.Fingerprint(original.Samples); var markers = AnimationCompiler.Fingerprint(original.Markers); var items = AnimationCompiler.Fingerprint(original.Items);
        var equivalent = recordedFrames == replayedFrames && original.Fingerprint == replayAnimation.Fingerprint;
        return new AnimationReplayResult(equivalent, recordedFrames, replayedFrames, selections, samples, markers, items, original.Fingerprint);
    }
}

internal static class AnimationArtifactWriter
{
    public static async Task WriteAsync(string output, CompiledAnimation animation, IReadOnlyList<object> selections, IReadOnlyList<object> playback, IReadOnlyList<object> samples, IReadOnlyList<PresentationMarkerOccurrence> markers, IReadOnlyList<object> items, IReadOnlyList<AnimationDiagnostic> diagnostics, string source, string fingerprint)
    {
        Directory.CreateDirectory(output); var indented = new JsonSerializerOptions { WriteIndented = true }; await File.WriteAllTextAsync(Path.Combine(output, "animation-result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.animation.result.v1", status = "passed", source, animationDefinitionId = animation.Id, fingerprint }, indented)); await File.WriteAllTextAsync(Path.Combine(output, "compiled-animation.json"), JsonSerializer.Serialize(animation, indented)); await Lines(Path.Combine(output, "animation-selections.jsonl"), selections); await Lines(Path.Combine(output, "animation-playback.jsonl"), playback); await Lines(Path.Combine(output, "animation-samples.jsonl"), samples); await Lines(Path.Combine(output, "animation-markers.jsonl"), markers); await Lines(Path.Combine(output, "animated-render-items.jsonl"), items); await File.WriteAllTextAsync(Path.Combine(output, "animation-diagnostics.json"), JsonSerializer.Serialize(new { schema = "agentic2d.animation.diagnostics.v1", diagnostics }, indented));
    }
    private static Task Lines<T>(string path, IEnumerable<T> items) => File.WriteAllTextAsync(path, string.Join(Environment.NewLine, items.Select(x => JsonSerializer.Serialize(x))));
}
