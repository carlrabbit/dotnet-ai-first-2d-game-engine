using System.Text.Json;
using Agentic2D.Gameplay;
using Agentic2D.Validation;

namespace Agentic2D.Tools;

internal static class M019ItemCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3 || args[0] != "content" || args[1] != "validate" || args[2] != "items") return -1;
        var index = Array.IndexOf(args, "--output");
        if (index < 0 || index + 1 >= args.Length) { await error.WriteLineAsync("missing required --output <directory>"); return 2; }
        var root = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game", "items");
        var diagnostics = new List<object>();
        var items = new List<ItemDefinitionSource>();
        foreach (var path in Directory.EnumerateFiles(root, "*.json").Order(StringComparer.Ordinal))
        {
            try
            {
                var item = JsonSerializer.Deserialize<ItemDefinitionSource>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                if (!item.IsValid) diagnostics.Add(new { id = "ITEM0001", severity = "error", message = "Item definition is invalid.", target = path });
                items.Add(item);
            }
            catch (JsonException exception) { diagnostics.Add(new { id = "ITEM0002", severity = "error", message = exception.Message, target = path }); }
        }
        if (!items.Any(x => x.Id == "item.collectible-crystal")) diagnostics.Add(new { id = "ITEM0003", severity = "error", message = "Required collectible crystal is missing.", target = root });
        var passed = diagnostics.Count == 0;
        var directory = args[index + 1]; Directory.CreateDirectory(directory);
        var json = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(Path.Combine(directory, "result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.content-validation.result.v1", command = "content validate", scope = "items", status = passed ? "passed" : "failed", exitCode = passed ? 0 : 1 }, json));
        await File.WriteAllTextAsync(Path.Combine(directory, "diagnostics.json"), JsonSerializer.Serialize(new { diagnostics }, json));
        await File.WriteAllTextAsync(Path.Combine(directory, "validated-items.json"), JsonSerializer.Serialize(new { items = items.Select(x => new { id = x.Id, status = passed ? "passed" : "failed" }) }, json));
        await output.WriteLineAsync($"content validate: {(passed ? "passed" : "failed")}; result: {Path.Combine(directory, "result.json")}");
        return passed ? 0 : 1;
    }
}
