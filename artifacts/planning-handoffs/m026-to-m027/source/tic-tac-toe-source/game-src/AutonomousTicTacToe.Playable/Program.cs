using Agentic2D.DebugClient.Raylib;
using AutonomousTicTacToe.Game;

if (args.Contains("--verify-autonomous-rounds", StringComparer.Ordinal))
{
    var verificationGame = new LiveTicTacToeGame("tic-tac-toe-autonomous-round-verification");
    for (var tick = 0; tick < 10_000 && verificationGame.Snapshot.RoundNumber < 2; tick++) verificationGame.AdvanceOneTick();
    var verification = verificationGame.Snapshot;
    Console.WriteLine($"autonomous-round-verification: round={verification.RoundNumber}; tick={verification.Tick}; phase={verification.Phase}; cells={string.Concat(verification.Cells.Select(cell => cell == "" ? "-" : cell))}");
    return verification.RoundNumber >= 2 ? 0 : 1;
}

var frames = Array.IndexOf(args, "--frames") is var index && index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var parsed) ? parsed : (int?)null;
var game = new LiveTicTacToeGame();
TicTacToeWindow.Show("Autonomous Tic-Tac-Toe", () => ToAdapter(game.Snapshot), game.AdvanceOneTick, game.Apply, frames);
return 0;

static TicTacToeWindowSnapshot ToAdapter(LiveTicTacToeSnapshot state) => new(state.Cells, state.CurrentMark, state.XController, state.OController, state.Phase, state.Winner, state.RoundNumber, state.ScoreX, state.ScoreO, state.DrawCount, state.Message, state.Tick, state.LastPlacedCell, state.MarkOpacity, state.MarkScale, state.RoundStartParticles.Select(particle => new TicTacToeBurstParticle(particle.X, particle.Y, particle.Scale, particle.Opacity)).ToArray());
