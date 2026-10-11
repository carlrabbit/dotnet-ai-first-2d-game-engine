using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agentic2D.Tools;

/// <summary>
/// Versioned, session-local preview transport. The host owns only ephemeral preview state;
/// decisions and editable workbench input remain in the provider-side session files.
/// </summary>
public static class PreviewIpcHost
{
    private const string RequestSchema = "agentic2d.asset-preview-ipc.request.v2";
    private const string ResponseSchema = "agentic2d.asset-preview-ipc.response.v2";
    private const string LegacyRequestSchema = "agentic2d.asset-preview-ipc.request.v1";
    private const string LegacyResponseSchema = "agentic2d.asset-preview-ipc.response.v1";

    public static async Task<int> ServeAsync(string endpoint, string sessionId, string stateDirectory, CancellationToken cancellationToken = default)
    {
        var path = SocketPath(endpoint);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path)) File.Delete(path);
        using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(path)); listener.Listen(4);
        var running = true;
        try
        {
            while (running && !cancellationToken.IsCancellationRequested)
            {
                using var client = await listener.AcceptAsync(cancellationToken);
                using var stream = new NetworkStream(client, ownsSocket: false);
                using var reader = new StreamReader(stream, leaveOpen: true);
                await using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };
                var line = await reader.ReadLineAsync(cancellationToken);
                var response = Handle(line, sessionId, stateDirectory, out var shutdown);
                await writer.WriteLineAsync(JsonSerializer.Serialize(response));
                running = !shutdown;
            }
        }
        finally
        {
            listener.Close();
            if (File.Exists(path)) File.Delete(path);
        }
        return 0;
    }

    public static async Task<JsonDocument> SendAsync(string endpoint, object request, CancellationToken cancellationToken = default)
    {
        var path = SocketPath(endpoint);
        using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await client.ConnectAsync(new UnixDomainSocketEndPoint(path), cancellationToken);
        using var stream = new NetworkStream(client, ownsSocket: false);
        using var reader = new StreamReader(stream, leaveOpen: true);
        await using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(JsonSerializer.Serialize(request).AsMemory(), cancellationToken);
        var line = await reader.ReadLineAsync(cancellationToken) ?? throw new IOException("preview host closed without an IPC response");
        return JsonDocument.Parse(line);
    }

    public static string SocketPath(string endpoint)
    {
        const string prefix = "unix://";
        if (!endpoint.StartsWith(prefix, StringComparison.Ordinal)) throw new ArgumentException("preview endpoint must use unix://");
        var path = Path.GetFullPath(endpoint[prefix.Length..]);
        if (!path.EndsWith(".sock", StringComparison.Ordinal) || path.Contains('\0')) throw new ArgumentException("preview endpoint is not a safe Unix socket path");
        return path.Length <= 100 ? path : Path.Combine(Path.GetTempPath(), "agentic2d-preview", Short(path) + ".sock");
    }

    public static string EndpointForSession(string assetHome, string sessionId)
    {
        var candidate = Path.Combine(assetHome, "previews", sessionId + ".sock");
        return "unix://" + (candidate.Length <= 100 ? candidate : Path.Combine(Path.GetTempPath(), "agentic2d-preview", Short(assetHome + "\n" + sessionId) + ".sock"));
    }

    private static object Handle(string? line, string sessionId, string stateDirectory, out bool shutdown)
    {
        shutdown = false;
        if (string.IsNullOrWhiteSpace(line) || line.Length > 65_536) return Response(sessionId, "", "invalid-request", "request is missing or exceeds the 64 KiB bound");
        try
        {
            using var document = JsonDocument.Parse(line); var request = document.RootElement;
            var schema = Text(request, "schema"); var requestSession = Text(request, "sessionId"); var requestId = Text(request, "requestId"); var operation = Text(request, "operation");
            if (requestSession != sessionId || string.IsNullOrWhiteSpace(requestId)) return Response(sessionId, requestId, "invalid-request", "schema, sessionId, or requestId is invalid");
            if (operation is not ("health" or "load" or "background" or "overlay" or "animation" or "audio" or "capture" or "reset" or "shutdown")) return Response(sessionId, requestId, "invalid-request", "operation is not supported");
            if (schema == LegacyRequestSchema)
            {
                if (operation is not ("health" or "shutdown")) return new { schema = LegacyResponseSchema, sessionId, requestId, status = "invalid-request", diagnostic = "preview IPC v1 is retired; use request.v2 with an exact materialization subject", durableStateOwned = false };
                shutdown = operation == "shutdown";
                return new { schema = LegacyResponseSchema, sessionId, requestId, status = "ok", operation, hostState = shutdown ? "shutting-down" : "ready", durableStateOwned = false };
            }
            if (schema != RequestSchema) return Response(sessionId, requestId, "invalid-request", "preview IPC v1 is retired; use request.v2 with an exact materialization subject");
            shutdown = operation == "shutdown";
            var state = new { schema = "agentic2d.asset-preview-runtime-state.v1", sessionId, operation, updatedAt = DateTimeOffset.UtcNow.ToString("O"), durableStateOwned = false };
            Directory.CreateDirectory(stateDirectory);
            File.WriteAllText(Path.Combine(stateDirectory, "preview-runtime.json"), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            if (operation == "load")
            {
                var subject = Text(request, "materializationSubjectFingerprint"); var bundle = Text(request, "previewBundleFingerprint");
                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(bundle)) return Response(sessionId, requestId, "invalid-request", "v2 load requires exact materializationSubjectFingerprint and previewBundleFingerprint");
                return new { schema = ResponseSchema, sessionId, requestId, status = "ok", operation, hostState = "ready", durableStateOwned = false, acknowledgedMaterializationSubjectFingerprint = subject, loadedMediaFingerprint = bundle };
            }
            return new { schema = ResponseSchema, sessionId, requestId, status = "ok", operation, hostState = shutdown ? "shutting-down" : "ready", durableStateOwned = false };
        }
        catch (JsonException) { return Response(sessionId, "", "invalid-request", "request is not valid JSON"); }
    }

    private static object Response(string sessionId, string requestId, string status, string diagnostic) => new { schema = ResponseSchema, sessionId, requestId, status, diagnostic, durableStateOwned = false };
    private static string Text(JsonElement value, string name) => value.TryGetProperty(name, out var result) && result.ValueKind == JsonValueKind.String ? result.GetString() ?? "" : "";
    private static string Short(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..24];
}
