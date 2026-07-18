using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Agentic2D.Animation;
using Agentic2D.Contracts;
using Agentic2D.Input;
using Agentic2D.Persistence;
using Agentic2D.Presentation;

namespace AutonomousTicTacToe.Game;

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static int Main(string[] args)
    {
        var output = Option(args, "--output"); if (output is null) { Console.Error.WriteLine("usage: AutonomousTicTacToe.Game --scenario <id> --output <directory>"); return 2; }
        var scenario = Option(args, "--scenario") ?? "tic-tac-toe.ai-vs-ai-smoke"; var seed = Option(args, "--seed") ?? "tic-tac-toe-v1"; Directory.CreateDirectory(output);
        var result = Run(scenario, seed); File.WriteAllText(Path.Combine(output, "tic-tac-toe-result.json"), JsonSerializer.Serialize(result, Json));
        File.WriteAllText(Path.Combine(output, "tic-tac-toe-presentation.json"), JsonSerializer.Serialize(Present(result, seed), Json));
        var save = new SaveSnapshot("autonomous-tic-tac-toe.save.v1", result.SaveState, result.SimulationTick, result.RandomDrawCount, seed, new[] { "sound-commands", "particles", "prompt-instances", "completed-mark-animation" });
        File.WriteAllText(Path.Combine(output, "tic-tac-toe-save.json"), CanonicalJson.Serialize(new { save, fingerprint = CanonicalJson.Fingerprint(save) }));
        return result.Status == "passed" ? 0 : 1;
    }
    private static RunResult Run(string scenario, string seed)
    {
        var events = new List<object>(); var simulation = new Simulation(seed); var state = State.New(); events.Add(new { type = "round-start", cue = "cue.tic-tac-toe.round-start" });
        if (scenario is "tic-tac-toe.x-wins" or "tic-tac-toe.o-wins" or "tic-tac-toe.draw") state = ForcedRound(state, scenario, events, simulation);
        else if (scenario == "tic-tac-toe.save-during-thinking")
        {
            var uninterruptedSimulation = new Simulation(seed); var uninterrupted = Advance(State.New(), uninterruptedSimulation, [], 612);
            state = Advance(state, simulation, events, 12); var saved = state; var savedTick = simulation.Tick; var savedDraws = simulation.RandomDrawCount; var transientCount = events.Count;
            var restored = new Simulation(seed); restored.ReplayDraws(savedDraws); restored.Tick = savedTick;
            state = Advance(saved, restored, events, 600); simulation = restored; var equivalent = JsonSerializer.Serialize(uninterrupted) == JsonSerializer.Serialize(state);
            events.Add(new { type = "save-resume-equivalence", remainingTicksRestored = saved.ThinkingTicksRemaining, randomContinuationRestored = savedDraws, uninterruptedFinalStateMatches = equivalent, transientEffectsReplayed = false, preSaveTransientCount = transientCount });
        }
        else
        {
            if (scenario == "tic-tac-toe.human-takes-x") { state = ApplyAction(state, "participant.take-x", null, events, simulation); state = ApplyAction(state, "board.select-cell", 0, events, simulation); }
            else if (scenario == "tic-tac-toe.human-takes-o") { state = Advance(state, simulation, events, 120); state = ApplyAction(state, "participant.take-o", null, events, simulation); state = ApplyAction(state, "board.select-cell", FirstFree(state), events, simulation); }
            else if (scenario == "tic-tac-toe.release-control") { state = ApplyAction(state with { XController = "human", Phase = "awaiting-human-input" }, "participant.release-x", null, events, simulation); }
            else if (scenario == "tic-tac-toe.invalid-cell-rejected") { state = ApplyAction(state, "participant.take-x", null, events, simulation); state = ApplyAction(state, "board.select-cell", 0, events, simulation); state = ApplyAction(state, "board.select-cell", 0, events, simulation); }
            else if (scenario == "tic-tac-toe.round-reset") { state = Advance(state, simulation, events, 600); state = ApplyAction(state, "round.restart", null, events, simulation); }
            else state = Advance(state, simulation, events, 600);
        }
        var deterministic = scenario != "tic-tac-toe.deterministic-random-choice" || DeterministicProof(seed);
        if (scenario == "tic-tac-toe.deterministic-random-choice") events.Add(new { type = "deterministic-ai-proof", identicalMovesAndFinalState = deterministic });
        var status = deterministic && events.All(x => !JsonSerializer.Serialize(x).Contains("failure", StringComparison.Ordinal) && !JsonSerializer.Serialize(x).Contains("uninterruptedFinalStateMatches\":false", StringComparison.Ordinal)) ? "passed" : "failed";
        return new RunResult("autonomous-tic-tac-toe.run.v2", scenario, status, state, state, simulation.Tick, simulation.RandomDrawCount, events, new[] { "cue.tic-tac-toe.round-start", "cue.tic-tac-toe.thinking-start", "cue.tic-tac-toe.mark-x", "cue.tic-tac-toe.mark-o", "cue.tic-tac-toe.invalid-selection", "cue.tic-tac-toe.win", "cue.tic-tac-toe.draw", "cue.tic-tac-toe.human-takeover" });
    }
    private static State Advance(State state, Simulation simulation, List<object> events, int ticks)
    {
        for (var tick = 0; tick < ticks && state.Phase != "round-complete"; tick++)
        {
            simulation.Tick++;
            if (state.Phase == "resetting") { state = state with { Phase = "round-starting" }; continue; }
            if (state.Phase == "round-starting") { state = state with { Phase = "thinking", ThinkingTicksRemaining = simulation.Next(30, 91) }; events.Add(new { type = "thinking-start", mark = state.CurrentMark, cue = "cue.tic-tac-toe.thinking-start", ticks = state.ThinkingTicksRemaining, simulationTick = simulation.Tick }); continue; }
            if (state.Phase == "placing-mark") { state = state with { Phase = Controller(state, state.CurrentMark) == "human" ? "awaiting-human-input" : "thinking", ThinkingTicksRemaining = Controller(state, state.CurrentMark) == "human" ? 0 : simulation.Next(30, 91) }; continue; }
            if (state.Phase == "thinking" && state.ThinkingTicksRemaining > 1) { state = state with { ThinkingTicksRemaining = state.ThinkingTicksRemaining - 1 }; continue; }
            if (state.Phase == "thinking") { var free = Enumerable.Range(0, 9).Where(i => state.Cells[i] == "").ToArray(); if (free.Length == 0) break; state = Place(state, free[simulation.Next(0, free.Length)], events, simulation); }
        }
        return state;
    }
    private static State ForcedRound(State state, string scenario, List<object> events, Simulation simulation)
    {
        var moves = scenario switch { "tic-tac-toe.x-wins" => new[] { 0, 3, 1, 4, 2 }, "tic-tac-toe.o-wins" => new[] { 0, 3, 1, 4, 8, 5 }, _ => new[] { 0, 1, 2, 4, 3, 5, 7, 6, 8 } };
        foreach (var move in moves) state = Place(state, move, events, simulation); return state;
    }
    private static State Place(State state, int cell, List<object> events, Simulation simulation) { _ = TryPlace(state, cell, events, simulation, out var next); return next; }
    private static bool TryPlace(State state, int cell, List<object> events, Simulation simulation, out State next)
    {
        if (state.Phase == "round-complete" || cell is < 0 or > 8 || state.Cells[cell] != "") { events.Add(new { type = "selection-rejected", cell, reason = "cell-not-free", cue = "cue.tic-tac-toe.invalid-selection" }); next = state; return false; }
        var cells = state.Cells.ToArray(); cells[cell] = state.CurrentMark; var winner = Winner(cells); events.Add(new { type = "mark-placed", cell, mark = state.CurrentMark, cue = "cue.tic-tac-toe.mark-" + state.CurrentMark.ToLowerInvariant() });
        if (winner != "") { events.Add(new { type = "round-won", winner, cue = "cue.tic-tac-toe.win" }); next = state with { Cells = cells, Winner = winner, Phase = "round-complete", ScoreX = state.ScoreX + (winner == "X" ? 1 : 0), ScoreO = state.ScoreO + (winner == "O" ? 1 : 0), ThinkingTicksRemaining = 0 }; return true; }
        if (cells.All(x => x != "")) { events.Add(new { type = "round-draw", cue = "cue.tic-tac-toe.draw" }); next = state with { Cells = cells, Winner = "draw", Phase = "round-complete", DrawCount = state.DrawCount + 1, ThinkingTicksRemaining = 0 }; return true; }
        var mark = state.CurrentMark == "X" ? "O" : "X"; next = state with { Cells = cells, CurrentMark = mark, Phase = "placing-mark", ThinkingTicksRemaining = 0 }; return true;
    }
    private static bool DeterministicProof(string seed) { var aSimulation = new Simulation(seed); var bSimulation = new Simulation(seed); var a = Advance(State.New(), aSimulation, [], 600); var b = Advance(State.New(), bSimulation, [], 600); return JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b) && aSimulation.RandomDrawCount == bSimulation.RandomDrawCount; }
    private static State ApplyAction(State state, string action, int? cell, List<object> events, Simulation simulation)
    {
        // These are the consumer's semantic actions. No physical-device or native input state enters rule evaluation.
        return action switch
        {
            "participant.take-x" => Take(state, "X", events),
            "participant.take-o" => Take(state, "O", events),
            "participant.release-x" => Release(state, "X", events, simulation),
            "participant.release-o" => Release(state, "O", events, simulation),
            "board.select-cell" when cell is not null && Controller(state, state.CurrentMark) == "human" => Place(state, cell.Value, events, simulation),
            "board.select-cell" => Reject(state, cell ?? -1, "current-participant-is-ai", events),
            "round.restart" => Restart(state, events),
            _ => Reject(state, cell ?? -1, "unsupported-semantic-action", events),
        };
    }
    private static State Take(State state, string mark, List<object> events)
    {
        var next = mark == "X" ? state with { XController = "human" } : state with { OController = "human" };
        if (next.CurrentMark == mark) next = next with { Phase = "awaiting-human-input", ThinkingTicksRemaining = 0 };
        events.Add(new { type = "participant-taken", participant = mark.ToLowerInvariant(), action = "participant.take-" + mark.ToLowerInvariant(), cue = "cue.tic-tac-toe.human-takeover" }); return next;
    }
    private static State Release(State state, string mark, List<object> events, Simulation simulation)
    {
        var next = mark == "X" ? state with { XController = "ai" } : state with { OController = "ai" };
        if (next.CurrentMark == mark && next.Phase != "round-complete") next = next with { Phase = "thinking", ThinkingTicksRemaining = simulation.Next(30, 91) };
        events.Add(new { type = "participant-released", participant = mark.ToLowerInvariant(), action = "participant.release-" + mark.ToLowerInvariant() }); return next;
    }
    private static State Restart(State state, List<object> events) { events.Add(new { type = "round-reset", action = "round.restart", retainedScores = true }); return State.New(state.ScoreX, state.ScoreO, state.DrawCount, state.RoundNumber + 1, "resetting"); }
    private static State Reject(State state, int cell, string reason, List<object> events) { events.Add(new { type = "selection-rejected", cell, reason, action = "board.select-cell", cue = "cue.tic-tac-toe.invalid-selection" }); return state; }
    private static object Present(RunResult result, string seed)
    {
        var animation = new CompiledAnimation("agentic2d.animation-compiled.v1", "animation.tic-tac-toe.mark", "visual-definition.autonomous-tic-tac-toe.board", [new CompiledClip("place", 12, "once", [new CompiledTrack("mark-opacity", "mark", "visual.opacity", "scalar", "linear", [new CompiledKeyframe("zero", 0, 0, null), new CompiledKeyframe("one", 11, 1, null)]), new CompiledTrack("mark-scale", "mark", "visual.scale.x", "scalar", "linear", [new CompiledKeyframe("small", 0, .6, null), new CompiledKeyframe("full", 11, 1, null)])], [])], "consumer-authored");
        var sampled = new AnimationSampler().Sample(animation, new AnimationSelection("overlay", "place", "mark." + result.SimulationTick, "mark-placed", Math.Max(0, result.SimulationTick - 4)), result.SimulationTick);
        var effect = new EffectInstance("effect-instance.round-start", "effect.round-start", "effect-request.round-start", "round-start", 0, 20, seed, Math.Min(result.SimulationTick, 20), result.SimulationTick >= 20 ? "completed" : "active", ["particle-request.effect-instance.round-start"], "consumer-authored");
        var emitter = new ParticleEmitterDefinition("emitter.tic-tac-toe.round-start", "visual-definition.autonomous-tic-tac-toe.board", "selection", 12, 20, 16, [-.2, -.2], [.2, .2], [-.03, -.03], [.03, .03], [.5, 1], [0, 360], [-8, 8], [255, 212, 94, 255], [255, 255, 180, 255], "linear-inverse", "linear-inverse", "foreground");
        var particles = ParticleProjector.Sample(ParticleProjector.Spawn(emitter, effect, 0, "1.5,1.5", seed), Math.Min(result.SimulationTick, 15), "linear-inverse", "linear-inverse");
        var frame = new InputFrame("agentic2d.input-frame.v1", result.SimulationTick, result.SimulationTick, "input-source.player-1", "input-map.autonomous-tic-tac-toe", "consumer-v1", new Dictionary<string, DigitalActionValue> { ["participant.take-x"] = new(0, DigitalPhase.Inactive), ["participant.take-o"] = new(0, DigitalPhase.Inactive), ["participant.release-x"] = new(0, DigitalPhase.Inactive), ["participant.release-o"] = new(0, DigitalPhase.Inactive), ["board.select-cell"] = new(0, DigitalPhase.Inactive), ["round.restart"] = new(0, DigitalPhase.Inactive) }, new Dictionary<string, ScalarActionValue>(), new Dictionary<string, Vector2ActionValue>(), new Dictionary<string, PointerState>(), [], []);
        return new { board = "geometric-3x3", state = result.State.Phase, controllerLabels = new { x = result.State.XController, o = result.State.OController }, selectionHighlight = result.State.Phase == "awaiting-human-input", winnerLine = result.State.Winner, animation = new { sampled.Playback, sampled.Patches }, particles, semanticInputFrame = frame, presentationIsTransient = true };
    }
    private static int FirstFree(State state) => Array.FindIndex(state.Cells, x => x == "");
    private static string Controller(State state, string mark) => mark == "X" ? state.XController : state.OController;
    private static string Winner(string[] cells) { foreach (var row in new[] { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, new[] { 0, 4, 8 }, new[] { 2, 4, 6 } }) if (cells[row[0]] != "" && cells[row[0]] == cells[row[1]] && cells[row[1]] == cells[row[2]]) return cells[row[0]]; return ""; }
    private static string? Option(string[] args, string key) { var i = Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private sealed class Simulation
    {
        private readonly IDeterministicRandom random;
        public Simulation(string seed) => random = new ScenarioRandomSource(Seed(seed));
        public int Tick { get; set; }
        public int RandomDrawCount { get; private set; }
        public int Next(int minimumInclusive, int maximumExclusive) { RandomDrawCount++; return random.NextInt(minimumInclusive, maximumExclusive); }
        public void ReplayDraws(int count) { for (var i = 0; i < count; i++) _ = Next(0, 1); }
        private static int Seed(string value) => BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(value)), 0);
    }
    private sealed record State(string[] Cells, string CurrentMark, string XController, string OController, string Phase, int ThinkingTicksRemaining, string Winner, int RoundNumber, int ScoreX, int ScoreO, int DrawCount)
    { public static State New(int scoreX = 0, int scoreO = 0, int draws = 0, int round = 1, string phase = "round-starting") => new(["", "", "", "", "", "", "", "", ""], "X", "ai", "ai", phase, 0, "", round, scoreX, scoreO, draws); }
    private sealed record SaveSnapshot(string Schema, State State, int SimulationTick, int RandomDrawCount, string Seed, IReadOnlyList<string> ExcludedTransientState);
    private sealed record RunResult(string Schema, string Scenario, string Status, State State, State SaveState, int SimulationTick, int RandomDrawCount, IReadOnlyList<object> Events, IReadOnlyList<string> CueInventory);
}
