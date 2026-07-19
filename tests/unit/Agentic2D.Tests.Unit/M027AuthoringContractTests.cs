using System.Text.Json;
using Agentic2D.Tools;

namespace Agentic2D.Tests.Unit;

public sealed class M027AuthoringContractTests
{
    [Test]
    public async Task GeometryReviewPackEmitsStableSchemaAndExplicitCaptureStatus()
    {
        var output = CreateTempDirectory();
        var code = await ToolsCli.RunAsync(["geometry", "review-pack", Path.Combine(RepositoryRoot(), "consumers/signal-passage"), "--output", output], new StringWriter(), new StringWriter());

        await Assert.That(code).IsEqualTo(0);
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "manifest.json")));
        using var capture = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "capture-status.json")));
        await Assert.That(manifest.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.geometry-review-pack.v1");
        await Assert.That(capture.RootElement.GetProperty("status").GetString()).IsEqualTo("not-captured");
    }

    [Test]
    public async Task SoundReviewPackEmitsStableLinkageAndProvenanceSchemas()
    {
        var output = CreateTempDirectory();
        var code = await ToolsCli.RunAsync(["sound", "linkage", "review-pack", Path.Combine(RepositoryRoot(), "consumers/signal-passage"), "--output", output], new StringWriter(), new StringWriter());

        await Assert.That(code).IsEqualTo(0);
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "manifest.json")));
        using var linkage = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "generated-sound-linkage.json")));
        using var provenance = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "generated-sound-provenance.json")));
        await Assert.That(manifest.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.generated-sound-review-pack.v1");
        await Assert.That(linkage.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.generated-sound-linkage.v1");
        await Assert.That(provenance.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.generated-sound-provenance.v1");
    }

    [Test]
    public async Task GeometryDiagnosticsUseStableCodesAndFieldPaths()
    {
        var root = CreateTempDirectory();
        var source = Path.Combine(root, "invalid-geometry.json");
        await File.WriteAllTextAsync(source, """
            {"schema":"agentic2d.visual-definition.v1","id":"visual-definition.invalid","parts":[{"id":"part.invalid","anchor":"center","offset":{"x":0,"y":0},"worldSize":{"width":1,"height":1},"layer":"ground","order":0,"sortMode":"fixed","geometry":{"kind":"hexagon","fill":{"r":255,"g":255,"b":255,"a":255},"opacity":2}}]}
            """);

        var output = Path.Combine(root, "output");
        var code = await ToolsCli.RunAsync(["geometry", "inspect", source, "--output", output], new StringWriter(), new StringWriter());

        await Assert.That(code).IsEqualTo(1);
        using var diagnostics = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "geometry-diagnostics.json")));
        var values = diagnostics.RootElement.GetProperty("diagnostics").EnumerateArray().ToArray();
        await Assert.That(values.Select(value => value.GetProperty("code").GetString())).Contains("GEO001");
        await Assert.That(values.Select(value => value.GetProperty("code").GetString())).Contains("GEO003");
        await Assert.That(values.All(value => value.TryGetProperty("sourcePath", out _) && value.TryGetProperty("fieldPath", out _) && value.TryGetProperty("remediation", out _))).IsTrue();
    }

    [Test]
    public async Task MalformedLinkageProducesStableUnsupportedSchemaDiagnostic()
    {
        var root = CreateTempDirectory();
        var content = Path.Combine(root, "game-content", "sounds");
        Directory.CreateDirectory(content);
        await File.WriteAllTextAsync(Path.Combine(content, "generated-sound-linkage.json"), "{");
        var output = Path.Combine(root, "output");

        var code = await ToolsCli.RunAsync(["sound", "linkage", "validate", root, "--output", output], new StringWriter(), new StringWriter());

        await Assert.That(code).IsEqualTo(1);
        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "generated-sound-linkage-report.json")));
        await Assert.That(report.RootElement.GetProperty("diagnostics").EnumerateArray().Select(value => value.GetProperty("code").GetString())).Contains("SNDL010");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "agentic2d-m027-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string RepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "dotnet-ai-first-2d-game-engine.slnx"))) return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("repository root was not found");
    }
}
