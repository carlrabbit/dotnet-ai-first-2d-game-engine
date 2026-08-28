using System.Text.Json;
using Agentic2D.Tools;

namespace Agentic2D.Tests.Unit;

public sealed class AssetWorkbenchTests
{
    private static readonly SemaphoreSlim EnvironmentGate = new(1, 1);
    [Test]
    public async Task AssetWorkbenchInput_TextStreamsStayEditableUntilExplicitSubmitAndMouseMatchesText()
    {
        var (root, session) = await CreateSession();
        var textOut = Path.Combine(root, "text");
        await Run(["asset", "workbench", "--session", session, "--text", "2", "--output", textOut], root);
        using (var input = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(textOut, "input-state.json"))))
        {
            await Assert.That(input.RootElement.GetProperty("textBuffer").GetString()).IsEqualTo("2");
            await Assert.That(input.RootElement.GetProperty("lastSubmittedCanonicalCommand").ValueKind).IsEqualTo(JsonValueKind.Null);
        }
        var submit = Path.Combine(root, "submit"); var mouse = Path.Combine(root, "mouse");
        await Run(["asset", "workbench", "--session", session, "--submit", "--output", submit], root);
        await Run(["asset", "workbench", "--session", session, "--select", "2", "--output", mouse], root);
        using var submitted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(submit, "input-result.json")));
        using var clicked = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(mouse, "input-result.json")));
        await Assert.That(submitted.RootElement.GetProperty("action").GetProperty("command").GetString()).IsEqualTo(clicked.RootElement.GetProperty("action").GetProperty("command").GetString());
    }

    [Test]
    public async Task AssetWorkbenchInput_InvalidPasteAndFocusDoNotCreateDecisions()
    {
        var (root, session) = await CreateSession(); var output = Path.Combine(root, "invalid");
        await Run(["asset", "workbench", "--session", session, "--paste", "99", "--submit", "--focus", "lost", "--output", output], root);
        using var input = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "input-state.json")));
        await Assert.That(input.RootElement.GetProperty("textBuffer").GetString()).IsEqualTo("99");
        await Assert.That(input.RootElement.GetProperty("validationMessage").GetString()).Contains("stale");
        await Assert.That(File.Exists(Path.Combine(root, "home", "sessions", session, "review-decisions.jsonl"))).IsFalse();
    }

    [Test]
    public async Task AssetWorkbenchInput_StaleAliasAndInvalidDecisionDoNotBecomeDurableAuthority()
    {
        var (root, session) = await CreateSession(); var output = Path.Combine(root, "stale");
        await Run(["asset", "workbench", "resume", session, "--output", Path.Combine(root, "resumed")], root);
        await Run(["asset", "workbench", "--session", session, "--select", "1", "--generation", "1", "--decision", "accept-proposal", "--output", output], root);
        using var input = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "input-state.json")));
        await Assert.That(input.RootElement.GetProperty("validationMessage").GetString()).Contains("stale");
        await Assert.That(File.Exists(Path.Combine(root, "home", "sessions", session, "review-decisions.jsonl"))).IsFalse();
    }

    [Test]
    public async Task AssetWorkbenchInput_RaylibCommandFileUsesTheSameCanonicalActionAsMouse()
    {
        var (root, session) = await CreateSession();
        var command = Path.Combine(root, "raylib-input.jsonl");
        await File.WriteAllTextAsync(command, JsonSerializer.Serialize(new { schema = "agentic2d.asset-workbench-input-command.v1", sessionId = session, source = "mouse-touch", value = "2", generation = 1 }) + Environment.NewLine);
        var raylib = Path.Combine(root, "raylib"); var mouse = Path.Combine(root, "mouse");
        await Run(["asset", "workbench", "--session", session, "--input-command-file", command, "--output", raylib], root);
        await Run(["asset", "workbench", "--session", session, "--select", "2", "--output", mouse], root);
        using var emitted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(raylib, "input-result.json")));
        using var direct = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(mouse, "input-result.json")));
        await Assert.That(emitted.RootElement.GetProperty("action").GetProperty("command").GetString()).IsEqualTo(direct.RootElement.GetProperty("action").GetProperty("command").GetString());
    }

    [Test]
    public async Task AssetWorkbenchDecision_PresentationOnlyPreservesHistoryWithoutGameplayBinding()
    {
        var (root, session) = await CreateSession(); var output = Path.Combine(root, "decision");
        await Run(["asset", "workbench", "--session", session, "--select", "3", "--decision", "approve-with-corrections", "--consequence", "presentation-only", "--reason", "consequence collision presentation only", "--output", output], root);
        var log = await File.ReadAllTextAsync(Path.Combine(root, "home", "sessions", session, "review-decisions.jsonl"));
        await Assert.That(log).Contains("presentationOnly");
        await Assert.That(log).Contains("gameplayBindingsApplied");
    }

    [Test]
    public async Task AssetWorkbenchDecision_CanonicalGuidedDecisionCommandRecordsTheSameDurableDecision()
    {
        var (root, session) = await CreateSession();
        await Run(["asset", "workbench", "--session", session, "--command", "decision accept-proposal", "--output", Path.Combine(root, "decision-command")], root);
        var log = await File.ReadAllTextAsync(Path.Combine(root, "home", "sessions", session, "review-decisions.jsonl"));
        await Assert.That(log).Contains("accept-proposal");
        await Assert.That(log).Contains("canonical-workbench-action");
    }

    [Test]
    public async Task AssetPreviewIpc_RestartAndMalformedSceneKeepWorkbenchInputUsable()
    {
        var (root, session) = await CreateSession();
        await Run(["asset", "preview-host", session, "--malformed", "--output", Path.Combine(root, "preview")], root);
        await Run(["asset", "workbench", "--session", session, "--preview-restart", "--text", "1", "--output", Path.Combine(root, "after")], root);
        using var scene = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "preview", "preview-scene.json")));
        using var input = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "after", "input-state.json")));
        await Assert.That(scene.RootElement.GetProperty("status").GetString()).IsEqualTo("diagnostic");
        await Assert.That(File.Exists(Path.Combine(root, "preview", "render", "render-frame.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(root, "preview", "preview-animation.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(root, "preview", "preview-audio.json"))).IsTrue();
        await Assert.That(input.RootElement.GetProperty("textBuffer").GetString()).IsEqualTo("1");
    }

    [Test]
    public async Task AssetPreviewIpc_UnixSocketHostServesBoundedHealthAndShutdownRequests()
    {
        var (root, session) = await CreateSession();
        using var stored = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "home", "sessions", session, "review-session.json")));
        var endpoint = stored.RootElement.GetProperty("preview").GetProperty("endpoint").GetString()!;
        var socket = PreviewIpcHost.SocketPath(endpoint); var state = Path.Combine(root, "preview-state");
        var server = PreviewIpcHost.ServeAsync(endpoint, session, state);
        for (var attempt = 0; attempt < 40 && !File.Exists(socket); attempt++) await Task.Delay(25);
        await Assert.That(File.Exists(socket)).IsTrue();
        using (var health = await PreviewIpcHost.SendAsync(endpoint, new { schema = "agentic2d.asset-preview-ipc.request.v1", sessionId = session, requestId = "request.health", operation = "health" }))
            await Assert.That(health.RootElement.GetProperty("status").GetString()).IsEqualTo("ok");
        using (var shutdown = await PreviewIpcHost.SendAsync(endpoint, new { schema = "agentic2d.asset-preview-ipc.request.v1", sessionId = session, requestId = "request.shutdown", operation = "shutdown" }))
            await Assert.That(shutdown.RootElement.GetProperty("hostState").GetString()).IsEqualTo("shutting-down");
        await server.WaitAsync(TimeSpan.FromSeconds(2));
        await Assert.That(File.Exists(socket)).IsFalse();
    }

    [Test]
    public async Task AssetPromotion_UsesProjectLocalAtomicOutputsWithoutOperationalState()
    {
        var (root, session) = await CreateSession(); var decision = Path.Combine(root, "decision");
        await Run(["asset", "workbench", "--session", session, "--select", "1", "--decision", "accept-proposal", "--reason", "approved", "--output", decision], root);
        var workspace = Path.Combine(root, "workspace"); var promotion = Path.Combine(root, "promotion");
        await Run(["asset", "batch", "promote", "campaign.test", "--target", workspace, "--output", promotion], root);
        var manifest = await File.ReadAllTextAsync(Path.Combine(workspace, "promotion-manifest.json"));
        await Assert.That(manifest).DoesNotContain("workbench-session");
        await Assert.That(manifest).DoesNotContain(root);
    }

    [Test]
    public async Task AssetPromotion_LaterRejectionSupersedesEarlierApprovalInTheEffectivePlan()
    {
        var (root, session) = await CreateSession();
        await Run(["asset", "workbench", "--session", session, "--select", "1", "--decision", "accept-proposal", "--reason", "initial approval", "--output", Path.Combine(root, "accept")], root);
        await Run(["asset", "workbench", "--session", session, "--select", "1", "--decision", "reject", "--reason", "later review", "--output", Path.Combine(root, "reject")], root);
        var plan = Path.Combine(root, "plan"); await Run(["asset", "batch", "promotion-plan", "campaign.test", "--output", plan], root);
        using var result = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(plan, "promotion-plan.json")));
        await Assert.That(result.RootElement.GetProperty("approved").GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task AssetPromotion_GroupApprovalAllowsARecordedIndividualException()
    {
        var (root, session) = await CreateSession();
        await Run(["asset", "workbench", "--session", session, "--command", "decision approve-group confirm", "--output", Path.Combine(root, "group")], root);
        await Run(["asset", "workbench", "--session", session, "--select", "2", "--decision", "reject", "--reason", "exception", "--output", Path.Combine(root, "exception")], root);
        var plan = Path.Combine(root, "group-plan"); await Run(["asset", "batch", "promotion-plan", "campaign.test", "--output", plan], root);
        using var result = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(plan, "promotion-plan.json")));
        var candidates = result.RootElement.GetProperty("approved").EnumerateArray().Select(item => item.GetProperty("candidateId").GetString()).ToArray();
        await Assert.That(candidates).DoesNotContain("candidate.b");
        await Assert.That(candidates).Contains("candidate.a");
        await Assert.That(candidates).Contains("candidate.collision");
    }

    private static async Task<(string Root, string Session)> CreateSession()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m029-tests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var source = Path.Combine(root, "source"); Directory.CreateDirectory(source); await File.WriteAllTextAsync(Path.Combine(source, "a.bin"), "candidate-a"); await File.WriteAllTextAsync(Path.Combine(source, "b.bin"), "candidate-b"); await File.WriteAllTextAsync(Path.Combine(source, "collision.bin"), "candidate-collision");
        var campaign = Path.Combine(root, "campaign.json"); await File.WriteAllTextAsync(campaign, """{"schema":"agentic2d.asset-campaign.v2","id":"campaign.test","sourceId":"asset-source.test","profileFingerprint":"sha256:test","candidates":[{"candidateId":"candidate.a","sourceRelativePath":"a.bin","mediaKind":"image-file","presentationRole":"default","proposalFingerprint":"proposal.a","selection":{"type":"file"}},{"candidateId":"candidate.b","sourceRelativePath":"b.bin","mediaKind":"image-file","presentationRole":"default","proposalFingerprint":"proposal.b","selection":{"type":"file"}},{"candidateId":"candidate.collision","sourceRelativePath":"collision.bin","mediaKind":"image-file","presentationRole":"default","proposalFingerprint":"proposal.collision","selection":{"type":"file"}}]}""");
        var registry = Path.Combine(root, "home", "registry"); Directory.CreateDirectory(registry); await File.WriteAllTextAsync(Path.Combine(registry, "sources.json"), $"{{\"schema\":\"agentic2d.asset-source-registry.v1\",\"sources\":[{{\"id\":\"asset-source.test\",\"path\":{JsonSerializer.Serialize(source)},\"currentProfileFingerprint\":\"sha256:test\"}}]}}");
        var output = Path.Combine(root, "create"); await Run(["asset", "workbench", "--campaign", campaign, "--headless", "--output", output], root);
        using var session = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "review-session.json"))); return (root, session.RootElement.GetProperty("id").GetString()!);
    }
    private static async Task Run(string[] args, string? root = null)
    {
        await EnvironmentGate.WaitAsync();
        try
        {
            var old = Environment.GetEnvironmentVariable("AGENTIC2D_ASSET_HOME"); if (root is not null) Environment.SetEnvironmentVariable("AGENTIC2D_ASSET_HOME", Path.Combine(root, "home"));
            await RunCore(args, old, root is not null);
        }
        finally { EnvironmentGate.Release(); }
    }
    private static async Task RunCore(string[] args, string? old, bool restore)
    {
        try
        {
            var errors = new StringWriter(); var code = await ToolsCli.RunAsync(args, new StringWriter(), errors);
            if (code != 0) throw new InvalidOperationException("workbench command failed: " + errors);
        }
        finally { if (restore) Environment.SetEnvironmentVariable("AGENTIC2D_ASSET_HOME", old); }
    }
}
