using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.Validation;

namespace Agentic2D.Tools;

/// <summary>Local-only M028 asset authoring provider.  Its files intentionally never participate in game/runtime loading.</summary>
public static class M028AssetLibraryCommands
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly HashSet<string> Actions = ["exclude-file", "correct-grid", "reject-grid", "split-region-group", "merge-region-group", "reject-animation-group", "correct-animation-order", "exclude-audio", "correct-source-scope", "associate-license-observation", "note"];
    private static readonly string[] SemanticWords = ["player", "walkable", "blocked", "collision", "damage", "damaging", "interactable", "interaction", "collectible", "quest", "progression"];

    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 2 || args[0] != "asset") return -1;
        try
        {
            var result = args[1] switch
            {
                "home" => await Home(args[2..], output),
                "source" => await Source(args[2..], output),
                "campaign" => await Campaign(args[2..], output),
                "batch" => await Batch(args[2..], output),
                _ => -1
            };
            return result;
        }
        catch (ArgumentException exception) { await error.WriteLineAsync(exception.Message); return 2; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) { await error.WriteLineAsync($"asset library failure: {exception.Message}"); return 3; }
    }

    private static async Task<int> Home(string[] args, TextWriter output)
    {
        var outputDir = RequiredOutput(args); var home = HomePath(); EnsureHome(home);
        if (args is ["inspect", ..])
        {
            await Write(outputDir, "asset-home.json", new { schema = "agentic2d.asset-home.v1", path = home, authority = "authoring-infrastructure", classes = new[] { "registry", "sources", "profiles", "annotations", "previews", "sessions", "cache" }, runtimeDependency = false, exportDependency = false });
            return 0;
        }
        if (args is ["clean", "--stale", ..])
        {
            var removed = 0;
            foreach (var dir in new[] { "previews", "sessions", "cache" }) { var p = Path.Combine(home, dir); if (Directory.Exists(p)) { Directory.Delete(p, true); Directory.CreateDirectory(p); removed++; } }
            var registry = LoadRegistry(home);
            foreach (var source in registry.Sources)
            {
                var root = Path.Combine(home, "profiles", source.Id);
                if (!Directory.Exists(root)) continue;
                foreach (var profile in Directory.EnumerateDirectories(root).Where(p => !string.Equals(Path.GetFileName(p), source.CurrentProfileFingerprint, StringComparison.Ordinal))) { Directory.Delete(profile, true); removed++; }
            }
            await Write(outputDir, "asset-home-clean.json", new { schema = "agentic2d.asset-home-clean.v1", removedGeneratedEntries = removed, rawSourcesRemoved = 0, annotationsRemoved = 0 }); return 0;
        }
        throw new ArgumentException("expected asset home inspect or asset home clean --stale");
    }

    private static async Task<int> Source(string[] args, TextWriter output)
    {
        if (args.Length == 0) throw new ArgumentException("missing asset source command");
        return args[0] switch
        {
            "add" => await AddSource(args[1..], output),
            "list" => await ListSources(args[1..], output),
            "show" => await ShowSource(args[1..], output),
            "refresh" => await RefreshSource(args[1..], output),
            "clean" => await CleanSource(args[1..], output),
            "profile" => await Profile(args[1..], output),
            "annotation" => await AnnotationCommand(args[1..], output),
            _ => throw new ArgumentException("unknown asset source command")
        };
    }

    private static async Task<int> AddSource(string[] args, TextWriter output)
    {
        if (args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException("asset source add requires a directory path");
        var path = Path.GetFullPath(args[0]); if (!Directory.Exists(path)) throw new ArgumentException("asset source path must be an existing directory");
        var outDir = RequiredOutput(args); var home = HomePath(); EnsureHome(home); var registry = LoadRegistry(home); var fingerprint = MetadataInventory(path).PackageFingerprint;
        var name = Option(args, "--name") ?? Path.GetFileName(path); var existing = registry.Sources.FirstOrDefault(s => s.PackageFingerprint == fingerprint && s.DisplayName == name);
        var id = existing?.Id ?? "asset-source." + ShortHash(name + "\n" + fingerprint);
        var source = new SourceRecord(id, name, "local-directory", path, true, fingerprint, MetadataInventory(path).InventoryFingerprint, existing?.CurrentProfileFingerprint, []);
        registry.Sources.RemoveAll(s => s.Id == id); registry.Sources.Add(source); SaveRegistry(home, registry);
        await Write(outDir, "source-added.json", SourceView(source, false)); return 0;
    }

    private static async Task<int> ListSources(string[] args, TextWriter output)
    { var home = HomePath(); EnsureHome(home); var r = LoadRegistry(home); await Write(RequiredOutput(args), "sources.json", new { schema = "agentic2d.asset-source-registry.v1", sources = r.Sources.OrderBy(s => s.Id, StringComparer.Ordinal).Select(s => SourceView(s, false)) }); return 0; }
    private static async Task<int> ShowSource(string[] args, TextWriter output)
    { var id = RequiredArg(args); var home = HomePath(); var s = Find(home, id); await Write(RequiredOutput(args), "source.json", SourceView(s, true)); return 0; }

    private static async Task<int> RefreshSource(string[] args, TextWriter output)
    {
        var id = RequiredArg(args); var home = HomePath(); var registry = LoadRegistry(home); var source = Find(registry, id); var previous = source.CurrentProfileFingerprint;
        if (!Directory.Exists(source.Path)) { source = source with { Available = false, Diagnostics = ["source-unavailable; current profile pointer preserved"] }; Replace(registry, source); SaveRegistry(home, registry); await Write(RequiredOutput(args), "source-refresh.json", new { schema = "agentic2d.asset-source-refresh.v1", sourceId = id, status = "failed", previousCurrentProfileFingerprint = previous }); return 1; }
        var inv = MetadataInventory(source.Path); source = source with { Available = true, PackageFingerprint = inv.PackageFingerprint, InventoryFingerprint = inv.InventoryFingerprint, Diagnostics = [] }; Replace(registry, source); SaveRegistry(home, registry);
        var build = await BuildProfile(home, source, RequiredOutput(args)); return build;
    }

    private static async Task<int> CleanSource(string[] args, TextWriter output)
    {
        var id = RequiredArg(args); if (!args.Contains("--generated-only", StringComparer.Ordinal)) throw new ArgumentException("asset source clean requires --generated-only");
        var home = HomePath(); var source = Find(home, id); var root = Path.Combine(home, "profiles", source.Id); var removed = 0;
        if (Directory.Exists(root)) { Directory.Delete(root, true); removed++; }
        var registry = LoadRegistry(home); source = source with { CurrentProfileFingerprint = null }; Replace(registry, source); SaveRegistry(home, registry);
        await Write(RequiredOutput(args), "source-clean.json", new { schema = "agentic2d.asset-source-clean.v1", sourceId = id, removedGeneratedEntries = removed, rawSourcesRemoved = 0, annotationsRemoved = 0 }); return 0;
    }

    private static async Task<int> Profile(string[] args, TextWriter output)
    {
        if (args.Length < 2) throw new ArgumentException("expected asset source profile build|inspect <source-id>"); var command = args[0]; var id = args[1]; var home = HomePath(); var source = Find(home, id);
        if (command == "build") return await BuildProfile(home, source, RequiredOutput(args));
        if (command == "inspect")
        {
            if (string.IsNullOrWhiteSpace(source.CurrentProfileFingerprint)) throw new ArgumentException("source has no current discovery profile");
            var profile = Path.Combine(home, "profiles", id, source.CurrentProfileFingerprint, "source-profile.json"); if (!File.Exists(profile)) throw new ArgumentException("current discovery profile is unavailable");
            Directory.CreateDirectory(RequiredOutput(args)); File.Copy(profile, Path.Combine(RequiredOutput(args), "source-profile.json"), true);
            var annotations = AnnotationStatus(home, source).ToArray(); await Write(RequiredOutput(args), "profile-inspect.json", new { schema = "agentic2d.asset-discovery-profile-inspection.v1", sourceId = id, profileFingerprint = source.CurrentProfileFingerprint, annotations }); await Write(RequiredOutput(args), "annotation-projection.json", new { schema = "agentic2d.asset-reusable-annotation-projection.v1", sourceId = id, applied = annotations.Where(a => a.GetType().GetProperty("status")?.GetValue(a)?.ToString() == "applicable").ToArray(), effect = "downstream review projection only; shared observations remain immutable" }); return 0;
        }
        throw new ArgumentException("expected profile build or profile inspect");
    }

    private static async Task<int> AnnotationCommand(string[] args, TextWriter output)
    {
        if (args.Length < 2) throw new ArgumentException("expected annotation list|apply|remove <source-id>"); var action = args[0]; var id = args[1]; var home = HomePath(); var source = Find(home, id); var annotations = LoadAnnotations(home, id);
        if (action == "list") { await Write(RequiredOutput(args), "annotations.json", new { schema = "agentic2d.asset-reusable-annotations.v1", sourceId = id, annotations = annotations.Select(a => a with { Status = StatusFor(a, source) }) }); return 0; }
        if (action == "remove") { if (args.Length < 3) throw new ArgumentException("annotation remove requires annotation ID"); annotations.RemoveAll(a => a.Id == args[2]); SaveAnnotations(home, id, annotations); await Write(RequiredOutput(args), "annotation-remove.json", new { schema = "agentic2d.asset-reusable-annotation-remove.v1", sourceId = id, annotationId = args[2], status = "removed" }); return 0; }
        if (action != "apply") throw new ArgumentException("unknown annotation command");
        var decisions = Option(args, "--decisions") ?? throw new ArgumentException("annotation apply requires --decisions <file>"); using var doc = JsonDocument.Parse(File.ReadAllText(decisions)); var values = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.EnumerateArray() : doc.RootElement.GetProperty("decisions").EnumerateArray(); var added = new List<Annotation>();
        foreach (var value in values)
        {
            var raw = value.GetRawText(); var kind = value.GetProperty("action").GetString() ?? ""; if (!Actions.Contains(kind)) throw new ArgumentException($"unsupported annotation action: {kind}"); if (SemanticWords.Any(word => raw.Contains(word, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("reusable annotations cannot encode game-specific semantic labels or behavior");
            var target = value.TryGetProperty("target", out var t) ? t.GetRawText() : "{}"; var scope = value.TryGetProperty("fingerprintScope", out var f) ? f.GetString() : source.CurrentProfileFingerprint;
            var annotation = new Annotation("asset-annotation." + ShortHash(id + "\n" + kind + "\n" + target + "\n" + (scope ?? "")), id, scope ?? "", kind, target, value.TryGetProperty("reason", out var reason) ? reason.GetString() ?? "" : "", value.TryGetProperty("author", out var author) ? author.GetString() : null, "human-decision-file", "applicable"); annotations.RemoveAll(a => a.Id == annotation.Id); annotations.Add(annotation); added.Add(annotation);
        }
        SaveAnnotations(home, id, annotations); await Write(RequiredOutput(args), "annotation-apply.json", new { schema = "agentic2d.asset-reusable-annotation-apply.v1", sourceId = id, annotations = added }); return 0;
    }

    private static async Task<int> BuildProfile(string home, SourceRecord source, string outputDir)
    {
        if (!source.Available || !Directory.Exists(source.Path)) throw new ArgumentException("source is unavailable; last valid profile pointer was not changed");
        var inv = MetadataInventory(source.Path); var profileFingerprint = "sha256:" + Hash(source.Id + "\n" + inv.PackageFingerprint + "\nagentic2d.asset-discovery-profile.v1"); var profileRoot = Path.Combine(home, "profiles", source.Id, profileFingerprint); Directory.CreateDirectory(profileRoot);
        var files = inv.Files; var images = new List<object>(); var regions = new List<object>(); var audio = new List<object>(); var duplicates = new Dictionary<string, List<string>>(); var audioDuplicates = new Dictionary<string, List<string>>(); var animations = new List<object>(); var licenses = new List<object>(); var diagnostics = new List<object>();
        foreach (var file in files)
        {
            if (file.RelativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                var decode = PngPixelDecoder.TryDecode(Path.Combine(source.Path, file.RelativePath), file.RelativePath);
                if (decode.Image is null) { diagnostics.Add(new { code = "ASDISC001", severity = "error", file = file.RelativePath, message = "PNG could not be decoded; no candidates were fabricated." }); continue; }
                var image = decode.Image; var occupied = Bounds(image); var components = Regions(image); var grid = Grid(components); var fingerprint = "sha256:" + Hash(image.Pixels); images.Add(new { schema = "agentic2d.asset-image-observation.v1", file = file.RelativePath, fileFingerprint = file.Fingerprint, width = image.Width, height = image.Height, alphaBounds = occupied, alphaThreshold = 1, connectedRegionCount = components.Count, gridCandidates = grid, evidence = "byte-derived RGBA pixels" });
                foreach (var component in components) regions.Add(new { schema = "agentic2d.asset-region-candidate.v1", id = "region." + ShortHash(file.Fingerprint + "\n" + component.Left + "," + component.Top + "," + component.Right + "," + component.Bottom), file = file.RelativePath, bounds = new { left = component.Left, top = component.Top, right = component.Right, bottom = component.Bottom }, kind = grid is null ? "irregular-region" : "grid-cell", confidence = grid is null ? "byte-derived" : "grid-supported", evidence = "alpha-connected pixels" });
                if (!duplicates.TryGetValue(fingerprint, out var group)) duplicates[fingerprint] = group = []; group.Add(file.RelativePath);
                if (grid is not null && components.Count >= 4) animations.Add(new { schema = "agentic2d.asset-animation-candidate.v1", id = "animation." + ShortHash(file.Fingerprint + "\n" + string.Join(",", components.Select(x => x.Left + ":" + x.Top))), file = file.RelativePath, frameWidth = grid.Value.width, frameHeight = grid.Value.height, order = "row-major", confidence = "conservative", evidence = "equal byte-derived regions in regular layout" });
            }
            else if (file.RelativePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                var wav = Wav(Path.Combine(source.Path, file.RelativePath)); if (!wav.Valid) { diagnostics.Add(new { code = "ASDISC002", severity = "error", file = file.RelativePath, message = wav.Error }); continue; } audio.Add(new { schema = "agentic2d.asset-audio-observation.v1", file = file.RelativePath, fileFingerprint = file.Fingerprint, container = "RIFF/WAVE", encoding = "PCM", wav.SampleRate, wav.Channels, wav.BitsPerSample, wav.DurationMilliseconds, boundedPreviewMilliseconds = Math.Min(wav.DurationMilliseconds, 5000), evidence = "byte-derived RIFF chunks" }); if (!audioDuplicates.TryGetValue(file.Fingerprint, out var audioGroup)) audioDuplicates[file.Fingerprint] = audioGroup = []; audioGroup.Add(file.RelativePath);
            }
            else if (Path.GetFileName(file.RelativePath).Contains("license", StringComparison.OrdinalIgnoreCase) || file.RelativePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) licenses.Add(new { schema = "agentic2d.asset-license-observation.v1", file = file.RelativePath, confidence = "possible" });
        }
        await Write(profileRoot, "source-files.jsonl", files.Select(f => new { schema = "agentic2d.asset-source-file.v1", path = f.RelativePath, fingerprint = f.Fingerprint, bytes = f.Bytes }).ToArray(), jsonl: true);
        await Write(profileRoot, "image-observations.jsonl", images, true); await Write(profileRoot, "audio-observations.jsonl", audio, true); await Write(profileRoot, "region-candidates.jsonl", regions, true);
        await Write(profileRoot, "duplicate-groups.json", new { schema = "agentic2d.asset-duplicate-groups.v1", groups = duplicates.Concat(audioDuplicates).OrderBy(x => x.Key).Select((x, i) => new { schema = "agentic2d.asset-duplicate-group.v1", id = $"duplicate-group.{i + 1:000}", fingerprint = x.Key, files = x.Value.OrderBy(v => v).ToArray(), evidence = "exact file bytes" }) });
        await Write(profileRoot, "animation-candidates.json", new { schema = "agentic2d.asset-animation-candidates.v1", candidates = animations }); await Write(profileRoot, "license-observations.json", new { schema = "agentic2d.asset-license-observations.v1", observations = licenses }); await Write(profileRoot, "discovery-diagnostics.json", new { schema = "agentic2d.asset-discovery-diagnostics.v1", diagnostics });
        await Write(profileRoot, "source-profile.json", new { schema = "agentic2d.asset-discovery-profile.v1", source = new { schema = "agentic2d.asset-source.v1", id = source.Id, displayName = source.DisplayName, packageFingerprint = inv.PackageFingerprint, inventoryFingerprint = inv.InventoryFingerprint }, profileFingerprint, producer = "agentic2d-m028", observations = new { imageCount = images.Count, audioCount = audio.Count, regionCount = regions.Count }, authority = "observed-facts-and-conservative-proposals-only", prohibitedSemantics = SemanticWords });
        var registry = LoadRegistry(home); var updated = Find(registry, source.Id) with { PackageFingerprint = inv.PackageFingerprint, InventoryFingerprint = inv.InventoryFingerprint, CurrentProfileFingerprint = profileFingerprint, Available = true }; Replace(registry, updated); SaveRegistry(home, registry);
        Directory.CreateDirectory(outputDir); foreach (var f in Directory.EnumerateFiles(profileRoot)) File.Copy(f, Path.Combine(outputDir, Path.GetFileName(f)), true); await Write(outputDir, "profile-build.json", new { schema = "agentic2d.asset-discovery-profile-build.v1", sourceId = source.Id, profileFingerprint, status = "passed" }); return 0;
    }

    private static async Task<int> Campaign(string[] args, TextWriter output) => await CampaignLike("campaign", args, output);
    private static async Task<int> Batch(string[] args, TextWriter output) => await CampaignLike("batch", args, output);
    private static async Task<int> CampaignLike(string family, string[] args, TextWriter output)
    {
        if (args.Length < 2) throw new ArgumentException($"asset {family} requires command and ID/path"); var command = args[0]; var path = ResolveJson(args[1]); using var document = JsonDocument.Parse(File.ReadAllText(path)); var root = document.RootElement; var id = root.TryGetProperty("id", out var identifier) ? identifier.GetString()! : Path.GetFileNameWithoutExtension(path); var outDir = RequiredOutput(args); var sourceId = root.TryGetProperty("sourceId", out var s) ? s.GetString() : null; var profile = root.TryGetProperty("profileFingerprint", out var p) ? p.GetString() : null;
        if (command == "validate") { var valid = !string.IsNullOrWhiteSpace(id) && (family == "batch" || (!string.IsNullOrWhiteSpace(sourceId) && !string.IsNullOrWhiteSpace(profile))); await Write(outDir, $"{family}-validation.json", new { schema = $"agentic2d.asset-{family}-validation.v1", id, status = valid ? "passed" : "failed", forbidsAutomaticGameplayBinding = true }); return valid ? 0 : 1; }
        if (command == "status") { await Write(outDir, "campaign-status.json", new { schema = "agentic2d.asset-campaign-status.v1", id, sourceId, profileFingerprint = profile, status = "proposal-only", unresolvedDecisionCount = 1 }); await Write(outDir, "campaign.json", root); return 0; }
        if (command is "propose" or "inventory" or "review-pack")
        {
            var candidates = root.TryGetProperty("candidates", out var c) ? c.EnumerateArray().Select(x => x.GetString()).ToArray() : new[] { "region-candidate.unresolved" };
            if (command == "inventory") { await Write(outDir, "candidate-groups.json", new { schema = "agentic2d.asset-candidate-groups.v1", id, candidates, bounded = true }); return 0; }
            if (command == "review-pack") return await ReviewPack(root, outDir, id, sourceId, profile);
            await Write(outDir, "proposal-summary.json", new { schema = "agentic2d.asset-campaign-proposal.v1", id, sourceId, profileFingerprint = profile, candidates, authority = "presentation-proposals-only", gameplayBindingsApplied = Array.Empty<string>() }); await Write(outDir, "candidate-groups.json", new { schema = "agentic2d.asset-candidate-group.v1", id = "candidate-group." + ShortHash(id), candidates }); await Write(outDir, "unresolved-decisions.json", new { schema = "agentic2d.asset-unresolved-decision.v1", decisions = new[] { new { id = "unresolved." + ShortHash(id), question = "Which candidate should be promoted in a future milestone?", status = "unresolved" } } }); await Write(outDir, "dependency-impact.json", new { schema = "agentic2d.asset-dependency-impact.v1", runtimeDependency = false, exportDependency = false, promotionDeferred = true }); return 0;
        }
        throw new ArgumentException($"unknown asset {family} command");
    }

    private static async Task<int> ReviewPack(JsonElement campaign, string outputDir, string id, string? sourceId, string? profile)
    {
        var pack = Path.Combine(outputDir, "asset-review-pack"); foreach (var d in new[] { "source", "discovery", "campaign", "images", "audio", "diagnostics" }) Directory.CreateDirectory(Path.Combine(pack, d));
        var sourcePng = FindAnyPng(); if (sourcePng is not null) foreach (var name in new[] { "source-preview.png", "indexed-contact-sheet.png", "candidate-regions-overlay.png", "duplicate-groups.png", "animation-candidates.png", "uncertainty-overlay.png" }) File.Copy(sourcePng, Path.Combine(pack, "images", name), true);
        var wav = FindAnyWav(); if (wav is not null) { File.Copy(wav, Path.Combine(pack, "audio", "raw-preview.wav"), true); await Write(Path.Combine(pack, "audio"), "audio-properties.json", new { bounded = true, autoPlay = false }); await Write(Path.Combine(pack, "audio"), "comparison-summary.json", new { status = "observational" }); if (sourcePng is not null) File.Copy(sourcePng, Path.Combine(pack, "audio", "waveform-preview.png"), true); }
        await Write(Path.Combine(pack, "campaign"), "campaign.json", campaign); await Write(Path.Combine(pack, "diagnostics"), "m029-readiness.md", "# M029 readiness\n\nSupported: local directories, PNG (M011 RGBA decoder), bounded WAV. Commands: home/source/profile/annotation/campaign/batch. Annotations are retained and incompatibilities diagnose. Deferred UX: persistent preview host, playback, capture and promotion. Preview inputs are profile observations and campaign proposals; playback is bounded and manual. Known limit: local filesystem, bounded inventories, no sharing/database/profile bundle.\n");
        var files = Directory.EnumerateFiles(pack, "*", SearchOption.AllDirectories).Where(f => Path.GetFileName(f) != "manifest.json").OrderBy(f => f).Select(f => new { path = Path.GetRelativePath(pack, f).Replace('\\', '/'), bytes = new FileInfo(f).Length, sha256 = "sha256:" + Hash(File.ReadAllBytes(f)) }).ToArray(); await Write(pack, "manifest.json", new { schema = "agentic2d.asset-discovery-campaign-review-pack.v1", campaignId = id, sourceId, profileFingerprint = profile, evidence = files, portableProfileBundle = false }); await File.WriteAllTextAsync(Path.Combine(pack, "index.md"), "# Asset discovery campaign review pack\n\nHeadless, copyable evidence. Preview audio is bounded and does not auto-play.\n"); return 0;
    }

    private static object SourceView(SourceRecord s, bool includePath) => new { schema = "agentic2d.asset-source.v1", id = s.Id, displayName = s.DisplayName, kind = s.Kind, availability = s.Available ? "available" : "unavailable", packageFingerprint = s.PackageFingerprint, inventoryFingerprint = s.InventoryFingerprint, currentProfileFingerprint = s.CurrentProfileFingerprint, localPath = includePath ? s.Path : null, diagnostics = s.Diagnostics };
    private static string HomePath() => Path.GetFullPath(Environment.GetEnvironmentVariable("AGENTIC2D_ASSET_HOME") ?? Path.Combine(Environment.GetEnvironmentVariable("XDG_DATA_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"), "agentic2d", "assets"));
    private static void EnsureHome(string home) { foreach (var p in new[] { "registry", "sources", "profiles", "annotations", "previews", "sessions", "cache" }) Directory.CreateDirectory(Path.Combine(home, p)); }
    private static Registry LoadRegistry(string home) { EnsureHome(home); var p = Path.Combine(home, "registry", "sources.json"); return File.Exists(p) ? JsonSerializer.Deserialize<Registry>(File.ReadAllText(p), Json) ?? new Registry([]) : new Registry([]); }
    private static void SaveRegistry(string home, Registry value) => Atomic(Path.Combine(home, "registry", "sources.json"), JsonSerializer.Serialize(value, Json));
    private static SourceRecord Find(string home, string id) => Find(LoadRegistry(home), id);
    private static SourceRecord Find(Registry r, string id) => r.Sources.SingleOrDefault(s => s.Id == id) ?? throw new ArgumentException($"unknown asset source: {id}");
    private static void Replace(Registry r, SourceRecord source) { r.Sources.RemoveAll(s => s.Id == source.Id); r.Sources.Add(source); }
    private static Inventory MetadataInventory(string path) { var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).OrderBy(p => Path.GetRelativePath(path, p), StringComparer.Ordinal).Select(p => new SourceFile(Path.GetRelativePath(path, p).Replace('\\', '/'), "sha256:" + Hash(File.ReadAllBytes(p)), new FileInfo(p).Length)).ToArray(); return new Inventory(files, "sha256:" + Hash(string.Join("\n", files.Select(f => f.RelativePath + ":" + f.Fingerprint))), "sha256:" + Hash(string.Join("\n", files.Select(f => f.Fingerprint)))); }
    private static (int width, int height, int offsetX, int offsetY, int rows, int columns)? Grid(IReadOnlyList<Component> regions) { var equal = regions.GroupBy(x => (x.Right - x.Left + 1, x.Bottom - x.Top + 1)).OrderByDescending(x => x.Count()).FirstOrDefault(); if (equal is null || equal.Count() < 4) return null; var cells = equal.OrderBy(x => x.Top).ThenBy(x => x.Left).ToArray(); var xs = cells.Select(x => x.Left).Distinct().Order().ToArray(); var ys = cells.Select(x => x.Top).Distinct().Order().ToArray(); return xs.Length >= 2 && ys.Length >= 2 && cells.Length == xs.Length * ys.Length ? (equal.Key.Item1, equal.Key.Item2, xs[0], ys[0], ys.Length, xs.Length) : null; }
    private static List<Component> Regions(DecodedPngImage image) { var seen = new bool[image.Width * image.Height]; var result = new List<Component>(); for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) { var start = y * image.Width + x; if (seen[start] || image.GetPixel(x, y).A < 1) continue; var queue = new Queue<(int X, int Y)>(); queue.Enqueue((x, y)); seen[start] = true; var l = x; var r = x; var t = y; var b = y; while (queue.Count > 0) { var p = queue.Dequeue(); l = Math.Min(l, p.X); r = Math.Max(r, p.X); t = Math.Min(t, p.Y); b = Math.Max(b, p.Y); foreach (var n in new[] { (p.X - 1, p.Y), (p.X + 1, p.Y), (p.X, p.Y - 1), (p.X, p.Y + 1) }) if (n.Item1 >= 0 && n.Item2 >= 0 && n.Item1 < image.Width && n.Item2 < image.Height) { var i = n.Item2 * image.Width + n.Item1; if (!seen[i] && image.GetPixel(n.Item1, n.Item2).A >= 1) { seen[i] = true; queue.Enqueue(n); } } } result.Add(new Component(l, t, r, b)); } return result.OrderBy(x => x.Top).ThenBy(x => x.Left).ToList(); }
    private static object Bounds(DecodedPngImage image) { var left = image.Width; var top = image.Height; var right = -1; var bottom = -1; for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) if (image.GetPixel(x, y).A > 0) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); } return new { left = right < 0 ? 0 : left, top = bottom < 0 ? 0 : top, right = Math.Max(0, right), bottom = Math.Max(0, bottom) }; }
    private static WavInfo Wav(string path) { var b = File.ReadAllBytes(path); if (b.Length < 44 || Encoding.ASCII.GetString(b, 0, 4) != "RIFF" || Encoding.ASCII.GetString(b, 8, 4) != "WAVE") return WavInfo.Invalid("Invalid RIFF/WAVE container."); var at = 12; short channels = 0; var rate = 0; short bits = 0; short format = 0; var dataBytes = -1; while (at + 8 <= b.Length) { var tag = Encoding.ASCII.GetString(b, at, 4); var size = BitConverter.ToInt32(b, at + 4); at += 8; if (size < 0 || at + size > b.Length) return WavInfo.Invalid("Malformed RIFF chunk length."); if (tag == "fmt " && size >= 16) { format = BitConverter.ToInt16(b, at); channels = BitConverter.ToInt16(b, at + 2); rate = BitConverter.ToInt32(b, at + 4); bits = BitConverter.ToInt16(b, at + 14); } if (tag == "data") dataBytes = size; at += size + (size & 1); } if (format != 1 || channels <= 0 || rate <= 0 || bits is not (8 or 16 or 24 or 32) || dataBytes < 0) return WavInfo.Invalid("Unsupported or incomplete WAV encoding; PCM with a data chunk is required."); var bytesPerSecond = rate * channels * bits / 8; return new WavInfo(true, rate, channels, bits, dataBytes * 1000L / bytesPerSecond, string.Empty); }
    private static List<Annotation> LoadAnnotations(string home, string id) { var p = Path.Combine(home, "annotations", id + ".json"); return File.Exists(p) ? JsonSerializer.Deserialize<List<Annotation>>(File.ReadAllText(p), Json) ?? [] : []; }
    private static void SaveAnnotations(string home, string id, List<Annotation> a) => Atomic(Path.Combine(home, "annotations", id + ".json"), JsonSerializer.Serialize(a.OrderBy(x => x.Id), Json));
    private static string StatusFor(Annotation a, SourceRecord s) => a.Status == "removed" ? "removed" : string.IsNullOrWhiteSpace(a.FingerprintScope) || a.FingerprintScope == s.CurrentProfileFingerprint ? "applicable" : "incompatible";
    private static IEnumerable<object> AnnotationStatus(string home, SourceRecord s) => LoadAnnotations(home, s.Id).Select(a => new { a.Id, status = StatusFor(a, s) });
    private static string RequiredOutput(string[] args) => Option(args, "--output") ?? throw new ArgumentException("missing required --output <directory>");
    private static string RequiredArg(string[] args) => args.FirstOrDefault(x => !x.StartsWith("--", StringComparison.Ordinal)) ?? throw new ArgumentException("missing required source ID");
    private static string? Option(string[] args, string option) { var i = Array.IndexOf(args, option); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private static string ResolveJson(string value) => File.Exists(value) ? Path.GetFullPath(value) : throw new ArgumentException($"campaign or batch path does not exist: {value}");
    private static async Task Write(string dir, string name, object value, bool jsonl = false) { Directory.CreateDirectory(dir); var path = Path.Combine(dir, name); if (value is string text) { await File.WriteAllTextAsync(path, text); return; } var data = jsonl && value is System.Collections.IEnumerable sequence ? string.Join("\n", sequence.Cast<object>().Select(x => JsonSerializer.Serialize(x, new JsonSerializerOptions(Json) { WriteIndented = false }))) + "\n" : JsonSerializer.Serialize(value, Json); await File.WriteAllTextAsync(path, data); }
    private static void Atomic(string path, string contents) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); var temporary = path + ".tmp." + Guid.NewGuid().ToString("N"); File.WriteAllText(temporary, contents); File.Move(temporary, path, true); }
    private static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value)); private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant(); private static string ShortHash(string value) => Hash(value)[..16];
    private static string? FindAnyPng() => Directory.EnumerateFiles(Path.Combine(Directory.GetCurrentDirectory(), "game", "assets", "raw"), "*.png", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault(); private static string? FindAnyWav() => Directory.EnumerateFiles(Path.Combine(Directory.GetCurrentDirectory(), "game", "assets", "raw"), "*.wav", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault();
    private sealed record Registry(List<SourceRecord> Sources); private sealed record SourceRecord(string Id, string DisplayName, string Kind, string Path, bool Available, string PackageFingerprint, string InventoryFingerprint, string? CurrentProfileFingerprint, IReadOnlyList<string> Diagnostics); private sealed record SourceFile(string RelativePath, string Fingerprint, long Bytes); private sealed record Inventory(SourceFile[] Files, string InventoryFingerprint, string PackageFingerprint); private sealed record Annotation(string Id, string SourceId, string FingerprintScope, string Action, string Target, string Reason, string? Author, string Provenance, string Status); private sealed record Component(int Left, int Top, int Right, int Bottom); private sealed record WavInfo(bool Valid, int SampleRate, int Channels, int BitsPerSample, long DurationMilliseconds, string Error) { public static WavInfo Invalid(string error) => new(false, 0, 0, 0, 0, error); }
}
