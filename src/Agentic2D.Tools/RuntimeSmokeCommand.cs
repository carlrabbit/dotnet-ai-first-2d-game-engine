namespace Agentic2D.Tools;

public sealed record RuntimeSmokeCommand(int Ticks, string OutputDirectory)
{
    public static RuntimeSmokeParseResult TryParse(IReadOnlyList<string> args)
    {
        if (args.Count < 2 || args[0] != "runtime" || args[1] != "smoke")
        {
            return RuntimeSmokeParseResult.Failure("unknown command. expected: runtime smoke [--ticks <positive-integer>] --output <directory>");
        }

        var ticks = 3;
        string? output = null;

        for (var index = 2; index < args.Count; index++)
        {
            var current = args[index];

            switch (current)
            {
                case "--ticks":
                    if (++index >= args.Count)
                    {
                        return RuntimeSmokeParseResult.Failure("missing value for --ticks");
                    }

                    if (!int.TryParse(args[index], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out ticks) || ticks <= 0)
                    {
                        return RuntimeSmokeParseResult.Failure("--ticks must be a positive integer");
                    }

                    break;

                case "--output":
                    if (++index >= args.Count)
                    {
                        return RuntimeSmokeParseResult.Failure("missing value for --output");
                    }

                    output = args[index];
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        return RuntimeSmokeParseResult.Failure("--output must not be empty");
                    }

                    break;

                default:
                    return RuntimeSmokeParseResult.Failure($"unknown argument: {current}");
            }
        }

        return output is null
            ? RuntimeSmokeParseResult.Failure("missing required --output <directory>")
            : RuntimeSmokeParseResult.Success(new RuntimeSmokeCommand(ticks, output));
    }
}
