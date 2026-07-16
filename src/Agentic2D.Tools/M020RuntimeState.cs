using System.Text.Json;
using Agentic2D.Persistence;

namespace Agentic2D.Tools;

internal static class M020RuntimeState
{
    public static PersistentWorldSnapshot? Read(string runDirectory, string tick)
    {
        var name = tick == "final" ? "final-state.json" : "tick-" + tick + "-state.json";
        var path = Path.Combine(runDirectory, "runtime", "ticks", name);
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<PersistentWorldSnapshot>(File.ReadAllText(path), CanonicalJson.Options); }
        catch (JsonException) { return null; }
    }
    public static Task Write(string runDirectory, string name, PersistentWorldSnapshot snapshot)
    {
        var directory = Path.Combine(runDirectory, "runtime", "ticks"); Directory.CreateDirectory(directory);
        return File.WriteAllTextAsync(Path.Combine(directory, name), JsonSerializer.Serialize(snapshot, CanonicalJson.Options));
    }
}
