using System.Text.Json;

namespace SignalPassage.Game;

internal static class Program
{
    private static int Main(string[] args)
    {
        var index = Array.IndexOf(args, "--output");
        if (index < 0 || index + 1 >= args.Length) { Console.Error.WriteLine("usage: SignalPassage.Game --output <directory>"); return 2; }
        var output = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(output);
        var objective = new SignalPassageObjectiveComponent(0, false, false, false);
        var journey = new List<object> { new { step = 1, state = "objective-explained" }, new { step = 2, state = "container-opened", container = "container.alpha" } };
        objective = objective.Collect(); journey.Add(new { step = 3, state = "fragment-collected", count = objective.FragmentsCollected });
        journey.Add(new { step = 4, state = "hazard-damaged", health = 2 });
        journey.Add(new { step = 5, state = "mechanism-rejected", reason = "three-fragments-required" });
        objective = objective.Collect().Collect(); journey.Add(new { step = 6, state = "fragments-complete", count = objective.FragmentsCollected });
        objective = objective.Activate(); journey.Add(new { step = 7, state = "mechanism-activated", exitOpen = objective.ExitOpen });
        var save = new { schema = "signal-passage.save.v1", health = 2, fragments = objective.FragmentsCollected, mechanism = objective.MechanismActive, exit = objective.ExitOpen, transientFeedback = Array.Empty<string>() };
        File.WriteAllText(Path.Combine(output, "save.json"), JsonSerializer.Serialize(save, new JsonSerializerOptions { WriteIndented = true }));
        journey.Add(new { step = 8, state = "saved-and-resumed", restored = save, transientFeedbackReplayed = false });
        objective = objective.Complete(); journey.Add(new { step = 9, state = "objective-completed", completed = objective.Completed });
        File.WriteAllText(Path.Combine(output, "complete-journey.json"), JsonSerializer.Serialize(new { schema = "signal-passage.complete-journey.v1", status = objective.Completed ? "passed" : "failed", objective, health = 2, events = journey, cues = new[] { "fragment-collected", "container-opened", "player-damaged", "switch-activated", "exit-opened", "objective-completed" } }, new JsonSerializerOptions { WriteIndented = true }));
        return objective.Completed ? 0 : 1;
    }
}
