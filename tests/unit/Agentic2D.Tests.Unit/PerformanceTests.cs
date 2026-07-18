using System.Text.Json;
using Agentic2D.Engineering;

namespace Agentic2D.Tests.Unit;

public sealed class PerformanceTests
{
    [Test]
    public async Task CaptureCompareAndReportProduceAdvisoryArtifacts()
    {
        var root = Directory.GetCurrentDirectory();
        var output = Path.Combine(Path.GetTempPath(), "agentic2d-performance-tests", Guid.NewGuid().ToString("N"));
        var before = Path.Combine(output, "before");
        var after = Path.Combine(output, "after");
        var report = Path.Combine(output, "report");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        await Assert.That(await PerformanceCli.RunAsync(["capture", "--label", "before", "--output", before], root, stdout, stderr)).IsEqualTo(0);
        await Assert.That(await PerformanceCli.RunAsync(["capture", "--label", "after", "--output", after], root, stdout, stderr)).IsEqualTo(0);
        await Assert.That(await PerformanceCli.RunAsync(["report", "--milestone", "M023", "--before", before, "--after", after, "--output", report], root, stdout, stderr)).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(report, "performance-report.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(report, "performance-report.md"))).IsTrue();

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(report, "performance-report.json")));
        await Assert.That(document.RootElement.GetProperty("comparison").GetProperty("workloads").GetArrayLength()).IsEqualTo(8);
        await Assert.That(document.RootElement.GetProperty("comparison").GetProperty("workloads").EnumerateArray().Any(workload => workload.GetProperty("id").GetString() == "performance.runtime-reference-scaled")).IsTrue();
        await Assert.That(document.RootElement.GetProperty("limitations").GetString()).Contains("same-machine");
    }
}
