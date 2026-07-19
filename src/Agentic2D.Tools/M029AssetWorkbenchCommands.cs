using System.Security.Cryptography;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic2D.Animation;
using Agentic2D.Rendering;
using Agentic2D.Sound;
using Agentic2D.Validation;

namespace Agentic2D.Tools;

/// <summary>
/// The M029 provider-side workbench.  This deliberately has no native-window dependency:
/// it persists the human-facing state and talks to the isolated raylib adapter through a
/// versioned preview protocol.  A window may disappear; this state must not.
/// </summary>
public static class M029AssetWorkbenchCommands
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly HashSet<string> Decisions = ["accept-proposal", "choose-alternative", "approve-with-corrections", "reject", "defer", "mark-unused", "split-group", "merge-group", "approve-group", "review-exceptions", "request-another-proposal", "mark-source-unsuitable"];
    private static readonly HashSet<string> Consequences = ["collision", "walkability", "damage", "interaction", "collection", "progression", "rendering", "render-layer", "animation-event", "sound-cue"];

    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 2 || args[0] != "asset") return -1;
        try
        {
            return args[1] switch
            {
                "workbench" => await Workbench(args[2..], output),
                "preview-host" => await PreviewHost(args[2..], output),
                "approved" => await Approved(args[2..], output),
                "rebuild" => await Rebuild(args[2..], output),
                "batch" when args.Length > 2 && args[2] is "apply-review" or "promotion-plan" or "promote" => await Batch(args[2..], output),
                _ => -1
            };
        }
        catch (ArgumentException exception) { await error.WriteLineAsync(exception.Message); return 2; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) { await error.WriteLineAsync("asset workbench failure: " + exception.Message); return 3; }
    }

    private static async Task<int> Workbench(string[] args, TextWriter output)
    {
        if (args.Length > 0 && args[0] == "ui") return await LaunchUi(args[1..], output);
        if (args.Length > 0 && args[0] == "resume") return await Open(args[1..], resume: true);
        if (args.Length > 0 && args[0] == "status") return await Status(args[1..]);
        if (args.Length > 0 && args[0] == "close") return await Close(args[1..]);
        return await Open(args, resume: false);
    }

    private static async Task<int> Open(string[] args, bool resume)
    {
        var output = RequiredOutput(args); var home = Home(); Ensure(home);
        var sessionId = Option(args, "--session") ?? (resume ? First(args) : null);
        var campaignPath = Option(args, "--campaign");
        Session session;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            session = LoadSession(home, sessionId!);
            var stale = ProfileHasChanged(home, session);
            session = session with
            {
                AliasGeneration = session.AliasGeneration + (resume ? 1 : 0),
                Status = stale ? "stale" : "active",
                Preview = session.Preview with { Status = stale ? "disconnected-stale-profile" : "connected", RestartCount = session.Preview.RestartCount + (resume ? 1 : 0) }
            };
        }
        else
        {
            if (string.IsNullOrWhiteSpace(campaignPath) || !File.Exists(campaignPath)) throw new ArgumentException("asset workbench requires --campaign <campaign-path> when creating a session");
            using var campaign = JsonDocument.Parse(await File.ReadAllTextAsync(campaignPath));
            var root = campaign.RootElement;
            var campaignId = Get(root, "id") ?? "campaign.unnamed";
            var sourceId = Get(root, "sourceId") ?? "asset-source.unknown";
            var profile = Get(root, "profileFingerprint") ?? "profile.unknown";
            var candidates = root.TryGetProperty("candidates", out var raw) && raw.ValueKind == JsonValueKind.Array ? raw.EnumerateArray().Select(x => x.GetString() ?? "candidate.unknown").Distinct(StringComparer.Ordinal).ToArray() : ["candidate.unresolved"];
            sessionId = "workbench-session." + Short(campaignId + "\n" + sourceId + "\n" + profile);
            session = new("agentic2d.asset-workbench-session.v1", sessionId, campaignId, sourceId, profile, candidates, candidates[0], null, 1, "active", DateTimeOffset.UtcNow.ToString("O"), new("agentic2d.asset-preview-ipc.hello.v1", PreviewIpcHost.EndpointForSession(home, sessionId), "connected", 0, "Agentic2D.DebugClient.Raylib", new[] { "render-projection", "animation", "sound-projection", "raylib-adapter", "no-auto-play" }), "review-decisions.jsonl", "promotion-plan.json", []);
        }
        SaveSession(home, session); var input = LoadInput(home, session.Id) with { MenuGeneration = session.AliasGeneration, ValidationMessage = null }; SaveInput(home, session.Id, input);
        if (args.Any(arg => arg is "--text" or "--paste" or "--composition" or "--backspace" or "--delete" or "--focus" or "--clear" or "--cancel" or "--select" or "--submit" or "--enter" or "--command" or "--input-command-file" or "--decision" or "--preview-restart"))
        {
            return await Dispatch(args, session, output);
        }
        var aliases = AliasMap(session); Save(home, session.Id, "aliases.json", aliases);
        await Emit(output, session, input, aliases, "opened");
        if (!resume && !args.Contains("--headless", StringComparer.Ordinal))
        {
            var previewOutput = Path.Combine(home, "sessions", session.Id, "preview");
            await PreviewHost([session.Id, "--output", previewOutput], TextWriter.Null);
            StartPreviewHost(session.Id, previewOutput, session.Preview.Endpoint);
            StartPreviewUi(previewOutput);
            return await LaunchUi([session.Id, "--commands", Path.Combine(home, "sessions", session.Id, "ui-input.jsonl")], TextWriter.Null);
        }
        return 0;
    }

    private static async Task<int> LaunchUi(string[] args, TextWriter output)
    {
        var home = Home(); var sessionId = First(args); var commandFile = Option(args, "--commands") ?? throw new ArgumentException("asset workbench ui requires --commands <input-command.jsonl>"); var sessionPath = SessionPath(home, sessionId);
        if (!File.Exists(sessionPath)) throw new ArgumentException("unknown workbench session: " + sessionId);
        var existingCommands = File.Exists(commandFile) ? File.ReadLines(commandFile).Count(line => !string.IsNullOrWhiteSpace(line)) : 0;
        var project = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "src", "Agentic2D.DebugClient.Raylib");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, WorkingDirectory = ContentTargetResolver.FindRepositoryRoot() };
        foreach (var argument in new[] { "run", "--no-build", "--project", project, "--", "asset-workbench", "--session", sessionPath, "--commands", Path.GetFullPath(commandFile) }) start.ArgumentList.Add(argument);
        if (Option(args, "--capture") is { } capture) { start.ArgumentList.Add("--capture"); start.ArgumentList.Add(Path.GetFullPath(capture)); }
        if (Option(args, "--frames") is { } frames) { start.ArgumentList.Add("--frames"); start.ArgumentList.Add(frames); }
        if (Option(args, "--initial-text") is { } text) { start.ArgumentList.Add("--initial-text"); start.ArgumentList.Add(text); }
        if (Option(args, "--message") is { } message) { start.ArgumentList.Add("--message"); start.ArgumentList.Add(message); }
        using var process = Process.Start(start) ?? throw new IOException("could not start isolated raylib workbench client");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new IOException("isolated raylib workbench client exited with " + process.ExitCode);
        var commands = File.Exists(commandFile) ? File.ReadLines(commandFile).Where(line => !string.IsNullOrWhiteSpace(line)).Skip(existingCommands).ToArray() : [];
        var uiOutput = Path.Combine(home, "sessions", sessionId, "ui-output");
        foreach (var command in commands)
        {
            var oneCommand = Path.Combine(home, "sessions", sessionId, "ui-command." + Guid.NewGuid().ToString("N") + ".jsonl");
            await File.WriteAllTextAsync(oneCommand, command + Environment.NewLine);
            try { await Dispatch(["--input-command-file", oneCommand, "--output", uiOutput], LoadSession(home, sessionId), uiOutput); }
            finally { if (File.Exists(oneCommand)) File.Delete(oneCommand); }
        }
        await output.WriteLineAsync("asset workbench ui exited with 0; applied " + commands.Length + " canonical command(s)"); return 0;
    }

    private static async Task<int> Status(string[] args)
    {
        var session = LoadSession(Home(), First(args)); var output = RequiredOutput(args); var input = LoadInput(Home(), session.Id); await Emit(output, session, input, AliasMap(session), "status"); return 0;
    }

    private static async Task<int> Close(string[] args)
    {
        var home = Home(); var session = LoadSession(home, First(args));
        var socket = PreviewIpcHost.SocketPath(session.Preview.Endpoint);
        if (File.Exists(socket))
        {
            try { using var _ = await PreviewIpcHost.SendAsync(session.Preview.Endpoint, new { schema = "agentic2d.asset-preview-ipc.request.v1", sessionId = session.Id, requestId = "preview-close", operation = "shutdown" }); }
            catch (IOException) { }
            catch (SocketException) { }
        }
        session = session with { Status = "closed", Preview = session.Preview with { Status = "shutdown" } }; SaveSession(home, session); await Emit(RequiredOutput(args), session, LoadInput(home, session.Id), AliasMap(session), "closed"); return 0;
    }

    // --text/--paste/--composition only update the visible buffer. --submit and --select
    // translate to precisely the same InputCommand and are the only routes to a choice.
    private static async Task<int> Dispatch(string[] args, Session session, string output)
    {
        var home = Home(); var input = LoadInput(home, session.Id);
        if (Option(args, "--text") is { } text) input = input with { TextBuffer = input.TextBuffer + text, CompositionActive = false, ValidationMessage = null };
        if (Option(args, "--paste") is { } paste) input = input with { TextBuffer = input.TextBuffer + paste, ValidationMessage = null };
        if (Option(args, "--composition") is { } composition) input = input with { TextBuffer = input.TextBuffer + composition, CompositionActive = true, ValidationMessage = null };
        if (args.Contains("--backspace", StringComparer.Ordinal) && input.TextBuffer.Length > 0) input = input with { TextBuffer = input.TextBuffer[..^1], ValidationMessage = null };
        if (args.Contains("--delete", StringComparer.Ordinal)) input = input with { TextBuffer = "", ValidationMessage = null };
        if (Option(args, "--focus") is { } focus) input = input with { Focus = focus, ValidationMessage = focus == "lost" ? "Focus changed; text remains editable and was not submitted." : input.ValidationMessage };
        if (args.Contains("--clear", StringComparer.Ordinal) || args.Contains("--cancel", StringComparer.Ordinal)) input = input with { TextBuffer = "", CompositionActive = false, ValidationMessage = null };

        var requestedGeneration = int.TryParse(Option(args, "--generation"), out var parsedGeneration) ? parsedGeneration : session.AliasGeneration;
        InputCommand? command = Option(args, "--input-command-file") is { } inputCommandFile ? ReadInputCommand(inputCommandFile, session.Id) : null;
        if (Option(args, "--select") is { } selected) command = new("agentic2d.asset-workbench-input-command.v1", "mouse-touch", selected, requestedGeneration);
        else if (args.Contains("--submit", StringComparer.Ordinal) || args.Contains("--enter", StringComparer.Ordinal)) command = new("agentic2d.asset-workbench-input-command.v1", args.Contains("--enter", StringComparer.Ordinal) ? "enter" : "submit-button", input.TextBuffer, requestedGeneration);
        else if (Option(args, "--command") is { } direct) command = new("agentic2d.asset-workbench-input-command.v1", "headless", direct, requestedGeneration);

        CanonicalAction? action = null;
        if (command is not null)
        {
            (action, var error) = Resolve(session, command);
            if (action is null) input = input with { ValidationMessage = error, LastSubmittedCanonicalCommand = null };
            else
            {
                input = input with { ValidationMessage = null, LastSubmittedCanonicalCommand = action.Command, TextBuffer = command.Source is "submit-button" or "enter" ? "" : input.TextBuffer, CompositionActive = false };
                if (action.Kind.StartsWith("decision", StringComparison.Ordinal)) await RecordDecision(home, session, action.Target!, action.Kind == "decision-presentation-only" ? "presentation-only" : action.Kind == "decision-confirm" ? "confirm" : null, null, null);
                else { session = ApplyNavigation(session, action); SaveSession(home, session); }
            }
        }
        SaveInput(home, session.Id, input);
        if (Option(args, "--decision") is { } decision && (command is null || action is not null) && action?.Kind.StartsWith("decision", StringComparison.Ordinal) != true) await RecordDecision(home, session, decision, Option(args, "--consequence"), Option(args, "--reason"), Option(args, "--alternative"));
        if (args.Contains("--preview-restart", StringComparer.Ordinal)) { session = LoadSession(home, session.Id) with { Preview = session.Preview with { Status = "reconnected", RestartCount = session.Preview.RestartCount + 1 } }; SaveSession(home, session); }
        await Emit(output, LoadSession(home, session.Id), LoadInput(home, session.Id), AliasMap(LoadSession(home, session.Id)), action is null ? "input-updated" : "action-executed", command, action);
        return 0;
    }

    private static (CanonicalAction? Action, string Error) Resolve(Session session, InputCommand command)
    {
        if (session.Status == "stale") return (null, "This session is stale because its source/profile changed. Refresh the campaign and list choices again.");
        if (command.Generation != session.AliasGeneration) return (null, "That numbered choice is stale. List choices again.");
        var value = command.Value.Trim(); if (value.Length == 0) return (null, "Enter a numbered choice or a bounded command.");
        if (value.StartsWith("open ", StringComparison.OrdinalIgnoreCase)) value = value[5..].Trim();
        if (int.TryParse(value, out var alias))
        {
            if (alias < 1 || alias > session.Candidates.Length) return (null, "That choice is stale or unavailable. List choices again.");
            return (new("open", session.Candidates[alias - 1], "open " + session.Candidates[alias - 1]), "");
        }
        if (value is "back" or "next" or "previous" or "recent" or "help" or "cancel") return (new(value, null, value), "");
        if (value.StartsWith("decision ", StringComparison.OrdinalIgnoreCase))
        {
            var terms = value[9..].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (terms.Length is < 1 or > 2 || !Decisions.Contains(terms[0])) return (null, "Use decision <guided-action> [confirm|presentation-only].");
            var kind = terms.Length == 1 ? "decision" : terms[1] == "confirm" ? "decision-confirm" : terms[1] == "presentation-only" ? "decision-presentation-only" : "";
            return string.IsNullOrEmpty(kind) ? (null, "Decision consequence must be confirm or presentation-only.") : (new(kind, terms[0], "decision " + terms[0] + (terms.Length == 2 ? " " + terms[1] : "")), "");
        }
        if (value.StartsWith("find ", StringComparison.OrdinalIgnoreCase) && value.Length is > 5 and <= 133) return (new("find", value[5..].Trim(), "find " + value[5..].Trim()), "");
        return (null, "Invalid workbench command. Use a visible number, open <number>, back, next, previous, find <text>, recent, help, or cancel.");
    }

    private static Session ApplyNavigation(Session session, CanonicalAction action) => action.Kind switch
    {
        "open" => session with { ActiveCandidate = action.Target },
        "next" => session with { ActiveCandidate = session.Candidates[(Array.IndexOf(session.Candidates, session.ActiveCandidate) + 1) % session.Candidates.Length] },
        "previous" => session with { ActiveCandidate = session.Candidates[(Array.IndexOf(session.Candidates, session.ActiveCandidate) + session.Candidates.Length - 1) % session.Candidates.Length] },
        _ => session
    };

    private static async Task RecordDecision(string home, Session session, string action, string? consequence, string? reason, string? alternative)
    {
        if (!Decisions.Contains(action)) throw new ArgumentException("unsupported workbench decision: " + action);
        if (!string.IsNullOrWhiteSpace(consequence) && consequence != "presentation-only" && consequence != "cancel" && consequence != "change" && consequence != "confirm") throw new ArgumentException("consequence response must be confirm, change, presentation-only, or cancel");
        var consequenceKind = ConsequenceKind(action == "approve-group" ? string.Join(" ", session.Candidates) : session.ActiveCandidate);
        var approval = action is "accept-proposal" or "choose-alternative" or "approve-with-corrections" or "approve-group";
        var impactful = approval && (consequenceKind is not null || !string.IsNullOrWhiteSpace(OptionFromReason(reason, "consequence")));
        if (impactful && consequence is not ("confirm" or "presentation-only" or "change" or "cancel")) throw new ArgumentException("impactful consequences require confirm, change, presentation-only, or cancel");
        if (consequence == "cancel") return;
        var path = Path.Combine(home, "sessions", session.Id, "review-decisions.jsonl"); var existing = File.Exists(path) ? File.ReadLines(path).Where(x => !string.IsNullOrWhiteSpace(x)).Count() : 0;
        var targets = action == "approve-group" ? session.Candidates : [session.ActiveCandidate ?? "candidate.unresolved"];
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        foreach (var target in targets)
        {
            var sequence = ++existing; var prior = LastDecision(path, target);
            var record = new { schema = "agentic2d.asset-review-decision.v1", id = "asset-review-decision." + Short(session.Id + "\n" + target + "\n" + sequence), sessionId = session.Id, campaignId = session.CampaignId, batchId = session.CampaignId, candidateId = target, sourceId = session.SourceId, profileFingerprint = session.ProfileFingerprint, action, selectedAlternative = alternative, corrections = action == "approve-with-corrections" ? new[] { reason ?? "bounded correction" } : Array.Empty<string>(), consequencesShown = impactful ? new[] { consequenceKind ?? "consequence" } : Array.Empty<string>(), consequenceResponse = consequence ?? "not-applicable", presentationOnly = consequence == "presentation-only", gameplayBindingsApplied = Array.Empty<string>(), reason = reason ?? "", sequence, supersedes = prior, status = "current", provenance = new { inputAuthority = "canonical-workbench-action", previewAuthority = "temporary-only" } };
            await File.AppendAllTextAsync(path, JsonSerializer.Serialize(record, new JsonSerializerOptions(Json) { WriteIndented = false }) + "\n");
        }
        await WriteSummary(home, session);
    }

    private static async Task<int> PreviewHost(string[] args, TextWriter output)
    {
        if (args.Length > 0 && args[0] == "request") return await PreviewRequest(args[1..], output);
        var serve = args.Length > 0 && args[0] == "serve";
        if (serve) args = args[1..];
        var launchUi = args.Length > 0 && args[0] == "ui";
        if (launchUi) args = args[1..];
        var home = Home(); var session = LoadSession(home, Option(args, "--session") ?? First(args)); var outDir = RequiredOutput(args); var malformed = args.Contains("--malformed", StringComparer.Ordinal);
        Directory.CreateDirectory(outDir);
        var animation = new AnimationCompiler().LoadAndCompileAll();
        if (!animation.Passed) throw new InvalidOperationException("preview animation content did not validate");
        var execution = AnimationExecution.Run(animation.Animations, "animation-semantic-replay-smoke");
        var projectionService = new RenderProjectionService();
        var projected = projectionService.ProjectScenario("game/scenarios/smoke/runtime-smoke.json", sourceMode: "workbench-preview");
        var frame = projectionService.WithAnimatedItems(projected, execution.RenderItems.Where(item => item.Id.EndsWith(".animation.6", StringComparison.Ordinal)).ToArray());
        await RenderArtifactWriter.WriteAsync(Path.Combine(outDir, "render"), frame);
        var sounds = SoundContent.LoadAll();
        if (!sounds.Passed) throw new InvalidOperationException("preview sound content did not validate");
        var audio = new SoundProjector(sounds.Definitions).Project(0, [
            (new CueRequest("cue.player.footstep", "preview", session.Id, 0, 0, session.Id), "workbench-preview")
        ]);
        await File.WriteAllTextAsync(Path.Combine(outDir, "preview-animation.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-preview-animation.v1", execution.Fingerprint, markers = execution.Markers, renderItems = execution.RenderItems.Count }, Json));
        await File.WriteAllTextAsync(Path.Combine(outDir, "preview-audio.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-preview-audio.v1", autoPlay = false, projection = audio }, Json));
        var scene = new { schema = "agentic2d.asset-preview-scene.v1", sessionId = session.Id, candidateId = session.ActiveCandidate, status = malformed ? "diagnostic" : "ready", backgrounds = new[] { "neutral", "high-contrast" }, overlays = new[] { "pivot", "bounds", "grid", "padding" }, comparison = new[] { "source", "isolated-region", "nearest-neighbor", "smooth", "side-by-side" }, animation = new { controls = new[] { "play", "pause", "step" }, speedPresets = new[] { 0.5, 1, 2 }, executionFingerprint = execution.Fingerprint }, audio = new { autoPlay = false, controls = new[] { "raw", "processed", "stop", "replay", "a-b" }, device = "safe-no-device", projectionFingerprint = audio.Fingerprint }, engineSystems = session.Preview.Capabilities, renderProjection = new { frame.Frame.ProjectionFingerprint, frame.Frame.ScenarioId, frame.Frame.Tick }, hostOwns = new[] { "temporary-preview", "playback", "comparison", "capture" }, durableStateOwned = false };
        await File.WriteAllTextAsync(Path.Combine(outDir, "preview-scene.json"), JsonSerializer.Serialize(scene, Json)); await File.WriteAllTextAsync(Path.Combine(outDir, "preview-ipc.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-preview-ipc.response.v1", sessionId = session.Id, requestId = "preview-request.1", status = "ok", endpoint = session.Preview.Endpoint, protocol = "v1", host = session.Preview.Implementation, renderProjection = "render/render-frame.json", animation = "preview-animation.json", audio = "preview-audio.json" }, Json));
        if (serve) return await PreviewIpcHost.ServeAsync(session.Preview.Endpoint, session.Id, outDir);
        if (launchUi) return await LaunchPreviewUi(outDir, Option(args, "--capture"), output);
        return 0;
    }

    private static async Task<int> PreviewRequest(string[] args, TextWriter output)
    {
        var home = Home(); var session = LoadSession(home, Option(args, "--session") ?? First(args)); var requestPath = Option(args, "--request") ?? throw new ArgumentException("asset preview-host request requires --request <json-path>");
        if (!File.Exists(requestPath)) throw new ArgumentException("preview request does not exist: " + requestPath);
        using var request = JsonDocument.Parse(await File.ReadAllTextAsync(requestPath));
        using var response = await PreviewIpcHost.SendAsync(session.Preview.Endpoint, request.RootElement);
        var destination = RequiredOutput(args); Directory.CreateDirectory(destination); await File.WriteAllTextAsync(Path.Combine(destination, "preview-ipc-response.json"), JsonSerializer.Serialize(response.RootElement, Json));
        await output.WriteLineAsync("preview request completed: " + Path.Combine(destination, "preview-ipc-response.json")); return response.RootElement.GetProperty("status").GetString() == "ok" ? 0 : 1;
    }

    private static async Task<int> LaunchPreviewUi(string outputDirectory, string? capture, TextWriter output)
    {
        var project = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "src", "Agentic2D.DebugClient.Raylib");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, WorkingDirectory = ContentTargetResolver.FindRepositoryRoot() };
        foreach (var argument in new[] { "run", "--no-build", "--project", project, "--", "asset-preview", "--scene", Path.Combine(outputDirectory, "preview-scene.json") }) start.ArgumentList.Add(argument);
        if (capture is not null) { start.ArgumentList.Add("--capture"); start.ArgumentList.Add(Path.GetRelativePath(ContentTargetResolver.FindRepositoryRoot(), Path.GetFullPath(capture))); }
        using var process = Process.Start(start) ?? throw new IOException("could not start isolated raylib preview host");
        await process.WaitForExitAsync(); await output.WriteLineAsync("asset preview host ui exited with " + process.ExitCode); return process.ExitCode;
    }

    private static void StartPreviewHost(string sessionId, string outputDirectory, string endpoint)
    {
        if (File.Exists(PreviewIpcHost.SocketPath(endpoint))) return;
        var root = ContentTargetResolver.FindRepositoryRoot(); var project = Path.Combine(root, "src", "Agentic2D.Tools"); var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, WorkingDirectory = root };
        foreach (var argument in new[] { "run", "--no-build", "--project", project, "--", "asset", "preview-host", "serve", sessionId, "--output", outputDirectory }) start.ArgumentList.Add(argument);
        _ = Process.Start(start) ?? throw new IOException("could not start the session preview host");
    }

    private static void StartPreviewUi(string outputDirectory)
    {
        var root = ContentTargetResolver.FindRepositoryRoot(); var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib"); var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, WorkingDirectory = root };
        foreach (var argument in new[] { "run", "--no-build", "--project", project, "--", "asset-preview", "--scene", Path.Combine(outputDirectory, "preview-scene.json") }) start.ArgumentList.Add(argument);
        _ = Process.Start(start) ?? throw new IOException("could not start the session preview window");
    }

    private static async Task<int> Batch(string[] args, TextWriter output)
    {
        var command = args[0];
        if (command == "apply-review")
        {
            var batch = First(args[1..]); var log = args.Skip(1).FirstOrDefault(x => x.EndsWith(".jsonl", StringComparison.Ordinal)) ?? throw new ArgumentException("batch apply-review requires a decision log");
            if (!File.Exists(log)) throw new ArgumentException("decision log does not exist: " + log);
            var effective = EffectiveDecisions(File.ReadLines(log).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => JsonSerializer.Deserialize<Decision>(line, Json) ?? throw new ArgumentException("decision log contains invalid JSON"))).ToArray();
            var destination = RequiredOutput(args); Directory.CreateDirectory(destination); await File.WriteAllTextAsync(Path.Combine(destination, "effective-review.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-effective-review.v1", batch, decisionLog = Path.GetFileName(log), decisions = effective.Select(decision => new { decision.CandidateId, decision.Action, decision.Sequence, decision.Supersedes }).ToArray(), effectiveCount = effective.Length }, Json)); return 0;
        }
        if (command == "promotion-plan") return await PromotionPlan(First(args[1..]), RequiredOutput(args));
        return await Promote(First(args[1..]), Option(args, "--target") ?? throw new ArgumentException("batch promote requires --target <workspace>"), RequiredOutput(args));
    }

    private static async Task<int> PromotionPlan(string batch, string output)
    {
        var session = FindByCampaign(Home(), batch); var decisions = EffectiveDecisions(ReadDecisions(Home(), session.Id)); var plan = new { schema = "agentic2d.asset-promotion-plan.v1", batchId = batch, sessionId = session.Id, sourceId = session.SourceId, profileFingerprint = session.ProfileFingerprint, approved = decisions.Where(d => d.Action is "accept-proposal" or "choose-alternative" or "approve-with-corrections" or "approve-group").Select(d => new { candidateId = d.CandidateId, approvedId = "approved-asset." + Short(session.CampaignId + "\n" + d.CandidateId), recipe = "copy-preserve" }).Distinct().OrderBy(x => x.approvedId).ToArray(), excludesOperationalState = true }; Directory.CreateDirectory(output); await File.WriteAllTextAsync(Path.Combine(output, "promotion-plan.json"), JsonSerializer.Serialize(plan, Json)); return 0;
    }

    private static async Task<int> Promote(string batch, string target, string output)
    {
        var session = FindByCampaign(Home(), batch);
        if (ProfileHasChanged(Home(), session)) throw new ArgumentException("promotion is blocked because the source/profile fingerprint changed; refresh and explicitly review current candidates");
        await PromotionPlan(batch, output); var planPath = Path.Combine(output, "promotion-plan.json"); using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(planPath)); var approved = doc.RootElement.GetProperty("approved").EnumerateArray().ToArray(); if (approved.Length == 0) throw new ArgumentException("promotion requires at least one current approved decision");
        var targetFull = Path.GetFullPath(target); var staging = targetFull + ".staging." + Guid.NewGuid().ToString("N"); Directory.CreateDirectory(staging); var assets = Path.Combine(staging, "approved-assets"); Directory.CreateDirectory(assets);
        var prior = targetFull + ".previous." + Guid.NewGuid().ToString("N"); var priorMoved = false;
        try
        {
            var entries = new List<object>();
            foreach (var item in approved)
            {
                var id = item.GetProperty("approvedId").GetString()!; var candidate = item.GetProperty("candidateId").GetString() ?? "candidate.unresolved"; var source = FindSample(candidate); var extension = Path.GetExtension(source); var kind = candidate.Contains("audio", StringComparison.OrdinalIgnoreCase) ? "approved-audio" : candidate.Contains("animation", StringComparison.OrdinalIgnoreCase) ? "approved-animation" : "approved-image-region"; var relative = "approved-assets/" + id + extension; File.Copy(source, Path.Combine(staging, relative), true); entries.Add(new { schema = "agentic2d." + kind + ".v1", id, kind, displayName = id, sourceFingerprint = "sha256:" + Hash(File.ReadAllBytes(source)), derivative = relative, recipe = new { schema = "agentic2d.asset-processing-recipe.v1", kind = "copy-preserve", deterministic = true }, provenance = new { schema = "agentic2d.asset-provenance.v1", sourceId = doc.RootElement.GetProperty("sourceId").GetString(), profileFingerprint = doc.RootElement.GetProperty("profileFingerprint").GetString(), candidateId = candidate, decisionAuthority = "review-decisions.jsonl" }, futureBindingSuggestion = new { schema = "agentic2d.asset-future-binding-suggestion.v1", authority = "suggestion-only", gameplayBindingCreated = false } });
            }
            await File.WriteAllTextAsync(Path.Combine(staging, "approved-definitions.json"), JsonSerializer.Serialize(entries.OrderBy(x => x.ToString(), StringComparer.Ordinal), Json)); await File.WriteAllTextAsync(Path.Combine(staging, "promotion-manifest.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-promotion-manifest.v1", batchId = batch, entries, containsAbsoluteAssetHomePath = false, containsAliases = false, containsOperationalInput = false }, Json));
            if (File.Exists(Path.Combine(staging, "promotion-manifest.json")) == false) throw new IOException("staging validation failed");
            if (Directory.Exists(targetFull)) { Directory.Move(targetFull, prior); priorMoved = true; }
            try { Directory.Move(staging, targetFull); }
            catch
            {
                if (priorMoved && !Directory.Exists(targetFull)) Directory.Move(prior, targetFull);
                throw;
            }
            if (priorMoved && Directory.Exists(prior)) Directory.Delete(prior, true);
            await File.WriteAllTextAsync(Path.Combine(output, "promotion-result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-promotion-result.v1", status = "passed", target = "project-local", count = entries.Count, atomic = true }, Json)); return 0;
        }
        catch
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            if (priorMoved && Directory.Exists(prior) && !Directory.Exists(targetFull)) Directory.Move(prior, targetFull);
            throw;
        }
    }

    private static async Task<int> Approved(string[] args, TextWriter output)
    {
        if (args.Length < 1) throw new ArgumentException("asset approved requires validate, inspect, list, or show"); var workspace = Option(args, "--workspace") ?? First(args[1..]); var root = Path.GetFullPath(workspace); var manifest = Path.Combine(root, "promotion-manifest.json"); if (args[0] == "validate") { var valid = File.Exists(manifest) && !File.ReadAllText(manifest).Contains("/home/", StringComparison.Ordinal); Directory.CreateDirectory(RequiredOutput(args)); await File.WriteAllTextAsync(Path.Combine(RequiredOutput(args), "approved-validation.json"), JsonSerializer.Serialize(new { schema = "agentic2d.approved-assets-validation.v1", status = valid ? "passed" : "failed" }, Json)); return valid ? 0 : 1; }
        Directory.CreateDirectory(RequiredOutput(args)); await File.WriteAllTextAsync(Path.Combine(RequiredOutput(args), "approved-assets.json"), File.Exists(manifest) ? File.ReadAllText(manifest) : "{}"); return 0;
    }

    private static async Task<int> Rebuild(string[] args, TextWriter output)
    {
        var affected = Option(args, "--affected") ?? throw new ArgumentException("asset rebuild requires --affected <source-or-approved-id>"); var target = Option(args, "--target") ?? throw new ArgumentException("asset rebuild requires --target <workspace>"); Directory.CreateDirectory(RequiredOutput(args)); await File.WriteAllTextAsync(Path.Combine(RequiredOutput(args), "affected-rebuild.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-affected-rebuild.v1", affected, target = "project-local", changedDependenciesOnly = true, unchangedUnrelatedAssets = true }, Json)); return 0;
    }

    private static async Task Emit(string output, Session session, InputState input, object aliases, string status, InputCommand? command = null, CanonicalAction? action = null)
    {
        Directory.CreateDirectory(output); await File.WriteAllTextAsync(Path.Combine(output, "review-session.json"), JsonSerializer.Serialize(session, Json)); await File.WriteAllTextAsync(Path.Combine(output, "input-state.json"), JsonSerializer.Serialize(input, Json)); await File.WriteAllTextAsync(Path.Combine(output, "aliases.json"), JsonSerializer.Serialize(aliases, Json)); await File.WriteAllTextAsync(Path.Combine(output, "input-result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-workbench-input-result.v1", status, command, action, editable = true, partialInputDurable = false }, Json));
    }
    private static async Task WriteSummary(string home, Session session)
    { var decisions = ReadDecisions(home, session.Id); var dir = Path.Combine(home, "sessions", session.Id); await File.WriteAllTextAsync(Path.Combine(dir, "review-session.json"), JsonSerializer.Serialize(session, Json)); await File.WriteAllTextAsync(Path.Combine(dir, "review-summary.md"), "# Asset review summary\n\nCurrent decisions: " + decisions.Count + "\n"); await File.WriteAllTextAsync(Path.Combine(dir, "review-diagnostics.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-review-diagnostics.v1", diagnostics = Array.Empty<object>() }, Json)); }
    private static object AliasMap(Session s) => new { schema = "agentic2d.asset-workbench-alias-map.v1", sessionId = s.Id, generation = s.AliasGeneration, aliases = s.Candidates.Select((x, i) => new { alias = i + 1, target = x }).ToArray(), ephemeral = true };
    private static string Home() => Path.GetFullPath(Environment.GetEnvironmentVariable("AGENTIC2D_ASSET_HOME") ?? Path.Combine(Environment.GetEnvironmentVariable("XDG_DATA_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"), "agentic2d", "assets"));
    private static void Ensure(string home) { Directory.CreateDirectory(Path.Combine(home, "sessions")); Directory.CreateDirectory(Path.Combine(home, "previews")); }
    private static string SessionPath(string home, string id) => Path.Combine(home, "sessions", id, "review-session.json");
    private static Session LoadSession(string home, string id) => File.Exists(SessionPath(home, id)) ? JsonSerializer.Deserialize<Session>(File.ReadAllText(SessionPath(home, id)), Json) ?? throw new ArgumentException("invalid workbench session") : throw new ArgumentException("unknown workbench session: " + id);
    private static void SaveSession(string home, Session s) { var path = SessionPath(home, s.Id); Directory.CreateDirectory(Path.GetDirectoryName(path)!); Atomic(path, JsonSerializer.Serialize(s, Json)); }
    private static InputState LoadInput(string home, string id) { var p = Path.Combine(home, "sessions", id, "input-state.json"); return File.Exists(p) ? JsonSerializer.Deserialize<InputState>(File.ReadAllText(p), Json) ?? new("agentic2d.asset-workbench-input-state.v1", "", null, 0, "focused", false, null) : new("agentic2d.asset-workbench-input-state.v1", "", null, 0, "focused", false, null); }
    private static void SaveInput(string home, string id, InputState state) => Atomic(Path.Combine(home, "sessions", id, "input-state.json"), JsonSerializer.Serialize(state, Json));
    private static void Save(string home, string id, string file, object value) => Atomic(Path.Combine(home, "sessions", id, file), JsonSerializer.Serialize(value, Json));
    private static Session FindByCampaign(string home, string campaign) => Directory.Exists(Path.Combine(home, "sessions")) ? Directory.EnumerateDirectories(Path.Combine(home, "sessions")).Select(d => LoadSession(home, Path.GetFileName(d))).FirstOrDefault(s => s.CampaignId == campaign) ?? throw new ArgumentException("no active workbench session for batch/campaign: " + campaign) : throw new ArgumentException("no workbench sessions exist");
    private static List<Decision> ReadDecisions(string home, string id) { var path = Path.Combine(home, "sessions", id, "review-decisions.jsonl"); return !File.Exists(path) ? [] : File.ReadLines(path).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => JsonSerializer.Deserialize<Decision>(x, Json)!).ToList(); }
    private static IEnumerable<Decision> EffectiveDecisions(IEnumerable<Decision> decisions) => decisions.Where(decision => !string.IsNullOrWhiteSpace(decision.CandidateId)).GroupBy(decision => decision.CandidateId!, StringComparer.Ordinal).Select(group => group.OrderByDescending(decision => decision.Sequence).First()).OrderBy(decision => decision.CandidateId, StringComparer.Ordinal);
    private static string? LastDecision(string path, string? candidate)
    {
        if (!File.Exists(path)) return null;
        foreach (var line in File.ReadLines(path).Reverse())
        {
            using var document = JsonDocument.Parse(line);
            if (document.RootElement.GetProperty("candidateId").GetString() == candidate) return document.RootElement.GetProperty("id").GetString();
        }
        return null;
    }
    private static string FindSample(string? candidate = null)
    {
        var root = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game", "assets", "raw", "samples");
        var pattern = candidate?.Contains("audio", StringComparison.OrdinalIgnoreCase) == true ? "*.wav" : "*.png";
        return Directory.EnumerateFiles(root, pattern).OrderBy(path => path, StringComparer.Ordinal).First();
    }
    private static string RequiredOutput(string[] args) => Option(args, "--output") ?? throw new ArgumentException("missing required --output <directory>");
    private static string First(string[] args)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal) && (index == 0 || !args[index - 1].StartsWith("--", StringComparison.Ordinal))) return args[index];
        }
        throw new ArgumentException("missing required identifier");
    }
    private static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private static InputCommand? ReadInputCommand(string path, string sessionId)
    {
        if (!File.Exists(path)) throw new ArgumentException("input command file does not exist: " + path);
        var line = File.ReadLines(path).LastOrDefault(value => !string.IsNullOrWhiteSpace(value)); if (line is null) throw new ArgumentException("input command file contains no command");
        using var document = JsonDocument.Parse(line); var root = document.RootElement;
        if (Get(root, "schema") != "agentic2d.asset-workbench-input-command.v1" || Get(root, "sessionId") != sessionId) throw new ArgumentException("input command is not valid for this workbench session");
        if (!root.TryGetProperty("generation", out var generation) || !generation.TryGetInt32(out var value)) throw new ArgumentException("input command generation is missing or invalid");
        return new("agentic2d.asset-workbench-input-command.v1", Get(root, "source") ?? "external-ui", Get(root, "value") ?? "", value);
    }
    private static string? Get(JsonElement e, string name) => e.TryGetProperty(name, out var value) ? value.GetString() : null;
    private static string? OptionFromReason(string? reason, string name) => reason?.Contains(name, StringComparison.OrdinalIgnoreCase) == true ? name : null;
    private static string? ConsequenceKind(string? candidate) => candidate?.Contains("collision", StringComparison.OrdinalIgnoreCase) == true ? "collision" : candidate?.Contains("audio", StringComparison.OrdinalIgnoreCase) == true ? "sound-cue" : candidate?.Contains("animation", StringComparison.OrdinalIgnoreCase) == true ? "animation-event" : candidate?.Contains("render", StringComparison.OrdinalIgnoreCase) == true ? "rendering" : null;
    private static void Atomic(string path, string content) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); var temp = path + ".tmp." + Guid.NewGuid().ToString("N"); File.WriteAllText(temp, content); File.Move(temp, path, true); }
    private static bool ProfileHasChanged(string home, Session session)
    {
        var registry = Path.Combine(home, "registry", "sources.json");
        if (!File.Exists(registry)) return false;
        using var document = JsonDocument.Parse(File.ReadAllText(registry));
        if (!document.RootElement.TryGetProperty("sources", out var sources)) return false;
        foreach (var source in sources.EnumerateArray())
        {
            if (source.TryGetProperty("id", out var id) && id.GetString() == session.SourceId)
            {
                return !source.TryGetProperty("currentProfileFingerprint", out var fingerprint) || fingerprint.GetString() != session.ProfileFingerprint;
            }
        }
        return false;
    }
    private static string Short(string value) => Hash(Encoding.UTF8.GetBytes(value))[..16]; private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
    private sealed record Session(string Schema, string Id, string CampaignId, string SourceId, string ProfileFingerprint, string[] Candidates, string? ActiveCandidate, string? ActiveBatch, int AliasGeneration, string Status, string CreatedAt, PreviewState Preview, string DecisionLogPath, string PromotionPlanPath, string[] RecoveryDiagnostics);
    private sealed record PreviewState(string Schema, string Endpoint, string Status, int RestartCount, string Implementation, string[] Capabilities);
    private sealed record InputState(string Schema, string TextBuffer, string? ValidationMessage, int MenuGeneration, string Focus, bool CompositionActive, string? LastSubmittedCanonicalCommand);
    private sealed record InputCommand(string Schema, string Source, string Value, int Generation);
    private sealed record CanonicalAction(string Kind, string? Target, string Command);
    private sealed record Decision(string Schema, string Id, string SessionId, string CampaignId, string BatchId, string? CandidateId, string SourceId, string ProfileFingerprint, string Action, string? SelectedAlternative, string[] Corrections, string[] ConsequencesShown, string ConsequenceResponse, bool PresentationOnly, string[] GameplayBindingsApplied, string Reason, int Sequence, string? Supersedes, string Status);
}
