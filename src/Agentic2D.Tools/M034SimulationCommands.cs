using Agentic2D.Simulation;

namespace Agentic2D.Tools;

internal static class M034SimulationCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 2 || args[0] != "simulation") return -1;
        var outputIndex = Array.IndexOf(args, "--output");
        if (args[1] != "m034-settlement" || outputIndex < 0 || outputIndex + 1 >= args.Length) return -1;
        try
        {
            var state = await M034ArtifactWriter.WriteAsync(args[outputIndex + 1]);
            if (!state.PersistenceRoundTrip || !state.ShortageRecovered || !state.StorageRecovered || !state.MaintenanceRecovered || !state.Sustained) throw new InvalidOperationException("M034 proof invariant failed");
            await output.WriteLineAsync("simulation M034 settlement: passed; output: " + args[outputIndex + 1]);
            return 0;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or System.Text.Json.JsonException)
        {
            await error.WriteLineAsync("simulation M034 settlement failed: " + exception.Message);
            return 1;
        }
    }
}
