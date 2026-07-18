using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace Agentic2D.Workspaces;

public static class ExportCommands
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 2) return -1;
        try { return args[0..2] switch { ["project", "export"] => await ExportAsync(args, output, error), ["export", "inspect"] => await InspectAsync(args, output, error), ["export", "validate"] => await ValidateAsync(args, output, error), _ => -1 }; }
        catch (Exception e) { await error.WriteLineAsync("export failed: " + e.Message); return 1; }
    }
    private static async Task<int> ExportAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3) return await Usage(error, "project export requires <project-or-workspace>");
        var options = Options(args, 3); if (!options.TryGetValue("--output", out var destination)) return await Usage(error, "project export requires --output <directory>");
        if (options.TryGetValue("--target", out var target) && target != "linux-x64") return await Usage(error, "--target only supports linux-x64");
        var source = Path.GetFullPath(args[2]); if (!Directory.Exists(source)) return await Usage(error, "project export source does not exist");
        var game = Directory.Exists(Path.Combine(source, "game")) ? Path.Combine(source, "game") : Path.Combine(source, "game-content");
        if (!Directory.Exists(game)) return await Usage(error, "project export requires runtime game content");
        var final = Path.GetFullPath(destination); var parent = Path.GetDirectoryName(final)!; Directory.CreateDirectory(parent); var stage = Path.Combine(parent, ".agentic2d-export-stage-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(stage);
            var hostProject = Path.Combine(FindRoot(), "src", "Agentic2D.GameHost", "Agentic2D.GameHost.csproj");
            await Publish(hostProject, stage);
            CopyTree(game, Path.Combine(stage, "game"));
            var raylib = Path.Combine(FindRoot(), "src", "Agentic2D.DebugClient.Raylib", "bin", "Debug", "net10.0", "runtimes", "linux-x64", "native", "libraylib.so");
            if (!File.Exists(raylib)) throw new InvalidOperationException("required Linux raylib dependency was not built");
            Directory.CreateDirectory(Path.Combine(stage, "native")); File.Copy(raylib, Path.Combine(stage, "native", "libraylib.so"));
            Directory.CreateDirectory(Path.Combine(stage, "licenses")); await File.WriteAllTextAsync(Path.Combine(stage, "licenses", "dotnet-runtime.txt"), "Bundled .NET runtime files are distributed under their respective Microsoft licenses.\n");
            await File.WriteAllTextAsync(Path.Combine(stage, "licenses", "raylib.txt"), "raylib is bundled as the current Linux native presentation dependency; see its upstream zlib/libpng license.\n");
            var contentFingerprint = HashDirectory(Path.Combine(stage, "game")); var projectFingerprint = HashDirectory(game);
            var executable = Directory.EnumerateFiles(stage, "agentic2d-game", SearchOption.TopDirectoryOnly).SingleOrDefault() ?? throw new InvalidOperationException("publish did not produce agentic2d-game");
            var projectPath = Path.Combine(source, "agentic2d.project.json"); var gameId = "game.reference"; var displayName = "Agentic2D Reference Game"; var startupScenario = "presentation.persistent-world-player-facing-smoke";
            if (File.Exists(projectPath)) { using var project = JsonDocument.Parse(File.ReadAllText(projectPath)); gameId = project.RootElement.TryGetProperty("id", out var id) ? id.GetString() ?? gameId : gameId; displayName = gameId == "game.signal-passage" ? "Signal Passage" : gameId; startupScenario = project.RootElement.TryGetProperty("defaultScenarioId", out var scenario) ? scenario.GetString() ?? startupScenario : startupScenario; }
            var inventory = Inventory(stage); var manifest = new { schema = "agentic2d.standalone-linux-export.v1", exportId = "export." + Short(projectFingerprint), gameId, displayName, targetRid = "linux-x64", executable = Path.GetFileName(executable), contentRoot = "game", startupScenario, defaultMetrics = "off", defaultMode = "graphical", saveRootPolicy = "user-data/<game-id>/saves", artifactRootPolicy = "user-data/<game-id>/logs-or-artifacts; --output overrides", engineFingerprint = HashFile(hostProject), projectFingerprint, contentFingerprint, fileManifest = "export-files.json", exportFingerprint = "pending" };
            await File.WriteAllTextAsync(Path.Combine(stage, "export-files.json"), JsonSerializer.Serialize(new { schema = "agentic2d.export-file-inventory.v1", files = inventory }, Json));
            var fingerprint = HashText(JsonSerializer.Serialize(new { manifest, inventory })); var finalManifest = new { manifest.schema, manifest.exportId, manifest.gameId, manifest.displayName, manifest.targetRid, manifest.executable, manifest.contentRoot, manifest.startupScenario, manifest.defaultMetrics, manifest.defaultMode, manifest.saveRootPolicy, manifest.artifactRootPolicy, manifest.engineFingerprint, manifest.projectFingerprint, manifest.contentFingerprint, manifest.fileManifest, exportFingerprint = fingerprint };
            await File.WriteAllTextAsync(Path.Combine(stage, "agentic2d.export.json"), JsonSerializer.Serialize(finalManifest, Json));
            var validation = Validate(stage); if (!validation.Passed) throw new InvalidOperationException(string.Join("; ", validation.Diagnostics));
            await Write(output, "project export: passed; export: " + final);
            if (Directory.Exists(final)) Directory.Delete(final, true); Directory.Move(stage, final);
            return 0;
        }
        catch { if (Directory.Exists(stage)) Directory.Delete(stage, true); throw; }
    }
    private static async Task<int> InspectAsync(string[] args, TextWriter output, TextWriter error) { if (args.Length < 3 || !Options(args, 3).TryGetValue("--output", out var destination)) return await Usage(error, "export inspect requires <export-directory> --output <directory>"); var result = Validate(Path.GetFullPath(args[2])); Directory.CreateDirectory(destination); await File.WriteAllTextAsync(Path.Combine(destination, "export-manifest.json"), File.ReadAllText(Path.Combine(args[2], "agentic2d.export.json"))); await File.WriteAllTextAsync(Path.Combine(destination, "export-files.json"), File.ReadAllText(Path.Combine(args[2], "export-files.json"))); await File.WriteAllTextAsync(Path.Combine(destination, "export-diagnostics.json"), JsonSerializer.Serialize(result, Json)); await Write(output, "export inspect: " + (result.Passed ? "passed" : "failed")); return result.Passed ? 0 : 1; }
    private static async Task<int> ValidateAsync(string[] args, TextWriter output, TextWriter error) { if (args.Length < 3 || !Options(args, 3).TryGetValue("--output", out var destination)) return await Usage(error, "export validate requires <export-directory> --output <directory>"); var result = Validate(Path.GetFullPath(args[2])); Directory.CreateDirectory(destination); await File.WriteAllTextAsync(Path.Combine(destination, "export-validation.json"), JsonSerializer.Serialize(result, Json)); await File.WriteAllTextAsync(Path.Combine(destination, "export-diagnostics.json"), JsonSerializer.Serialize(result, Json)); await Write(output, "export validate: " + (result.Passed ? "passed" : "failed")); return result.Passed ? 0 : 1; }
    private static Validation Validate(string root) { var diagnostics = new List<string>(); try { var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "agentic2d.export.json"))).RootElement; if (manifest.GetProperty("targetRid").GetString() != "linux-x64") diagnostics.Add("unsupported target"); var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "export-files.json"))).RootElement.GetProperty("files"); foreach (var item in inventory.EnumerateArray()) { var path = item.GetProperty("path").GetString()!; if (Path.IsPathRooted(path) || path.Contains("..", StringComparison.Ordinal) || !File.Exists(Path.Combine(root, path)) || HashFile(Path.Combine(root, path)) != item.GetProperty("sha256").GetString()) diagnostics.Add("invalid inventory file: " + path); } if (!File.Exists(Path.Combine(root, manifest.GetProperty("executable").GetString()!))) diagnostics.Add("executable missing"); if (!Directory.Exists(Path.Combine(root, manifest.GetProperty("contentRoot").GetString()!))) diagnostics.Add("content root missing"); } catch (Exception e) { diagnostics.Add(e.Message); } return new("agentic2d.export-validation.v1", diagnostics.Count == 0, diagnostics); }
    private static async Task Publish(string project, string stage) { var p = Process.Start(new ProcessStartInfo("dotnet", "publish \"" + project + "\" -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o \"" + stage + "\"") { UseShellExecute = false }) ?? throw new InvalidOperationException("could not start dotnet publish"); await p.WaitForExitAsync(); if (p.ExitCode != 0) throw new InvalidOperationException("dotnet publish failed"); }
    private static object[] Inventory(string root) => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal).Select(path => new { path = Path.GetRelativePath(root, path).Replace('\\', '/'), category = path.EndsWith("agentic2d-game", StringComparison.Ordinal) ? "host" : path.Contains("/game/", StringComparison.Ordinal) ? "game-content" : path.Contains("licenses", StringComparison.Ordinal) ? "license" : path.EndsWith(".so", StringComparison.Ordinal) ? "native-runtime" : "managed-runtime", bytes = new FileInfo(path).Length, sha256 = HashFile(path), executable = !OperatingSystem.IsWindows() && File.GetUnixFileMode(path).HasFlag(UnixFileMode.UserExecute) }).Cast<object>().ToArray();
    private static Dictionary<string, string> Options(string[] args, int start) { var r = new Dictionary<string, string>(); for (var i = start; i < args.Length; i += 2) { if (i + 1 >= args.Length || !args[i].StartsWith("--")) throw new InvalidOperationException("options must be --name value pairs"); r.Add(args[i], args[i + 1]); } return r; }
    private static void CopyTree(string source, string destination) { var consumerContent = source.EndsWith("game-content", StringComparison.Ordinal); foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)) { var relative = Path.GetRelativePath(source, file); if (relative.StartsWith("artifacts" + Path.DirectorySeparatorChar, StringComparison.Ordinal) || (!consumerContent && relative.Contains("generated" + Path.DirectorySeparatorChar, StringComparison.Ordinal))) continue; var target = Path.Combine(destination, relative); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target); } }
    private static string FindRoot() { var d = Directory.GetCurrentDirectory(); while (!File.Exists(Path.Combine(d, "dotnet-ai-first-2d-game-engine.slnx"))) d = Directory.GetParent(d)?.FullName ?? throw new InvalidOperationException("repository root not found"); return d; }
    private static string HashFile(string p) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))).ToLowerInvariant(); private static string HashDirectory(string p) => HashText(string.Join("\n", Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal).Select(x => Path.GetRelativePath(p, x) + ":" + HashFile(x)))); private static string HashText(string v) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(v))).ToLowerInvariant(); private static string Short(string v) => v[..12]; private static Task Write(TextWriter writer, string text) => writer.WriteLineAsync(text);
    private static Task<int> Usage(TextWriter error, string message) { error.WriteLine(message); return Task.FromResult(2); }
    private sealed record Validation(string Schema, bool Passed, IReadOnlyList<string> Diagnostics);
}
