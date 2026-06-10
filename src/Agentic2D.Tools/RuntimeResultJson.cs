using System.Text.Json;
using Agentic2D.Contracts;

namespace Agentic2D.Tools;

public static class RuntimeResultJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static string Serialize(RuntimeResult result)
    {
        return JsonSerializer.Serialize(result, Options);
    }

    public static async Task WriteAsync(string outputPath, RuntimeResult result)
    {
        await File.WriteAllTextAsync(outputPath, Serialize(result));
    }
}
