using System.Security.Cryptography;
using System.Text;
using Agentic2D.Animation;
using Agentic2D.Contracts;
using Agentic2D.Presentation;

namespace AutonomousTicTacToe.Game;

/// <summary>Consumer-owned deterministic model used by the playable launcher; it has no graphics dependency.</summary>
public sealed class LiveTicTacToeGame
{
    private const int AutonomousRoundRestartDelayTicks = 90;
    private static readonly CompiledAnimation MarkAnimation = new("agentic2d.animation-compiled.v1", "animation.tic-tac-toe.mark", "visual-definition.autonomous-tic-tac-toe.board", [new CompiledClip("place", 12, "once", [new CompiledTrack("mark-opacity", "mark", "visual.opacity", "scalar", "linear", [new CompiledKeyframe("zero", 0, 0, null), new CompiledKeyframe("one", 11, 1, null)]), new CompiledTrack("mark-scale", "mark", "visual.scale.x", "scalar", "linear", [new CompiledKeyframe("small", 0, .6, null), new CompiledKeyframe("full", 11, 1, null)])], [])], "consumer-authored");
    private static readonly ParticleEmitterDefinition RoundStartEmitter = new("emitter.tic-tac-toe.round-start", "visual-definition.autonomous-tic-tac-toe.board", "selection", 12, 20, 16, [-.2, -.2], [.2, .2], [-.03, -.03], [.03, .03], [.5, 1], [0, 360], [-8, 8], [255, 212, 94, 255], [255, 255, 180, 255], "linear-inverse", "linear-inverse", "foreground");

    private readonly IDeterministicRandom random;
    private readonly string seed;
    private string[] cells = ["", "", "", "", "", "", "", "", ""];
    private string current = "X", xController = "ai", oController = "ai", phase = "round-starting", winner = "", message = "AI X versus AI O", lastCue = "cue.tic-tac-toe.round-start";
    private int thinking, roundCompleteTicksRemaining, round = 1, scoreX, scoreO, draws, tick, lastPlacementTick = -1, lastPlacedCell = -1, roundStartTick;

    public LiveTicTacToeGame(string seed = "tic-tac-toe-live-v1")
    {
        this.seed = seed;
        random = new ScenarioRandomSource(BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(seed)), 0));
    }

    public LiveTicTacToeSnapshot Snapshot
    {
        get
        {
            var animation = lastPlacementTick < 0 ? null : new AnimationSampler().Sample(MarkAnimation, new AnimationSelection("overlay", "place", "mark." + lastPlacementTick, "mark-placed", lastPlacementTick), tick);
            var opacity = animation?.Patches.Single(x => x.Property == "visual.opacity").Scalar ?? 1;
            var scale = animation?.Patches.Single(x => x.Property == "visual.scale.x").Scalar ?? 1;
            var effect = new EffectInstance("effect-instance.round-start." + round, "effect.round-start", "effect-request.round-start." + round, "round-start", roundStartTick, 20, seed, Math.Max(0, tick - roundStartTick), tick - roundStartTick >= 20 ? "completed" : "active", ["particle-request.effect-instance.round-start." + round], "consumer-authored");
            var particles = ParticleProjector.Sample(ParticleProjector.Spawn(RoundStartEmitter, effect, roundStartTick, "1.5,1.5", seed), tick, "linear-inverse", "linear-inverse");
            return new(cells.ToArray(), current, xController, oController, phase, thinking, roundCompleteTicksRemaining, winner, round, scoreX, scoreO, draws, message, tick, lastCue, lastPlacedCell, opacity, scale, particles);
        }
    }

    public void AdvanceOneTick()
    {
        tick++;
        if (phase == "round-complete" && IsAutonomousRound())
        {
            if (--roundCompleteTicksRemaining <= 0) Reset();
            else message = $"{(winner == "draw" ? "Draw" : winner + " wins")} — next autonomous round in {roundCompleteTicksRemaining} ticks.";
            return;
        }
        if (phase == "resetting") { phase = "round-starting"; return; }
        if (phase == "round-starting") { StartTurn(); return; }
        if (phase == "placing-mark") { StartTurn(); return; }
        if (phase != "thinking") return;
        if (--thinking > 0) return;
        var free = Enumerable.Range(0, 9).Where(index => cells[index] == "").ToArray();
        if (free.Length > 0) Place(free[random.NextInt(0, free.Length)]);
    }

    public void Apply(string action, int? cell = null)
    {
        switch (action)
        {
            case "participant.take-x": Take("X"); break;
            case "participant.take-o": Take("O"); break;
            case "participant.release-x": Release("X"); break;
            case "participant.release-o": Release("O"); break;
            case "board.select-cell" when cell is not null && Controller(current) == "human" && phase == "awaiting-human-input": Place(cell.Value); break;
            case "board.select-cell": Reject("the current participant is not allowed to place there"); break;
            case "round.restart": Reset(); break;
        }
    }

    private void StartTurn()
    {
        if (Controller(current) == "human") { phase = "awaiting-human-input"; thinking = 0; message = $"{current}: select a free cell."; return; }
        phase = "thinking";
        thinking = random.NextInt(30, 91);
        lastCue = "cue.tic-tac-toe.thinking-start";
        message = $"{current} is thinking…";
    }

    private void Take(string mark)
    {
        if (mark == "X") xController = "human"; else oController = "human";
        lastCue = "cue.tic-tac-toe.human-takeover";
        if (current == mark && phase != "round-complete") { phase = "awaiting-human-input"; thinking = 0; }
        message = $"{mark} is now human-controlled.";
    }

    private void Release(string mark)
    {
        if (mark == "X") xController = "ai"; else oController = "ai";
        if (current == mark && phase != "round-complete") StartTurn();
        message = $"{mark} is now AI-controlled.";
    }

    private void Place(int cell)
    {
        if (cell is < 0 or > 8 || cells[cell] != "") { Reject("choose a free board cell"); return; }
        cells[cell] = current;
        lastPlacedCell = cell;
        lastPlacementTick = tick;
        lastCue = "cue.tic-tac-toe.mark-" + current.ToLowerInvariant();
        var result = Winner(cells);
        if (result != "")
        {
            winner = result;
            phase = "round-complete";
            roundCompleteTicksRemaining = IsAutonomousRound() ? AutonomousRoundRestartDelayTicks : 0;
            if (result == "X") scoreX++; else scoreO++;
            lastCue = "cue.tic-tac-toe.win";
            message = IsAutonomousRound() ? $"{result} wins — next autonomous round starts shortly." : $"{result} wins — press R to start the next round.";
            return;
        }
        if (cells.All(value => value != ""))
        {
            winner = "draw";
            phase = "round-complete";
            roundCompleteTicksRemaining = IsAutonomousRound() ? AutonomousRoundRestartDelayTicks : 0;
            draws++;
            lastCue = "cue.tic-tac-toe.draw";
            message = IsAutonomousRound() ? "Draw — next autonomous round starts shortly." : "Draw — press R to start the next round.";
            return;
        }
        current = current == "X" ? "O" : "X";
        phase = "placing-mark";
        thinking = 0;
    }

    private void Reset()
    {
        cells = ["", "", "", "", "", "", "", "", ""];
        current = "X";
        winner = "";
        phase = "resetting";
        thinking = 0;
        roundCompleteTicksRemaining = 0;
        round++;
        roundStartTick = tick;
        lastPlacementTick = -1;
        lastPlacedCell = -1;
        lastCue = "cue.tic-tac-toe.round-start";
        message = "New round.";
    }

    private void Reject(string reason)
    {
        lastCue = "cue.tic-tac-toe.invalid-selection";
        message = "Selection rejected: " + reason + ".";
    }

    private string Controller(string mark) => mark == "X" ? xController : oController;
    private bool IsAutonomousRound() => xController == "ai" && oController == "ai";

    private static string Winner(string[] board)
    {
        foreach (var line in new[] { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, new[] { 0, 4, 8 }, new[] { 2, 4, 6 } })
            if (board[line[0]] != "" && board[line[0]] == board[line[1]] && board[line[1]] == board[line[2]]) return board[line[0]];
        return "";
    }
}

public sealed record LiveTicTacToeSnapshot(string[] Cells, string CurrentMark, string XController, string OController, string Phase, int ThinkingTicksRemaining, int RoundCompleteTicksRemaining, string Winner, int RoundNumber, int ScoreX, int ScoreO, int DrawCount, string Message, int Tick, string LastCue, int LastPlacedCell, double MarkOpacity, double MarkScale, IReadOnlyList<ParticleSample> RoundStartParticles);
