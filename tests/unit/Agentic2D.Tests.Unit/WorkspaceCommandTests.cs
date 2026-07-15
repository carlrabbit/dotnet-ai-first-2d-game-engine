using System.Text.Json;
using Agentic2D.Tools;

namespace Agentic2D.Tests.Unit;

public sealed class WorkspaceCommandTests
{
    [Test]
    public async Task WorkspaceCreateRejectsANonEmptyTargetWithoutChangingIt()
    {
        var root = CreateTempDirectory();
        var target = Path.Combine(root, "target");
        Directory.CreateDirectory(target);
        var sentinel = Path.Combine(target, "keep.txt");
        await File.WriteAllTextAsync(sentinel, "keep");

        var exitCode = await ToolsCli.RunAsync(
            ["workspace", "create", target, "--template", "minimal-game", "--engine-directory", RepositoryRoot(), "--engine-placement", "reference", "--output", Path.Combine(root, "out")],
            new StringWriter(),
            new StringWriter());

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(await File.ReadAllTextAsync(sentinel)).IsEqualTo("keep");
        await Assert.That(File.Exists(Path.Combine(target, "agentic2d.workspace.json"))).IsFalse();
    }

    [Test]
    public async Task WorkspaceProjectRunCreatesCentralManifestAndAllDeclaredContentDomainsValidate()
    {
        var root = CreateTempDirectory();
        var workspace = Path.Combine(root, "game");
        var output = Path.Combine(root, "out");
        var create = await ToolsCli.RunAsync(
            ["workspace", "create", workspace, "--template", "minimal-game", "--engine-directory", RepositoryRoot(), "--engine-placement", "reference", "--output", output],
            new StringWriter(),
            new StringWriter());
        var validate = await ToolsCli.RunAsync(["project", "validate", workspace, "--output", Path.Combine(root, "project")], new StringWriter(), new StringWriter());
        var run = await ToolsCli.RunAsync(["project", "run", workspace, "--scenario", "scenario.minimal.smoke", "--output", Path.Combine(root, "run")], new StringWriter(), new StringWriter());

        await Assert.That(create).IsEqualTo(0);
        await Assert.That(validate).IsEqualTo(0);
        await Assert.That(run).IsEqualTo(0);
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "run", "run-manifest.json")));
        await Assert.That(manifest.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.unified-run-manifest.v1");
        await Assert.That(manifest.RootElement.GetProperty("artifactFamilies").GetProperty("runtime").GetProperty("present").GetBoolean()).IsTrue();
        var render = manifest.RootElement.GetProperty("artifactFamilies").GetProperty("render");
        await Assert.That(render.GetProperty("status").GetString()).IsEqualTo("present");
        await Assert.That(render.GetProperty("fingerprint").GetString()).IsNotEmpty();
        await Assert.That(File.Exists(Path.Combine(root, "run", "render", "render-result.json"))).IsTrue();
    }

    [Test]
    public async Task WorkspaceValidationRejectsBootstrapManifestDrift()
    {
        var root = CreateTempDirectory(); var workspace = Path.Combine(root, "game");
        await ToolsCli.RunAsync(["workspace", "create", workspace, "--template", "minimal-game", "--engine-directory", RepositoryRoot(), "--engine-placement", "reference", "--output", Path.Combine(root, "out")], new StringWriter(), new StringWriter());
        var bootstrap = Path.Combine(workspace, "eng", "engine-bootstrap.env"); var original = await File.ReadAllTextAsync(bootstrap); await File.WriteAllTextAsync(bootstrap, original.Replace("ENGINE_PATH=\"", "ENGINE_PATH=\"missing/", StringComparison.Ordinal));
        var error = new StringWriter(); var failed = await ToolsCli.RunAsync(["workspace", "validate", workspace, "--output", Path.Combine(root, "invalid")], new StringWriter(), error);
        await Assert.That(failed).IsEqualTo(1);
        await Assert.That(await File.ReadAllTextAsync(Path.Combine(root, "invalid", "workspace-validation.json"))).Contains("WORKSPACE0018");
        await File.WriteAllTextAsync(bootstrap, original);
        var passed = await ToolsCli.RunAsync(["workspace", "validate", workspace, "--output", Path.Combine(root, "valid")], new StringWriter(), new StringWriter());
        await Assert.That(passed).IsEqualTo(0);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "agentic2d-workspace-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "dotnet-ai-first-2d-game-engine.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
