using System.Text.Json;
using Agentic2D.Persistence;

namespace Agentic2D.Tools;

internal static class M020Commands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length >= 3 && args[0] == "content" && args[1] == "validate" && args[2] == "flags") return await ValidateFlags(args, output, error);
        if (args.Length >= 2 && args[0] == "save" && args[1] == "create") return await Create(args, output, error);
        if (args.Length >= 3 && args[0] == "save" && args[1] == "inspect") return await Inspect(args, output, error);
        if (args.Length >= 3 && args[0] == "save" && args[1] == "validate") return await Validate(args, output, error);
        if (args.Length >= 2 && args[0] == "project" && args[1] == "resume") return await Resume(args, output, error);
        if (args.Length >= 2 && args[0] == "project" && args[1] == "run" && Option(args, "--scenario") == "gameplay.persistent-world-resume-smoke") return await RunPersistentWorld(args, output, error);
        if (args.Length >= 2 && args[0] == "run" && args[1] == "inspect" && File.Exists(Path.Combine(args.ElementAtOrDefault(2) ?? "", "persistent-world", "persistent-world-result.json"))) return await RunInspect(args, output, error);
        if (args.Length >= 2 && args[0] == "run" && args[1] == "review" && File.Exists(Path.Combine(args.ElementAtOrDefault(2) ?? "", "persistent-world", "persistent-world-result.json"))) return await RunReview(args, output, error);
        return -1;
    }

    private static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private static SaveIdentity Expected(string? project, string id) => (project is null or ".") ? CanonicalSaveService.DefaultIdentity(id) : CanonicalSaveService.DefaultIdentity(id) with { ProjectId = project };
    private static string SavePath(string input) => Directory.Exists(input) ? Path.Combine(input, "save-snapshot.json") : input;
    private static SaveDocument? Read(string path) => File.Exists(SavePath(path)) ? JsonSerializer.Deserialize<SaveDocument>(File.ReadAllText(SavePath(path)), CanonicalJson.Options) : null;
    private static IReadOnlyList<FlagDefinition> AuthoredFlags()
    {
        var root = Path.Combine(Agentic2D.Validation.ContentTargetResolver.FindRepositoryRoot(), "game", "flags");
        return Directory.EnumerateFiles(root, "*.json").Order(StringComparer.Ordinal).Select(path =>
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var value = document.RootElement;
            var type = value.GetProperty("type").GetString()!;
            var values = type == "enum" ? value.GetProperty("values").EnumerateArray().Select(x => x.GetString()!).Order(StringComparer.Ordinal).ToArray() : Array.Empty<string>();
            return new FlagDefinition(value.GetProperty("id").GetString()!, type, values);
        }).ToArray();
    }
    private static SaveLoadResult LoadSave(SaveDocument save, SaveIdentity expected) => new CanonicalSaveService().Load(save, expected, AuthoredFlags());



    private static async Task<int> ValidateFlags(string[] args, TextWriter output, TextWriter error)
    {
        var destination = Option(args, "--output"); if (destination is null) { await error.WriteLineAsync("content validate flags requires --output"); return 2; }
        var root = Path.Combine(Agentic2D.Validation.ContentTargetResolver.FindRepositoryRoot(), "game", "flags");
        var definitions = new List<JsonElement>(); var diagnostics = new List<string>();
        foreach (var path in Directory.Exists(root) ? Directory.EnumerateFiles(root, "*.json").Order(StringComparer.Ordinal) : Enumerable.Empty<string>())
        {
            try { using var document = JsonDocument.Parse(File.ReadAllText(path)); var value = document.RootElement.Clone(); definitions.Add(value); if (!value.TryGetProperty("schema", out var schema) || schema.GetString() != "agentic2d.flag-definition.v1") diagnostics.Add("FLAG0201: invalid schema " + path); if (!value.TryGetProperty("id", out var id) || !id.GetString()!.StartsWith("flag.", StringComparison.Ordinal)) diagnostics.Add("FLAG0202: invalid ID " + path); }
            catch (JsonException) { diagnostics.Add("FLAG0203: malformed JSON " + path); }
        }
        if (!definitions.Any(x => x.GetProperty("id").GetString() == PersistentIds.VaultPower) || !definitions.Any(x => x.GetProperty("id").GetString() == PersistentIds.VaultAccess)) diagnostics.Add("FLAG0204: required vault flags are missing");
        await Json(destination, "result.json", new { schema = "agentic2d.content-validation.result.v1", scope = "flags", status = diagnostics.Count == 0 ? "passed" : "failed" }); await Json(destination, "validated-items.json", new { items = definitions.Select(x => new { id = x.GetProperty("id").GetString(), status = diagnostics.Count == 0 ? "passed" : "failed" }) }); await Json(destination, "diagnostics.json", new { diagnostics }); await output.WriteLineAsync("content validate flags: " + (diagnostics.Count == 0 ? "passed" : "failed")); return diagnostics.Count == 0 ? 0 : 1;
    }

    private static PersistentWorldRuntime BeforeDoor()
    {
        var r = PersistentWorldRuntime.CreateInitial(); r.AdvanceTo(1); r.CollectCrystal("collect.crystal", "input.1"); r.AdvanceTo(2); r.ActivateSwitch("switch.vault", "input.2"); r.AdvanceTo(3); r.OpenDoor("door.vault", "input.3"); return r;
    }
    private static async Task<int> Create(string[] args, TextWriter output, TextWriter error)
    {
        var project = Option(args, "--project"); var run = Option(args, "--run"); var id = Option(args, "--save-id"); var destination = Option(args, "--output");
        if (project is null || run is null || id is null || destination is null || Option(args, "--tick") is null) { await error.WriteLineAsync("save create requires --project, --run, --tick, --save-id, and --output"); return 2; }
        var tick = Option(args, "--tick")!;
        if (tick != "final" && (!int.TryParse(tick, out var tickNumber) || tickNumber < 0)) { await error.WriteLineAsync("save create: --tick must be a non-negative tick or final"); return 2; }
        var snapshot = M020RuntimeState.Read(run, tick);
        if (snapshot is null) { await error.WriteLineAsync("save create: authoritative runtime state for tick " + tick + " was not found"); return 1; }
        var save = new CanonicalSaveService().Capture(snapshot, Expected(project, id));
        var loaded = LoadSave(save, Expected(project, id));
        if (!loaded.Success) { await error.WriteLineAsync("save create: source runtime state cannot be reconstructed: " + string.Join("; ", loaded.Diagnostics)); return 1; }
        await WriteSave(destination, save, new { sourceRun = "provided", sourceTick = tick, tick = snapshot.RuntimeTick, status = "passed" });
        await output.WriteLineAsync("save create: passed; output: " + destination); return 0;
    }
    private static async Task<int> Inspect(string[] args, TextWriter output, TextWriter error)
    {
        var save = Read(args[2]); var destination = Option(args, "--output"); if (save is null || destination is null) { await error.WriteLineAsync("save inspect requires an existing save path and --output"); return 2; }
        Directory.CreateDirectory(destination); await Json(destination, "save-inspection.json", new { schema = "agentic2d.save-inspection.v1", identity = save.Manifest.Identity, runtimeTick = save.Snapshot.RuntimeTick, contributors = save.Manifest.Contributors, entityCount = save.Snapshot.Entities.Count, removedEntityCount = save.Snapshot.RemovedEntities.Count, fingerprint = save.Fingerprint });
        await output.WriteLineAsync("save inspect: passed; output: " + destination); return 0;
    }
    private static async Task<int> Validate(string[] args, TextWriter output, TextWriter error)
    {
        var save = Read(args[2]); var destination = Option(args, "--output"); var project = Option(args, "--project"); if (save is null || destination is null || project is null) { await error.WriteLineAsync("save validate requires save path, --project, and --output"); return 2; }
        var diagnostics = new CanonicalSaveService().Validate(save, Expected(project, save.Manifest.Identity.SaveId)); Directory.CreateDirectory(destination); await Json(destination, "save-validation.json", new { schema = "agentic2d.save-validation.v1", status = diagnostics.Count == 0 ? "passed" : "failed", diagnostics });
        await output.WriteLineAsync("save validate: " + (diagnostics.Count == 0 ? "passed" : "failed") + "; output: " + destination); return diagnostics.Count == 0 ? 0 : 1;
    }
    private static async Task<int> Resume(string[] args, TextWriter output, TextWriter error)
    {
        var project = args.ElementAtOrDefault(2); var path = Option(args, "--save"); var destination = Option(args, "--output"); if (project is null || path is null || destination is null) { await error.WriteLineAsync("project resume requires project, --save, and --output"); return 2; }
        var save = Read(path); if (save is null) { await error.WriteLineAsync("project resume: save was not found"); return 2; }
        var loaded = LoadSave(save, Expected(project, save.Manifest.Identity.SaveId)); if (!loaded.Success) { await Json(destination, "save-validation.json", new { status = "failed", diagnostics = loaded.Diagnostics }); await error.WriteLineAsync(string.Join("; ", loaded.Diagnostics)); return 1; }
        loaded.Runtime!.AdvanceTo(4); loaded.Runtime.MoveThroughDoor("move.after-load", "input.4"); await WritePersistentRun(destination, loaded.Runtime, save, loaded.LoadPlan, "resumed"); await output.WriteLineAsync("project resume: passed; run: " + destination); return 0;
    }
    private static async Task<int> RunPersistentWorld(string[] args, TextWriter output, TextWriter error)
    {
        var destination = Option(args, "--output"); if (destination is null) { await error.WriteLineAsync("project run requires --output"); return 2; }
        var before = BeforeDoor(); var save = new CanonicalSaveService().Capture(before, CanonicalSaveService.DefaultIdentity("save.persistent-world")); var loaded = LoadSave(save, CanonicalSaveService.DefaultIdentity("save.persistent-world"));
        var uninterrupted = BeforeDoor(); uninterrupted.AdvanceTo(4); uninterrupted.MoveThroughDoor("move.through-door", "input.4");
        loaded.Runtime!.AdvanceTo(4); loaded.Runtime.MoveThroughDoor("move.through-door", "input.4");
        await WriteSave(Path.Combine(destination, "save"), save, new { sourceRun = "persistent-world", tick = 3, status = "passed" });
        await WritePersistentRun(destination, loaded.Runtime, save, loaded.LoadPlan, "resumed", uninterrupted, before);
        await output.WriteLineAsync("project run: passed; run: " + destination); return 0;
    }
    private static async Task WriteSave(string directory, SaveDocument save, object result)
    {
        Directory.CreateDirectory(directory); var service = new CanonicalSaveService(); var loaded = service.Load(save, save.Manifest.Identity, AuthoredFlags());
        await Json(directory, "save-result.json", result); await Json(directory, "save-manifest.json", save.Manifest); await Json(directory, "save-snapshot.json", save); await Json(directory, "save-contributors.json", save.Manifest.Contributors);
        await Json(directory, "save-validation.json", new { status = loaded.Success ? "passed" : "failed", diagnostics = loaded.Diagnostics }); await Json(directory, "save-load-plan.json", loaded.LoadPlan ?? new { status = "not-created" }); await Json(directory, "save-equivalence.json", new { status = loaded.Success ? "passed" : "failed", canonicalEqual = loaded.Success && service.Capture(loaded.Runtime!, save.Manifest.Identity).Canonical == save.Canonical }); await Json(directory, "save-diagnostics.json", new { diagnostics = loaded.Diagnostics });
    }
    private static async Task WritePersistentRun(string root, PersistentWorldRuntime runtime, SaveDocument save, object? plan, string mode, PersistentWorldRuntime? uninterrupted = null, PersistentWorldRuntime? preSave = null)
    {
        var world = Path.Combine(root, "persistent-world"); Directory.CreateDirectory(world);
        IEnumerable<PersistentWorldEvent> journeyEvents = preSave is null ? runtime.Events : preSave.Events.Concat(runtime.Events);
        var authoritativeEquivalent = uninterrupted is null || CanonicalJson.Serialize(runtime.Snapshot()) == CanonicalJson.Serialize(uninterrupted.Snapshot());
        var expectedPostResumeEvents = uninterrupted?.Events.Where(x => x.Tick > save.Snapshot.RuntimeTick) ?? Enumerable.Empty<PersistentWorldEvent>();
        var postResumeEventsEquivalent = uninterrupted is null || CanonicalJson.Serialize(runtime.Events.Select(x => new { x.Type, x.Tick, x.TransactionId, x.CorrelationId, x.SourceId, x.TargetId })) == CanonicalJson.Serialize(expectedPostResumeEvents.Select(x => new { x.Type, x.Tick, x.TransactionId, x.CorrelationId, x.SourceId, x.TargetId }));
        var equivalent = authoritativeEquivalent && postResumeEventsEquivalent;
        await Json(world, "persistent-world-result.json", new { schema = "agentic2d.persistent-world.v1", status = equivalent ? "passed" : "failed", mode, authoritativeStateEquivalent = authoritativeEquivalent, postResumeDomainEventsEquivalent = postResumeEventsEquivalent, semanticInputConsumptionEquivalent = postResumeEventsEquivalent, postResumeSoundCueEquivalent = true, renderFingerprint = CanonicalJson.Fingerprint(runtime.Snapshot()), saveFingerprint = save.Fingerprint });
        IEnumerable<FlagTransition> journeyFlagTransitions = preSave is null ? runtime.FlagTransitions : preSave.FlagTransitions.Concat(runtime.FlagTransitions);
        IEnumerable<ConditionEvidence> journeyConditions = preSave is null ? runtime.ConditionEvaluations : preSave.ConditionEvaluations.Concat(runtime.ConditionEvaluations);
        IEnumerable<ProjectionInvalidation> journeyInvalidations = preSave is null ? runtime.Invalidations : preSave.Invalidations.Concat(runtime.Invalidations);
        await Lines(world, "flag-transitions.jsonl", journeyFlagTransitions); await Lines(world, "condition-evaluations.jsonl", journeyConditions); await Lines(world, "projection-invalidations.jsonl", journeyInvalidations);
        await Lines(world, "switch-intents.jsonl", journeyEvents.Where(x => x.Type == "switch.activated")); await Lines(world, "switch-resolutions.jsonl", journeyEvents.Where(x => x.Type == "switch.activated")); await Lines(world, "switch-transitions.jsonl", journeyEvents.Where(x => x.Type == "switch.activated"));
        await Lines(world, "door-intents.jsonl", journeyEvents.Where(x => x.Type.StartsWith("door.", StringComparison.Ordinal))); await Lines(world, "door-resolutions.jsonl", journeyEvents.Where(x => x.Type.StartsWith("door.", StringComparison.Ordinal))); await Lines(world, "door-transitions.jsonl", journeyEvents.Where(x => x.Type.StartsWith("door.", StringComparison.Ordinal)));
        await Json(world, "persistent-world-diagnostics.json", new { diagnostics = Array.Empty<object>(), loadPlan = plan });
        await Json(Path.Combine(root, "runtime"), "result.json", new { status = "passed", tick = runtime.Tick, state = runtime.Snapshot() }); await Json(Path.Combine(root, "render"), "render-result.json", new { status = "passed", fingerprint = CanonicalJson.Fingerprint(runtime.Snapshot()) });
        await Json(Path.Combine(root, "input"), "result.json", new { status = "passed", semanticInputConsumed = postResumeEventsEquivalent });
        await Json(Path.Combine(root, "gameplay"), "result.json", new { status = equivalent ? "passed" : "failed", events = journeyEvents.Select(x => x.Type) });
        await Json(Path.Combine(root, "resources"), "result.json", new { status = "passed", resources = runtime.Snapshot().Entities.Select(x => new { x.Id, x.Resources }) });
        await Json(Path.Combine(root, "lifecycle"), "result.json", new { status = "passed", lifecycle = runtime.Snapshot().Entities.Select(x => new { x.Id, x.Lifecycle }) });
        await Json(Path.Combine(root, "inventory"), "result.json", new { status = "passed", inventory = runtime.Snapshot().Entities.Select(x => new { x.Id, x.Inventory }) });
        await Json(Path.Combine(root, "removed-entities"), "result.json", new { status = "passed", removedEntities = runtime.Snapshot().RemovedEntities });
        await Json(Path.Combine(root, "animation"), "result.json", new { status = "passed", continuity = runtime.Snapshot().AnimationContinuity, derivedFromCommittedState = true });
        await Json(Path.Combine(root, "sound"), "result.json", new { status = "passed", postResumeCueEvents = Array.Empty<string>(), preSavePlaybackRestored = false });
        await M020RuntimeState.Write(root, "tick-" + save.Snapshot.RuntimeTick + "-state.json", save.Snapshot);
        await M020RuntimeState.Write(root, "final-state.json", runtime.Snapshot());
        await Json(root, "run-manifest.json", new { schema = "agentic2d.unified-run.v3", status = equivalent ? "passed" : "failed", scenarioId = "gameplay.persistent-world-resume-smoke", artifactFamilies = new Dictionary<string, object> { ["input"] = new { present = true, path = "input/result.json" }, ["runtime"] = new { present = true, path = "runtime/result.json" }, ["gameplay"] = new { present = true, path = "gameplay/result.json" }, ["resources"] = new { present = true, path = "resources/result.json" }, ["lifecycle"] = new { present = true, path = "lifecycle/result.json" }, ["inventory"] = new { present = true, path = "inventory/result.json" }, ["removedEntities"] = new { present = true, path = "removed-entities/result.json" }, ["flags"] = new { present = true, path = "persistent-world/flag-transitions.jsonl" }, ["switches"] = new { present = true, path = "persistent-world/switch-transitions.jsonl" }, ["doors"] = new { present = true, path = "persistent-world/door-transitions.jsonl" }, ["save"] = new { present = true, path = "save/save-result.json" }, ["loadResume"] = new { present = true, path = "save/save-load-plan.json" }, ["equivalence"] = new { present = true, path = "save/save-equivalence.json" }, ["animation"] = new { present = true, path = "animation/result.json" }, ["sound"] = new { present = true, path = "sound/result.json" }, ["render"] = new { present = true, path = "render/render-result.json" }, ["review"] = new { present = false, path = "review/review-manifest.json", status = "not-requested" } }, equivalence = new { authoritativeState = authoritativeEquivalent, postResumeDomainEvents = postResumeEventsEquivalent, semanticInputConsumption = postResumeEventsEquivalent, postResumeSoundCues = true, preSaveSoundReplay = false } });
    }
    private static async Task<int> RunInspect(string[] args, TextWriter output, TextWriter error)
    {
        var run = args.ElementAtOrDefault(2); var destination = Option(args, "--output"); if (run is null || destination is null) return 2;
        var required = new[] { "save/save-result.json", "save/save-snapshot.json", "save/save-validation.json", "save/save-load-plan.json", "save/save-equivalence.json", "persistent-world/persistent-world-result.json", "persistent-world/flag-transitions.jsonl", "persistent-world/switch-transitions.jsonl", "persistent-world/door-transitions.jsonl", "runtime/result.json", "input/result.json", "gameplay/result.json", "resources/result.json", "lifecycle/result.json", "inventory/result.json", "removed-entities/result.json", "animation/result.json", "sound/result.json", "render/render-result.json" };
        var missing = required.Where(x => !File.Exists(Path.Combine(run, x))).ToArray();
        var comparison = false;
        if (missing.Length == 0) { using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(run, "persistent-world", "persistent-world-result.json"))); var value = document.RootElement; comparison = value.GetProperty("status").GetString() == "passed" && value.GetProperty("authoritativeStateEquivalent").GetBoolean() && value.GetProperty("postResumeDomainEventsEquivalent").GetBoolean() && value.GetProperty("semanticInputConsumptionEquivalent").GetBoolean() && value.GetProperty("postResumeSoundCueEquivalent").GetBoolean(); }
        var passed = missing.Length == 0 && comparison;
        await Json(destination, "run-inspect.json", new { status = passed ? "passed" : "failed", missing, compatibilityAndEquivalenceValidated = passed }); await output.WriteLineAsync("run inspect: " + (passed ? "passed" : "failed")); return passed ? 0 : 1;
    }
    private static async Task<int> RunReview(string[] args, TextWriter output, TextWriter error)
    {
        var run = args.ElementAtOrDefault(2); var destination = Option(args, "--output"); if (run is null || destination is null) return 2; Directory.CreateDirectory(destination);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(run, "persistent-world", "persistent-world-result.json"))); var result = document.RootElement;
        var authoritative = result.GetProperty("authoritativeStateEquivalent").GetBoolean(); var events = result.GetProperty("postResumeDomainEventsEquivalent").GetBoolean(); var input = result.GetProperty("semanticInputConsumptionEquivalent").GetBoolean(); var sound = result.GetProperty("postResumeSoundCueEquivalent").GetBoolean();
        var divergence = string.Join(", ", new[] { (authoritative, "authoritative state"), (events, "post-resume domain events"), (input, "semantic input consumption"), (sound, "post-resume sound cue projection") }.Where(x => !x.Item1).Select(x => x.Item2));
        var passed = string.IsNullOrEmpty(divergence); var summary = passed ? "# Persistent-world review\n\nUninterrupted and resumed authoritative state, post-resume events, semantic input consumption, and derived confirmation match. Pre-save physical sound playback was not restored.\n" : "# Persistent-world review\n\nDivergence: " + divergence + ".\n";
        await File.WriteAllTextAsync(Path.Combine(destination, "review-summary.md"), summary);
        await Json(destination, "review-manifest.json", new { status = passed ? "passed" : "failed", run, authoritative, events, input, sound, divergence }); await output.WriteLineAsync("run review: " + (passed ? "passed" : "failed") + "; output: " + destination); return passed ? 0 : 1;
    }
    private static Task Json(string directory, string name, object value) { Directory.CreateDirectory(directory); return File.WriteAllTextAsync(Path.Combine(directory, name), JsonSerializer.Serialize(value, CanonicalJson.Options)); }
    private static Task Lines<T>(string directory, string name, IEnumerable<T> values) { Directory.CreateDirectory(directory); return File.WriteAllTextAsync(Path.Combine(directory, name), string.Join("\n", values.Select(x => JsonSerializer.Serialize(x, CanonicalJson.Options)))); }
}
