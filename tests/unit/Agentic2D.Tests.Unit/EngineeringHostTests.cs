using System.Text.Json;
using Agentic2D.Engineering;

namespace Agentic2D.Tests.Unit;

public sealed class EngineeringHostTests
{
    [Test]
    public async Task PlanIsMachineReadableAndDeclaresBoundedShards()
    {
        var host = new EngineeringHost(Directory.GetCurrentDirectory());
        using var plan = JsonDocument.Parse(host.SerializePlan(host.GetSuite("m019-smoke")));
        await Assert.That(plan.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.engineering.validation-plan.v1");
        await Assert.That(plan.RootElement.GetProperty("requiredShards").GetArrayLength()).IsEqualTo(5);
    }

    [Test]
    public async Task M022AndM023PlansDoNotDependOnGuideMetadata()
    {
        var host = new EngineeringHost(Directory.GetCurrentDirectory());
        using var m022 = JsonDocument.Parse(host.SerializePlan(host.GetSuite("m022-smoke")));
        using var m023 = JsonDocument.Parse(host.SerializePlan(host.GetSuite("m023-smoke")));

        await Assert.That(m022.RootElement.GetProperty("requiredShards").EnumerateArray().Any(shard => shard.GetProperty("id").GetString() == "platform-and-leakage")).IsTrue();
        await Assert.That(m023.RootElement.GetProperty("requiredShards").EnumerateArray().Any(shard => shard.GetProperty("id").GetString() == "guide-v051")).IsFalse();
        await Assert.That(m022.RootElement.ToString().Contains(".guide-profile.json", StringComparison.Ordinal)).IsFalse();
        await Assert.That(m023.RootElement.ToString().Contains(".guide-profile.json", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task AtomicReceiptWriteLeavesOnlyFinalReceipt()
    {
        var directory = Path.Combine(Path.GetTempPath(), "agentic2d-engineering-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "receipt.json");
        var receipt = new ValidationReceipt("agentic2d.engineering.validation-receipt.v1", "suite", "shard", "passed", "suite", "repository", "command", "input", "result", "command", [], [], new CompletionMetadata(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "linux-bash"), []);
        ReceiptStore.WriteAtomic(path, receipt, new JsonSerializerOptions { WriteIndented = true });
        await Assert.That(File.Exists(path)).IsTrue();
        await Assert.That(Directory.EnumerateFiles(directory, "*.tmp").Any()).IsFalse();
        await Assert.That(ReceiptStore.TryRead(path, new JsonSerializerOptions(), out var read, out _)).IsTrue();
        await Assert.That(read!.Status).IsEqualTo("passed");
    }
}
