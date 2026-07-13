using System.Text.Json;
using Agentic2D.ScenarioRunner;

namespace Agentic2D.Tests.Unit;

public sealed class Milestone013EvidenceTests
{
    [Test]
    public async Task TreeInspectionWritesLinkedClippedResolutionAndTransformMutation()
    {
        var output = Path.Combine(Path.GetTempPath(), "agentic2d-m013-evidence-" + Guid.NewGuid().ToString("N"));
        var run = new RuntimeInspector().Inspect("continuous.kinematic-tree-collision-smoke", "map.continuous-smoke");
        await RuntimeInspectionArtifactWriter.WriteAsync(output, run);

        var mutations = ReadJsonLines(Path.Combine(output, "component-mutations.jsonl"));
        var resolutions = ReadJsonLines(Path.Combine(output, "continuous-resolutions.jsonl"));
        var resolution = resolutions.Single();

        await Assert.That(mutations.Count).IsGreaterThan(0);
        await Assert.That(resolution.GetProperty("Outcome").GetString()).IsEqualTo("clipped");
        await Assert.That(resolution.GetProperty("CollisionCandidates").EnumerateArray().Any(item => item.GetProperty("SourceId").GetString() == "object.tree.large.smoke")).IsTrue();
        await Assert.That(resolution.GetProperty("XAxis").GetProperty("Constrained").GetBoolean()).IsTrue();
        var commandId = resolution.GetProperty("MutationCommandId").GetString();
        var mutation = mutations.Single(item => item.GetProperty("CommandId").GetString() == commandId);
        await Assert.That(mutation.GetProperty("MutationKind").GetString()).IsEqualTo("component-updated");
        await Assert.That(mutation.GetProperty("PreviousValue").GetProperty("X").GetDouble()).IsEqualTo(1.5d);
        await Assert.That(mutation.GetProperty("ResultingValue").GetProperty("X").GetDouble()).IsEqualTo(resolution.GetProperty("ResultingTransform").GetProperty("X").GetDouble());
    }

    [Test]
    public async Task AcceptedEvidenceIsDeterministicAndSequencesAreContiguous()
    {
        var first = await WriteAndReadAsync();
        var second = await WriteAndReadAsync();

        await Assert.That(first.Mutations).IsEqualTo(second.Mutations);
        await Assert.That(first.Resolutions).IsEqualTo(second.Resolutions);
        using var resolution = JsonDocument.Parse(first.Resolutions.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Single());
        await Assert.That(resolution.RootElement.GetProperty("Outcome").GetString()).IsEqualTo("accepted");
        await Assert.That(resolution.RootElement.GetProperty("MutationCommandId").GetString()).IsNotNull();
        var sequences = ReadJsonLinesFromText(first.Mutations).Select(item => item.GetProperty("Sequence").GetInt32()).ToArray();
        await Assert.That(sequences).IsEquivalentTo(Enumerable.Range(1, sequences.Length));
    }

    private static async Task<(string Mutations, string Resolutions)> WriteAndReadAsync()
    {
        var output = Path.Combine(Path.GetTempPath(), "agentic2d-m013-evidence-" + Guid.NewGuid().ToString("N"));
        await RuntimeInspectionArtifactWriter.WriteAsync(output, new RuntimeInspector().Inspect("continuous.kinematic-movement-smoke", "map.continuous-smoke"));
        return (File.ReadAllText(Path.Combine(output, "component-mutations.jsonl")), File.ReadAllText(Path.Combine(output, "continuous-resolutions.jsonl")));
    }

    private static List<JsonElement> ReadJsonLines(string path) => ReadJsonLinesFromText(File.ReadAllText(path));
    private static List<JsonElement> ReadJsonLinesFromText(string text) => text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToList();
}
