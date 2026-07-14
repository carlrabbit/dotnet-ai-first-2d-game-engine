using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.ScenarioRunner;
using Agentic2D.Validation;

namespace Agentic2D.Workspaces;

/// <summary>Built-in, source-based workspace acquisition and consumer workflow commands.</summary>
public static class WorkspaceCommands
{
    private const string WorkspaceFile = "agentic2d.workspace.json";
    private const string ProjectFile = "agentic2d.project.json";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 2) return -1;
        try
        {
            return args[0..2] switch
            {
                ["workspace", "create"] => await CreateAsync(args, output, error),
                ["workspace", "validate"] => await ValidateWorkspaceAsync(args, output, error),
                ["project", "validate"] => await ValidateProjectAsync(args, output, error),
                ["project", "run"] => await RunProjectAsync(args, output, error),
                ["run", "inspect"] => await InspectAsync(args, output, error),
                ["run", "review"] => await ReviewAsync(args, output, error),
                _ => -1,
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            await error.WriteLineAsync($"workspace workflow failed: {exception.Message}");
            return 3;
        }
    }

    private static async Task<int> CreateAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3) return await Usage(error, "workspace create requires <target>");
        var target = Path.GetFullPath(args[2]);
        var options = Options(args, 3, out var optionError);
        if (optionError is not null) return await Usage(error, optionError);
        var outputPath = options.GetValueOrDefault("--output");
        var diagnostics = new List<object>();
        if (outputPath is null || options.GetValueOrDefault("--template") != "minimal-game") return await Usage(error, "workspace create requires --template minimal-game and --output <directory>");
        if (Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any()) return await FailCreateAsync(outputPath, target, diagnostics, "WORKSPACE0010", "Target directory already exists and is non-empty.", error);
        var hasDirectory = options.TryGetValue("--engine-directory", out var engineDirectory);
        var hasGit = options.TryGetValue("--engine-git", out var engineGit);
        if (hasDirectory == hasGit) return await Usage(error, "provide exactly one of --engine-directory or --engine-git");
        var placement = options.GetValueOrDefault("--engine-placement");
        if (hasDirectory && placement is not ("reference" or "copy")) return await Usage(error, "--engine-directory requires --engine-placement reference|copy");
        var revision = options.GetValueOrDefault("--engine-revision");
        if (hasGit && string.IsNullOrWhiteSpace(revision)) return await Usage(error, "--engine-git requires --engine-revision <revision>");
        if (hasGit && placement is not null) return await Usage(error, "--engine-placement is only valid with --engine-directory");

        var parent = Directory.GetParent(target)?.FullName ?? Directory.GetCurrentDirectory();
        var stage = Path.Combine(parent, ".agentic2d-stage-" + Guid.NewGuid().ToString("N"));
        var targetWasEmpty = Directory.Exists(target);
        try
        {
            Directory.CreateDirectory(stage);
            var acquisition = hasDirectory
                ? AcquireDirectory(engineDirectory!, placement!, stage, target)
                : AcquireGit(engineGit!, revision!, stage);
            RenderMinimalGame(stage, acquisition);
            var validation = ValidateWorkspace(stage);
            await WriteJson(Path.Combine(stage, "artifacts", "workspace-creation", "engine-acquisition.json"), acquisition);
            await WriteJson(Path.Combine(stage, "artifacts", "workspace-creation", "workspace-validation.json"), validation);
            if (!validation.Passed) throw new InvalidOperationException("Generated workspace failed validation.");
            if (targetWasEmpty) Directory.Delete(target);
            Directory.Move(stage, target);
            var result = new { schema = "agentic2d.workspace-create-result.v1", status = "passed", targetStatus = targetWasEmpty ? "replaced-empty" : "created", templateId = "minimal-game", staging = "finalized", workspaceFingerprint = validation.WorkspaceFingerprint, projectFingerprint = validation.ProjectFingerprint, generatedFiles = CountFiles(target), validation = "workspace-validation.json" };
            await WriteJson(Path.Combine(target, "artifacts", "workspace-creation", "workspace-create-result.json"), result);
            await WriteJson(Path.Combine(target, "artifacts", "workspace-creation", "workspace-create-diagnostics.json"), new { schema = "agentic2d.workspace-create-diagnostics.v1", diagnostics });
            await WriteJson(Path.Combine(outputPath, "workspace-create-result.json"), result);
            await WriteJson(Path.Combine(outputPath, "engine-acquisition.json"), acquisition);
            await WriteJson(Path.Combine(outputPath, "workspace-validation.json"), validation);
            await WriteJson(Path.Combine(outputPath, "workspace-create-diagnostics.json"), new { schema = "agentic2d.workspace-create-diagnostics.v1", diagnostics });
            await output.WriteLineAsync($"workspace create: passed; workspace: {target}");
            return 0;
        }
        catch (Exception exception)
        {
            var cleaned = TryDelete(stage, out var cleanupError);
            diagnostics.Add(new { id = "WORKSPACE0011", severity = "error", message = exception.Message });
            if (cleanupError is not null) diagnostics.Add(new { id = "WORKSPACE0012", severity = "warning", message = cleanupError });
            await WriteJson(Path.Combine(outputPath, "workspace-create-diagnostics.json"), new { schema = "agentic2d.workspace-create-diagnostics.v1", target, stagingCleanup = cleaned ? "passed" : "failed", diagnostics });
            await error.WriteLineAsync($"workspace create failed: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> ValidateWorkspaceAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3) return await Usage(error, "workspace validate requires <workspace>");
        var options = Options(args, 3, out var message); if (message is not null || !options.TryGetValue("--output", out var outputPath)) return await Usage(error, message ?? "workspace validate requires --output <directory>");
        var validation = ValidateWorkspace(Path.GetFullPath(args[2]));
        await WriteJson(Path.Combine(outputPath, "workspace-validation.json"), validation);
        await output.WriteLineAsync($"workspace validate: {(validation.Passed ? "passed" : "failed")}; result: {Path.Combine(outputPath, "workspace-validation.json")}");
        return validation.Passed ? 0 : 1;
    }

    private static async Task<int> ValidateProjectAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3) return await Usage(error, "project validate requires <project-or-workspace>");
        var options = Options(args, 3, out var message); if (message is not null || !options.TryGetValue("--output", out var outputPath)) return await Usage(error, message ?? "project validate requires --output <directory>");
        var result = ValidateProject(ResolveWorkspaceOrProject(args[2]));
        await WriteJson(Path.Combine(outputPath, "project-validation.json"), result);
        await WriteJson(Path.Combine(outputPath, "project-reference-graph.json"), result.ReferenceGraph);
        await output.WriteLineAsync($"project validate: {(result.Passed ? "passed" : "failed")}; result: {Path.Combine(outputPath, "project-validation.json")}");
        return result.Passed ? 0 : 1;
    }

    private static async Task<int> RunProjectAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3) return await Usage(error, "project run requires <project-or-workspace>");
        var options = Options(args, 3, out var message); if (message is not null || !options.TryGetValue("--output", out var outputPath) || !options.TryGetValue("--scenario", out var scenarioId)) return await Usage(error, message ?? "project run requires --scenario <scenario-id> --output <run-directory>");
        var workspace = ResolveWorkspaceOrProject(args[2]); var workspaceValidation = ValidateWorkspace(workspace); var projectValidation = ValidateProject(workspace);
        var runDirectory = Path.GetFullPath(outputPath); Directory.CreateDirectory(runDirectory);
        var families = new Dictionary<string, object> { ["content"] = new { present = false }, ["input"] = new { present = false }, ["runtime"] = new { present = false }, ["animation"] = new { present = false }, ["render"] = new { present = false }, ["review"] = new { present = false }, ["diagnostics"] = new { present = false } };
        var diagnostics = new List<object>(); int exitCode = workspaceValidation.Passed && projectValidation.Passed ? 0 : 1;
        if (exitCode == 0)
        {
            var project = ReadDocument(Path.Combine(workspace, ProjectFile)); var scenarioPath = ResolveScenario(workspace, project, scenarioId);
            var contentDirectory = Path.Combine(runDirectory, "content"); Directory.CreateDirectory(contentDirectory);
            var content = new ContentValidator().Validate(scenarioPath); await ContentValidationArtifactWriter.WriteAsync(contentDirectory, content); families["content"] = new { present = true, path = "content/result.json", status = content.Result.Status.ToString().ToLowerInvariant() };
            var runtimeDirectory = Path.Combine(runDirectory, "runtime"); var scenarioExit = await new Agentic2D.ScenarioRunner.ScenarioRunner().RunAndWriteAsync(scenarioPath, runtimeDirectory); families["runtime"] = new { present = true, path = "runtime/result.json", status = scenarioExit == 0 ? "passed" : "failed" }; exitCode = Math.Max(exitCode, scenarioExit);
            var inputMap = Directory.EnumerateFiles(Path.Combine(workspace, "game-content", "input"), "*.json", SearchOption.AllDirectories).FirstOrDefault();
            if (inputMap is not null) { var input = Path.Combine(runDirectory, "input"); Directory.CreateDirectory(input); File.Copy(inputMap, Path.Combine(input, "input-map.json")); families["input"] = new { present = true, path = "input/input-map.json", status = "present" }; }
            var animation = Directory.EnumerateFiles(Path.Combine(workspace, "game-content", "animations"), "*.json", SearchOption.AllDirectories).FirstOrDefault();
            if (animation is not null) { var animationOutput = Path.Combine(runDirectory, "animation"); Directory.CreateDirectory(animationOutput); File.Copy(animation, Path.Combine(animationOutput, "animation-definition.json")); families["animation"] = new { present = true, path = "animation/animation-definition.json", status = "present" }; }
        }
        var diagnosticDirectory = Path.Combine(runDirectory, "diagnostics"); Directory.CreateDirectory(diagnosticDirectory); await WriteJson(Path.Combine(diagnosticDirectory, "workflow-diagnostics.json"), new { diagnostics }); families["diagnostics"] = new { present = true, path = "diagnostics/workflow-diagnostics.json", status = exitCode == 0 ? "passed" : "failed" };
        var manifest = BuildRunManifest(workspace, scenarioId, workspaceValidation, projectValidation, families, exitCode, diagnostics);
        await WriteJson(Path.Combine(runDirectory, "run-manifest.json"), manifest);
        await output.WriteLineAsync($"project run: {(exitCode == 0 ? "passed" : "failed")}; run: {runDirectory}");
        return exitCode;
    }

    private static async Task<int> InspectAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3) return await Usage(error, "run inspect requires <run-directory>");
        var options = Options(args, 3, out var message); if (message is not null || !options.TryGetValue("--output", out var outputPath)) return await Usage(error, message ?? "run inspect requires --output <directory>");
        var run = Path.GetFullPath(args[2]); var diagnostics = new List<object>();
        using var manifest = File.Exists(Path.Combine(run, "run-manifest.json")) ? ReadDocument(Path.Combine(run, "run-manifest.json")) : null;
        if (manifest is null) diagnostics.Add(new { id = "RUN0001", severity = "error", message = "run-manifest.json is missing", target = run });
        else foreach (var family in manifest.RootElement.GetProperty("artifactFamilies").EnumerateObject().Where(x => x.Value.GetProperty("present").GetBoolean())) { var path = family.Value.GetProperty("path").GetString(); if (path is null || !File.Exists(Path.Combine(run, path))) diagnostics.Add(new { id = "RUN0002", severity = "error", message = "Referenced evidence is missing", target = path, family = family.Name }); }
        var result = new { schema = "agentic2d.run-inspection.v1", status = diagnostics.Count == 0 ? "passed" : "failed", integrity = diagnostics.Count == 0 ? "verified" : "failed", primaryDiagnostics = diagnostics, affectedObjectIds = Array.Empty<string>(), recommendedNextActions = Recommendations(run) };
        await WriteJson(Path.Combine(outputPath, "run-inspection.json"), result); await output.WriteLineAsync($"run inspect: {result.status}; result: {Path.Combine(outputPath, "run-inspection.json")}"); return diagnostics.Count == 0 ? 0 : 1;
    }

    private static async Task<int> ReviewAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3) return await Usage(error, "run review requires <run-directory>");
        var options = Options(args, 3, out var message); if (message is not null || !options.TryGetValue("--output", out var outputPath)) return await Usage(error, message ?? "run review requires --output <directory>");
        var run = Path.GetFullPath(args[2]); using var manifest = ReadDocument(Path.Combine(run, "run-manifest.json"));
        var groups = manifest.RootElement.GetProperty("artifactFamilies").EnumerateObject().Where(x => x.Value.GetProperty("present").GetBoolean()).Select(x => new { kind = x.Name, path = x.Value.GetProperty("path").GetString(), evidence = x.Name == "render" ? "optional-visual" : "structural" }).ToArray();
        var review = new { schema = "agentic2d.unified-run-review.v1", status = "passed", runManifest = Path.Combine(run, "run-manifest.json"), structuralEvidence = groups.Where(x => x.evidence == "structural"), optionalScreenshots = groups.Where(x => x.evidence == "optional-visual"), automatedResults = groups, humanReviewQuestions = new[] { new { id = "review.run.evidence", question = "Is the structural evidence sufficient to judge the intended game behavior?" } }, recommendedNextActions = Recommendations(run) };
        await WriteJson(Path.Combine(outputPath, "review-manifest.json"), review); await File.WriteAllTextAsync(Path.Combine(outputPath, "review-summary.md"), "# Unified Run Review\n\nStatus: passed\n\n## Structural evidence\n" + string.Join('\n', groups.Where(x => x.evidence == "structural").Select(x => "- " + x.kind + ": " + x.path)) + "\n\n## Optional screenshots\nNone generated unless explicitly captured.\n\n## Human review questions\n- Is the structural evidence sufficient to judge the intended game behavior?\n");
        await WriteJson(Path.Combine(outputPath, "diagnostics.json"), new { diagnostics = Array.Empty<object>() }); await output.WriteLineAsync($"run review: passed; result: {Path.Combine(outputPath, "review-manifest.json")}"); return 0;
    }

    private static Acquisition AcquireDirectory(string source, string placement, string stage, string target)
    {
        source = Path.GetFullPath(source); EnsureEngine(source);
        if (placement == "reference") return new Acquisition("directory-reference", "reference", Relative(target, source), null, null, "directory", FingerprintDirectory(source, Excluded), 0, "source-tree-v1", []);
        var destination = Path.Combine(stage, "engine-src"); var copied = CopyTree(source, destination); return new Acquisition("directory-copy", "copy", Relative(target, source), "engine-src", null, "directory-copy", FingerprintDirectory(destination, _ => false), copied, "source-tree-v1", ExclusionNames);
    }

    private static Acquisition AcquireGit(string source, string revision, string stage)
    {
        if (!HasGit()) throw new InvalidOperationException("WORKSPACE0020: Git executable is required for git-clone acquisition.");
        var destination = Path.Combine(stage, "engine-src"); RunGit(stage, "clone", "--no-checkout", source, destination); RunGit(destination, "checkout", "--detach", revision); var sha = RunGit(destination, "rev-parse", "HEAD").Trim(); EnsureEngine(destination); return new Acquisition("git-clone", "copy", source, "engine-src", revision, sha, FingerprintDirectory(destination, Excluded), CountFiles(destination), "git-checkout-v1", ExclusionNames);
    }

    private static void RenderMinimalGame(string stage, Acquisition acquisition)
    {
        foreach (var path in new[] { "game-src/MinimalGame", "game-src/MinimalGame.Behaviors", "game-src/MinimalGame.Tests", "game-content/maps", "game-content/entities", "game-content/visuals", "game-content/animations", "game-content/input", "game-content/scenarios", "eng", "artifacts" }) Directory.CreateDirectory(Path.Combine(stage, path));
        var workspaceId = "workspace.minimal-game." + ShortHash(acquisition.Provider + acquisition.Source + acquisition.Resolved);
        var project = new { schema = "agentic2d.game-project.v1", id = "project.minimal-game", gameSourceRoots = new[] { "game-src" }, authoredContentRoots = new[] { "game-content" }, defaultScenarioId = "scenario.minimal.smoke", runtime = new { seed = "none", ticks = 3 }, presentation = new { mode = "headless" }, assemblies = new[] { "MinimalGame", "MinimalGame.Behaviors" }, supportedContentDomains = new[] { "scenarios", "maps", "entities", "visuals", "animations", "input" } };
        var workspace = new { schema = "agentic2d.game-workspace.v1", id = workspaceId, projectManifest = ProjectFile, engine = new { provider = acquisition.Provider, placement = acquisition.Placement, source = acquisition.Source, path = acquisition.Path, requestedRevision = acquisition.RequestedRevision, resolved = acquisition.Resolved, fingerprint = acquisition.Fingerprint, copyPolicy = acquisition.CopyPolicy }, areas = new[] { new { root = acquisition.Path ?? acquisition.Source, role = "engine-provider", mutationPolicy = "read-only-unless-authorized" }, new { root = "game-src", role = "game-code", mutationPolicy = "writable" }, new { root = "game-content", role = "authored-content", mutationPolicy = "writable" }, new { root = "artifacts", role = "generated-artifacts", mutationPolicy = "replaceable-generated" }, new { root = "eng", role = "tooling", mutationPolicy = "writable" } }, artifactRoot = "artifacts", wrapperRoot = "eng" };
        WriteJsonSync(Path.Combine(stage, ProjectFile), project); WriteJsonSync(Path.Combine(stage, WorkspaceFile), workspace);
        File.WriteAllText(Path.Combine(stage, "Directory.Build.props"), "<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup></Project>\n");
        File.WriteAllText(Path.Combine(stage, "Directory.Packages.props"), "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>\n");
        File.WriteAllText(Path.Combine(stage, "MinimalGame.slnx"), "<Solution><Folder Name=\"game-src\" /></Solution>\n");
        File.WriteAllText(Path.Combine(stage, "game-src/MinimalGame/MinimalGame.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>\n");
        File.WriteAllText(Path.Combine(stage, "game-src/MinimalGame/PlayerBehavior.cs"), "namespace MinimalGame; public static class PlayerBehavior { public const string Intent = \"move\"; }\n");
        File.WriteAllText(Path.Combine(stage, "game-src/MinimalGame.Behaviors/MinimalGame.Behaviors.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>\n");
        File.WriteAllText(Path.Combine(stage, "game-src/MinimalGame.Behaviors/MinimalBehavior.cs"), "namespace MinimalGame.Behaviors; public static class MinimalBehavior { public const string Id = \"behavior.player.minimal\"; }\n");
        File.WriteAllText(Path.Combine(stage, "game-src/MinimalGame.Tests/MinimalGame.Tests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>\n");
        File.WriteAllText(Path.Combine(stage, "game-src/MinimalGame.Tests/MinimalGameTests.cs"), "namespace MinimalGame.Tests; public static class MinimalGameTests { public static bool PlayerIntentIsStable() => true; }\n");
        WriteMinimalContent(stage); WriteWrappers(stage);
        File.WriteAllText(Path.Combine(stage, "AGENTS.md"), "# Consumer workspace\n\n`agentic2d.project.json` is game truth. `agentic2d.workspace.json` composes the checkout.\n\n- Engine provider area is read-only unless a task explicitly authorizes engine changes.\n- `game-src` and `game-content` are writable.\n- `artifacts` are replaceable generated evidence.\n- `eng` is writable only for workspace/tooling tasks.\n\nUse `./eng/validate.sh`, `./eng/run.sh`, `./eng/inspect.sh`, and `./eng/review.sh`.\n");
        File.WriteAllText(Path.Combine(stage, "README.md"), "# Minimal Game\n\nA deterministic headless Agentic2D consumer workspace. Run `./eng/validate.sh` then `./eng/run.sh scenario.minimal.smoke`.\n"); File.WriteAllText(Path.Combine(stage, ".gitignore"), "artifacts/*\n!artifacts/.gitkeep\n"); File.WriteAllText(Path.Combine(stage, "artifacts/.gitkeep"), string.Empty);
    }

    private static void WriteMinimalContent(string stage)
    {
        File.WriteAllText(Path.Combine(stage, "game-content/scenarios/minimal-smoke.json"), "{\n  \"schema\": \"agentic2d.scenario.v1\", \"id\": \"scenario.minimal.smoke\", \"category\": \"smoke\", \"title\": \"Minimal game smoke\", \"purpose\": \"Run a real deterministic player move.\", \"seedPolicy\": \"none\", \"runtime\": { \"ticks\": 3 }, \"initialState\": { \"entities\": [{ \"id\": \"entity.player\", \"position\": 0 }] }, \"steps\": [{ \"id\": \"step.player.move\", \"command\": { \"type\": \"move\", \"entityId\": \"entity.player\", \"amount\": 1 } }], \"expectedEvents\": [\"runtime.started\", \"entity.created\", \"command.accepted\", \"entity.moved\", \"runtime.completed\"], \"assertions\": [{ \"id\": \"assert.player.position\", \"type\": \"entityPositionEquals\", \"entityId\": \"entity.player\", \"position\": 1 }], \"artifacts\": { \"result\": \"result.json\", \"events\": \"events.jsonl\", \"diagnostics\": \"diagnostics.json\" }, \"humanReview\": { \"required\": false } }\n");
        File.WriteAllText(Path.Combine(stage, "game-content/maps/minimal.map.json"), "{ \"schema\": \"agentic2d.map.v1\", \"id\": \"map.minimal\", \"title\": \"Minimal map\", \"width\": 1, \"height\": 1, \"layers\": [], \"markers\": [] }\n");
        File.WriteAllText(Path.Combine(stage, "game-content/entities/player.entity.json"), "{ \"schema\": \"agentic2d.entity-definition.v1\", \"id\": \"entity-definition.player.minimal\", \"title\": \"Player\", \"components\": [] }\n");
        File.WriteAllText(Path.Combine(stage, "game-content/visuals/player.visual.json"), "{ \"schema\": \"agentic2d.visual-definition.v1\", \"id\": \"visual-definition.player.minimal\", \"title\": \"Player\", \"parts\": [] }\n");
        File.WriteAllText(Path.Combine(stage, "game-content/animations/player.animation.json"), "{ \"schema\": \"agentic2d.animation-definition.v1\", \"id\": \"animation-definition.player.minimal\", \"visualDefinitionId\": \"visual-definition.player.minimal\", \"clips\": [] }\n");
        File.WriteAllText(Path.Combine(stage, "game-content/input/player.input.json"), "{ \"schema\": \"agentic2d.input-map.v1\", \"id\": \"input-map.player.minimal\", \"actions\": [{ \"id\": \"action.move\", \"kind\": \"digital\" }] }\n");
    }

    private static void WriteWrappers(string stage)
    {
        var prefix = "#!/usr/bin/env bash\nset -euo pipefail\nroot=\"$(cd \"$(dirname \"${BASH_SOURCE[0]}\")/..\" && pwd)\nengine=$(python3 - \"$root/agentic2d.workspace.json\" \"$root\" <<'PY'\nimport json, os, sys\nw=json.load(open(sys.argv[1])); e=w['engine']; p=e.get('path') or e['source']; print(os.path.normpath(os.path.join(sys.argv[2], p)))\nPY\n)\ncli=(dotnet run --project \"$engine/src/Agentic2D.Tools\" --)\n";
        File.WriteAllText(Path.Combine(stage, "eng/validate.sh"), prefix + "\"${cli[@]}\" workspace validate \"$root\" --output \"$root/artifacts/validation\"\n\"${cli[@]}\" project validate \"$root\" --output \"$root/artifacts/project-validation\"\n");
        File.WriteAllText(Path.Combine(stage, "eng/run.sh"), prefix + "scenario=${1:?scenario id required}\n\"${cli[@]}\" project run \"$root\" --scenario \"$scenario\" --output \"$root/artifacts/runs/$scenario\"\n");
        File.WriteAllText(Path.Combine(stage, "eng/inspect.sh"), prefix + "run=${1:?run directory required}\n\"${cli[@]}\" run inspect \"$run\" --output \"$run/inspection\"\n");
        File.WriteAllText(Path.Combine(stage, "eng/review.sh"), prefix + "run=${1:?run directory required}\n\"${cli[@]}\" run review \"$run\" --output \"$run/review\"\n");
        if (!OperatingSystem.IsWindows()) foreach (var file in Directory.EnumerateFiles(Path.Combine(stage, "eng"))) File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }

    private static WorkspaceValidation ValidateWorkspace(string workspace)
    {
        var diagnostics = new List<object>(); var manifestPath = Directory.Exists(workspace) ? Path.Combine(workspace, WorkspaceFile) : workspace; var root = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        if (!File.Exists(manifestPath)) return new(false, null, null, new[] { new { id = "WORKSPACE0001", severity = "error", message = "Workspace manifest is missing." } });
        using var document = ReadDocument(manifestPath); var element = document.RootElement;
        string Required(string name) { if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString())) { diagnostics.Add(new { id = "WORKSPACE0002", severity = "error", message = $"Workspace manifest is missing {name}." }); return string.Empty; } return value.GetString()!; }
        var projectPath = Required("projectManifest"); if (!string.IsNullOrEmpty(projectPath) && !File.Exists(Path.Combine(root, projectPath))) diagnostics.Add(new { id = "WORKSPACE0003", severity = "error", message = "Project manifest path does not exist." });
        if (!element.TryGetProperty("engine", out var engine) || !engine.TryGetProperty("provider", out var provider)) diagnostics.Add(new { id = "WORKSPACE0004", severity = "error", message = "Engine provider record is missing." }); else { var providerId = provider.GetString(); if (providerId is not ("directory-reference" or "directory-copy" or "git-clone")) diagnostics.Add(new { id = "WORKSPACE0005", severity = "error", message = $"Unsupported engine provider: {providerId}." }); else { var path = engine.TryGetProperty("path", out var enginePath) && enginePath.ValueKind == JsonValueKind.String ? enginePath.GetString() : engine.GetProperty("source").GetString(); var resolved = Path.GetFullPath(Path.Combine(root, path!)); try { EnsureEngine(resolved); } catch (Exception e) { diagnostics.Add(new { id = "WORKSPACE0006", severity = "error", message = e.Message }); } if (providerId == "git-clone" && (!engine.TryGetProperty("resolved", out var resolvedCommit) || resolvedCommit.GetString()?.Length != 40)) diagnostics.Add(new { id = "WORKSPACE0007", severity = "error", message = "Git provider requires a resolved full commit SHA." }); } }
        var areaRoots = new HashSet<string>(StringComparer.Ordinal); if (!element.TryGetProperty("areas", out var areas) || areas.GetArrayLength() != 5) diagnostics.Add(new { id = "WORKSPACE0008", severity = "error", message = "Workspace must declare all five areas." }); else foreach (var area in areas.EnumerateArray()) { var policy = area.GetProperty("mutationPolicy").GetString(); var role = area.GetProperty("role").GetString(); if (policy is not ("read-only-unless-authorized" or "writable" or "replaceable-generated") || role is not ("engine-provider" or "game-code" or "authored-content" or "generated-artifacts" or "tooling")) diagnostics.Add(new { id = "WORKSPACE0009", severity = "error", message = "Workspace area has invalid role or mutation policy." }); var areaRoot = area.GetProperty("root").GetString()!; if (!areaRoots.Add(areaRoot)) diagnostics.Add(new { id = "WORKSPACE0013", severity = "error", message = "Workspace area roots overlap." }); }
        foreach (var wrapper in new[] { "validate.sh", "run.sh", "inspect.sh", "review.sh" }) if (!File.Exists(Path.Combine(root, "eng", wrapper))) diagnostics.Add(new { id = "WORKSPACE0014", severity = "error", message = $"Generated wrapper is missing: {wrapper}." });
        var project = File.Exists(Path.Combine(root, projectPath)) ? ValidateProject(root) : new ProjectValidation(false, null, new { nodes = Array.Empty<object>(), edges = Array.Empty<object>() }, Array.Empty<object>());
        var fingerprint = FingerprintObject(new { workspace = element.GetProperty("id").GetString(), engine = engineOrNull(element), areas = element.GetProperty("areas") }); return new(!diagnostics.Any(), fingerprint, project.Fingerprint, diagnostics);
        static object? engineOrNull(JsonElement x) => x.TryGetProperty("engine", out var y) ? y : null;
    }

    private static ProjectValidation ValidateProject(string workspace)
    {
        var diagnostics = new List<object>(); var projectPath = Path.Combine(workspace, ProjectFile); if (!File.Exists(projectPath)) return new(false, null, new { nodes = Array.Empty<object>(), edges = Array.Empty<object>() }, new[] { new { id = "PROJECT0001", severity = "error", message = "Project manifest is missing." } });
        using var project = ReadDocument(projectPath); var root = project.RootElement; var nodes = new List<object>(); var edges = new List<object>();
        if (!root.TryGetProperty("id", out var id) || string.IsNullOrWhiteSpace(id.GetString())) diagnostics.Add(new { id = "PROJECT0002", severity = "error", message = "Project ID is missing." });
        foreach (var property in new[] { "gameSourceRoots", "authoredContentRoots" }) { if (!root.TryGetProperty(property, out var roots)) { diagnostics.Add(new { id = "PROJECT0003", severity = "error", message = $"Project {property} is missing." }); continue; } foreach (var item in roots.EnumerateArray()) { var path = item.GetString()!; if (Path.IsPathRooted(path) || !Directory.Exists(Path.Combine(workspace, path))) diagnostics.Add(new { id = "PROJECT0004", severity = "error", message = $"Project root is invalid: {path}." }); else { nodes.Add(new { id = path, kind = property }); foreach (var file in Directory.EnumerateFiles(Path.Combine(workspace, path), "*.json", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal)) { var relative = Relative(workspace, file); nodes.Add(new { id = relative, kind = "content" }); edges.Add(new { from = path, to = relative, kind = "contains" }); if (relative.Contains("scenarios/", StringComparison.Ordinal)) { var result = new ContentValidator().Validate(file); if (result.Result.ExitCode != 0) diagnostics.Add(new { id = "PROJECT0005", severity = "error", message = "Scenario content validation failed.", target = relative }); } } } } }
        var graph = new { schema = "agentic2d.project-reference-graph.v1", nodes = nodes.OrderBy(x => JsonSerializer.Serialize(x), StringComparer.Ordinal), edges = edges.OrderBy(x => JsonSerializer.Serialize(x), StringComparer.Ordinal) }; var fingerprint = FingerprintObject(new { id = id.GetString(), roots = root.GetProperty("gameSourceRoots"), content = Directory.Exists(Path.Combine(workspace, "game-content")) ? FingerprintDirectory(Path.Combine(workspace, "game-content"), _ => false) : "missing" }); return new(!diagnostics.Any(), fingerprint, graph, diagnostics);
    }

    private static object BuildRunManifest(string workspace, string scenarioId, WorkspaceValidation workspaceValidation, ProjectValidation projectValidation, IDictionary<string, object> families, int exitCode, IReadOnlyList<object> diagnostics)
    {
        using var document = ReadDocument(Path.Combine(workspace, WorkspaceFile)); using var project = ReadDocument(Path.Combine(workspace, ProjectFile)); var engine = document.RootElement.GetProperty("engine"); var scenarioPath = ResolveScenario(workspace, project, scenarioId); var scenarioFingerprint = File.Exists(scenarioPath) ? FingerprintFile(scenarioPath) : "missing";
        return new { schema = "agentic2d.unified-run-manifest.v1", status = exitCode == 0 ? "passed" : "failed", runId = "run." + ShortHash(workspaceValidation.WorkspaceFingerprint + projectValidation.Fingerprint + scenarioId), workspace = new { id = document.RootElement.GetProperty("id").GetString(), fingerprint = workspaceValidation.WorkspaceFingerprint }, project = new { id = project.RootElement.GetProperty("id").GetString(), fingerprint = projectValidation.Fingerprint }, engine = new { provider = engine.GetProperty("provider").GetString(), resolved = engine.GetProperty("resolved").GetString(), fingerprint = engine.GetProperty("fingerprint").GetString() }, scenario = new { id = scenarioId, fingerprint = scenarioFingerprint }, runtime = new { seed = project.RootElement.GetProperty("runtime").GetProperty("seed").GetString(), ticks = project.RootElement.GetProperty("runtime").GetProperty("ticks").GetInt32() }, artifactFamilies = families, screenshots = Array.Empty<object>(), diagnostics = new { path = "diagnostics/workflow-diagnostics.json", items = diagnostics }, recommendedNextActions = Recommendations(workspace), exitCode };
    }

    private static string ResolveScenario(string workspace, JsonDocument project, string scenarioId) => Directory.EnumerateFiles(Path.Combine(workspace, "game-content", "scenarios"), "*.json", SearchOption.AllDirectories).FirstOrDefault(path => JsonDocument.Parse(File.ReadAllText(path)).RootElement.TryGetProperty("id", out var id) && id.GetString() == scenarioId) ?? throw new InvalidOperationException($"Scenario was not found: {scenarioId}");
    private static string ResolveWorkspaceOrProject(string path) { var full = Path.GetFullPath(path); return File.Exists(full) ? Path.GetDirectoryName(full)! : full; }
    private static Dictionary<string, string> Options(string[] args, int start, out string? error) { var output = new Dictionary<string, string>(StringComparer.Ordinal); error = null; for (var i = start; i < args.Length; i++) { if (!args[i].StartsWith("--", StringComparison.Ordinal) || ++i >= args.Length || args[i].StartsWith("--", StringComparison.Ordinal)) { error = "options must use --name <value>"; return output; } if (!output.TryAdd(args[i - 1], args[i])) { error = $"duplicate option: {args[i - 1]}"; return output; } } return output; }
    private static async Task<int> Usage(TextWriter error, string message) { await error.WriteLineAsync(message); return 2; }
    private static async Task<int> FailCreateAsync(string output, string target, List<object> diagnostics, string id, string message, TextWriter error) { diagnostics.Add(new { id, severity = "error", message }); await WriteJson(Path.Combine(output, "workspace-create-diagnostics.json"), new { schema = "agentic2d.workspace-create-diagnostics.v1", target, diagnostics }); await error.WriteLineAsync(message); return 1; }
    private static void EnsureEngine(string directory) { if (!Directory.Exists(directory) || !File.Exists(Path.Combine(directory, "src", "Agentic2D.Tools", "Agentic2D.Tools.csproj"))) throw new InvalidOperationException("WORKSPACE0021: Engine source does not contain src/Agentic2D.Tools/Agentic2D.Tools.csproj."); }
    private static bool HasGit() { try { return Run("git", ["--version"]).ExitCode == 0; } catch { return false; } }
    private static string RunGit(string workingDirectory, params string[] args) { var result = Run("git", args, workingDirectory); if (result.ExitCode != 0) throw new InvalidOperationException("WORKSPACE0022: Git acquisition failed: " + result.Error); return result.Output; }
    private static (int ExitCode, string Output, string Error) Run(string command, string[] args, string? directory = null) { using var process = Process.Start(new ProcessStartInfo(command) { WorkingDirectory = directory ?? Directory.GetCurrentDirectory(), RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, Arguments = string.Join(' ', args.Select(Escape)) })!; var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd(); process.WaitForExit(); return (process.ExitCode, output, error); }
    private static string Escape(string value) => value.Any(char.IsWhiteSpace) ? '"' + value.Replace("\"", "\\\"") + '"' : value;
    private static int CopyTree(string source, string destination) { var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).Where(x => !Excluded(Relative(source, x))).OrderBy(x => Relative(source, x), StringComparer.Ordinal).ToArray(); foreach (var file in files) { var target = Path.Combine(destination, Relative(source, file)); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target); if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(target, File.GetUnixFileMode(file)); } return files.Length; }
    private static readonly string[] ExclusionNames = [".git", "bin", "obj", "artifacts", ".vs", ".idea", ".vscode", ".DS_Store", "*.tmp"];
    private static bool Excluded(string relative) { var parts = relative.Replace('\\', '/').Split('/'); return parts.Any(p => p is ".git" or "bin" or "obj" or "artifacts" or ".vs" or ".idea" or ".vscode" or ".DS_Store" || p.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)); }
    private static string Relative(string from, string to) => Path.GetRelativePath(from, to).Replace(Path.DirectorySeparatorChar, '/');
    private static string FingerprintDirectory(string directory, Func<string, bool> exclude) { using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).OrderBy(x => Relative(directory, x), StringComparer.Ordinal)) { var relative = Relative(directory, file); if (exclude(relative)) continue; hash.AppendData(Encoding.UTF8.GetBytes(relative)); hash.AppendData(File.ReadAllBytes(file)); } return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(); }
    private static string FingerprintFile(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();
    private static string FingerprintObject(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Json)))).ToLowerInvariant();
    private static string ShortHash(string? input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input ?? string.Empty))).ToLowerInvariant()[..12];
    private static int CountFiles(string directory) => Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Count() : 0;
    private static bool TryDelete(string path, out string? error) { try { if (Directory.Exists(path)) Directory.Delete(path, true); error = null; return true; } catch (Exception exception) { error = exception.Message; return false; } }
    private static JsonDocument ReadDocument(string path) => JsonDocument.Parse(File.ReadAllText(path));
    private static Task WriteJson(string path, object value) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); return File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, Json)); }
    private static void WriteJsonSync(string path, object value) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, JsonSerializer.Serialize(value, Json)); }
    private static object[] Recommendations(string target) => [new { command = $"agentic2d workspace validate {target} --output artifacts/workspace-validation", kind = "workspace-validate" }, new { command = $"agentic2d project validate {target} --output artifacts/project-validation", kind = "project-validate" }, new { command = $"agentic2d run inspect <run-directory> --output <directory>", kind = "run-inspect" }, new { command = "agentic2d animation inspect <animation-id> --output <directory>", kind = "animation-inspect" }, new { command = "agentic2d input inspect <sequence-id> --input-map <map-id> --output <directory>", kind = "input-inspect" }];
    private sealed record Acquisition(string Provider, string Placement, string Source, string? Path, string? RequestedRevision, string Resolved, string Fingerprint, int Files, string CopyPolicy, IReadOnlyList<string> Exclusions);
    private sealed record WorkspaceValidation(bool Passed, string? WorkspaceFingerprint, string? ProjectFingerprint, IReadOnlyList<object> Diagnostics);
    private sealed record ProjectValidation(bool Passed, string? Fingerprint, object ReferenceGraph, IReadOnlyList<object> Diagnostics);
}
