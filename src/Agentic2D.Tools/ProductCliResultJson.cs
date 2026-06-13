using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic2D.Contracts;

namespace Agentic2D.Tools;

public static class ProductCliResultJson
{
    public const string Schema = "agentic2d.product-cli.result.v1";

    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static ProductCliResult FromRuntimeResult(string command, RuntimeResult runtimeResult)
    {
        var exitCode = runtimeResult.Status switch
        {
            RuntimeStatus.Passed => 0,
            RuntimeStatus.Failed => 1,
            _ => 3,
        };

        var diagnostics = runtimeResult.Diagnostics
            .Select(static diagnostic => new ProductCliDiagnostic(diagnostic.Code, diagnostic.Severity, diagnostic.Message))
            .ToArray();

        return new ProductCliResult(
            Schema: Schema,
            Command: command,
            Status: runtimeResult.Status,
            ExitCode: exitCode,
            Diagnostics: diagnostics,
            Artifacts: [],
            Runtime: new ProductCliRuntimeSummary(runtimeResult.FinalTick, runtimeResult.Events.Count));
    }

    public static ProductCliResult Error(string command, string diagnosticId, string message)
    {
        return new ProductCliResult(
            Schema: Schema,
            Command: command,
            Status: RuntimeStatus.Error,
            ExitCode: 3,
            Diagnostics: [new ProductCliDiagnostic(diagnosticId, "error", message)],
            Artifacts: [],
            Runtime: new ProductCliRuntimeSummary(0, 0));
    }

    public static string Serialize(ProductCliResult result)
    {
        return JsonSerializer.Serialize(result, Options);
    }

    public static async Task WriteAsync(string outputPath, ProductCliResult result)
    {
        await File.WriteAllTextAsync(outputPath, Serialize(result));
    }
}

public sealed record ProductCliResult(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ProductCliDiagnostic> Diagnostics,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ProductCliArtifact> Artifacts,
    [property: JsonPropertyName("runtime")] ProductCliRuntimeSummary Runtime);

public sealed record ProductCliDiagnostic(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("message")] string Message);

public sealed record ProductCliArtifact(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("kind")] string Kind);

public sealed record ProductCliRuntimeSummary(
    [property: JsonPropertyName("ticksExecuted")] int TicksExecuted,
    [property: JsonPropertyName("eventsEmitted")] int EventsEmitted);
