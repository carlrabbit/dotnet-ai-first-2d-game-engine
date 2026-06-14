using System.Globalization;

namespace Agentic2D.Tools;

public static class ToolsCliParser
{
    public static CliParseResult TryParse(IReadOnlyList<string> args)
    {
        if (args.Count == 2 && args[0] == "runtime" && args[1] == "smoke")
        {
            return CliParseResult.Failure("missing required --output <directory>");
        }

        if (args.Count >= 2 && args[0] == "runtime" && args[1] == "smoke")
        {
            return TryParseRuntimeSmoke(args);
        }

        if (args.Count >= 2 && args[0] == "scenario" && args[1] == "run")
        {
            return TryParseScenarioRun(args);
        }

        if (args.Count >= 2 && args[0] == "content" && args[1] == "validate")
        {
            return TryParseContentValidate(args);
        }

        if (args.Count >= 3 && args[0] == "asset" && args[1] == "inspect")
        {
            return TryParseAssetInspect(args);
        }

        if (args.Count >= 2 && args[0] == "review" && args[1] == "pack")
        {
            return TryParseReviewPack(args);
        }

        if (args.Count >= 2 && args[0] == "asset" && args[1] == "curate")
        {
            return TryParseAssetCurate(args);
        }

        if (args.Count >= 1 && args[0] == "validate")
        {
            return TryParseValidate(args);
        }

        return CliParseResult.Failure("unknown command. expected: runtime smoke --output <directory>, validate --output <directory>, scenario run <scenario-id-or-path> --output <directory>, content validate <scope-or-path> --output <directory>, asset inspect <asset-id-or-path> --output <directory>, review pack --input <artifact-root> --output <directory>, or asset curate --asset <asset-id-or-path> --review-pack <review-pack-path> --output <directory>");
    }

    private static CliParseResult TryParseRuntimeSmoke(IReadOnlyList<string> args)
    {
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
                        return CliParseResult.Failure("missing value for --ticks");
                    }

                    if (!int.TryParse(args[index], NumberStyles.None, CultureInfo.InvariantCulture, out ticks) || ticks <= 0)
                    {
                        return CliParseResult.Failure("--ticks must be a positive integer");
                    }

                    break;

                case "--output":
                    if (++index >= args.Count)
                    {
                        return CliParseResult.Failure("missing value for --output");
                    }

                    output = args[index];
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        return CliParseResult.Failure("--output must not be empty");
                    }

                    break;

                default:
                    return CliParseResult.Failure($"unknown argument: {current}");
            }
        }

        return output is null
            ? CliParseResult.Failure("missing required --output <directory>")
            : CliParseResult.Success(new CliCommand("runtime smoke", output, ticks));
    }

    private static CliParseResult TryParseValidate(IReadOnlyList<string> args)
    {
        string? output = null;

        for (var index = 1; index < args.Count; index++)
        {
            var current = args[index];

            switch (current)
            {
                case "--output":
                    if (++index >= args.Count)
                    {
                        return CliParseResult.Failure("missing value for --output");
                    }

                    output = args[index];
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        return CliParseResult.Failure("--output must not be empty");
                    }

                    break;

                default:
                    return CliParseResult.Failure($"unknown argument: {current}");
            }
        }

        return output is null
            ? CliParseResult.Failure("missing required --output <directory>")
            : CliParseResult.Success(new CliCommand("validate", output));
    }

    private static CliParseResult TryParseScenarioRun(IReadOnlyList<string> args)
    {
        if (args.Count < 3 || args[2].StartsWith("--", StringComparison.Ordinal))
        {
            return CliParseResult.Failure("missing required scenario ID or path");
        }

        var scenarioReference = args[2];
        string? output = null;

        for (var index = 3; index < args.Count; index++)
        {
            var current = args[index];

            switch (current)
            {
                case "--output":
                    if (++index >= args.Count)
                    {
                        return CliParseResult.Failure("missing value for --output");
                    }

                    output = args[index];
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        return CliParseResult.Failure("--output must not be empty");
                    }

                    break;

                default:
                    return CliParseResult.Failure($"unknown argument: {current}");
            }
        }

        return output is null
            ? CliParseResult.Failure("missing required --output <directory>")
            : CliParseResult.Success(new CliCommand("scenario run", output, ScenarioReference: scenarioReference));
    }

    private static CliParseResult TryParseContentValidate(IReadOnlyList<string> args)
    {
        if (args.Count < 3 || args[2].StartsWith("--", StringComparison.Ordinal))
        {
            return CliParseResult.Failure("missing required content scope or path");
        }

        var contentTarget = args[2];
        string? output = null;

        for (var index = 3; index < args.Count; index++)
        {
            var current = args[index];

            switch (current)
            {
                case "--output":
                    if (++index >= args.Count)
                    {
                        return CliParseResult.Failure("missing value for --output");
                    }

                    output = args[index];
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        return CliParseResult.Failure("--output must not be empty");
                    }

                    break;

                default:
                    return CliParseResult.Failure($"unknown argument: {current}");
            }
        }

        return output is null
            ? CliParseResult.Failure("missing required --output <directory>")
            : CliParseResult.Success(new CliCommand("content validate", output, ContentTarget: contentTarget));
    }

    private static CliParseResult TryParseAssetInspect(IReadOnlyList<string> args)
    {
        if (args.Count < 3 || args[2].StartsWith("--", StringComparison.Ordinal))
        {
            return CliParseResult.Failure("missing required asset ID or path");
        }

        var assetTarget = args[2];
        string? output = null;

        for (var index = 3; index < args.Count; index++)
        {
            var current = args[index];

            switch (current)
            {
                case "--output":
                    if (++index >= args.Count)
                    {
                        return CliParseResult.Failure("missing value for --output");
                    }

                    output = args[index];
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        return CliParseResult.Failure("--output must not be empty");
                    }

                    break;

                default:
                    return CliParseResult.Failure($"unknown argument: {current}");
            }
        }

        return output is null
            ? CliParseResult.Failure("missing required --output <directory>")
            : CliParseResult.Success(new CliCommand("asset inspect", output, AssetTarget: assetTarget));
    }

    private static CliParseResult TryParseReviewPack(IReadOnlyList<string> args)
    {
        string? input = null;
        string? output = null;

        for (var index = 2; index < args.Count; index++)
        {
            var current = args[index];

            switch (current)
            {
                case "--input":
                    if (++index >= args.Count)
                    {
                        return CliParseResult.Failure("missing value for --input");
                    }

                    input = args[index];
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        return CliParseResult.Failure("--input must not be empty");
                    }

                    break;

                case "--output":
                    if (++index >= args.Count)
                    {
                        return CliParseResult.Failure("missing value for --output");
                    }

                    output = args[index];
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        return CliParseResult.Failure("--output must not be empty");
                    }

                    break;

                default:
                    return CliParseResult.Failure($"unknown argument: {current}");
            }
        }

        if (input is null)
        {
            return CliParseResult.Failure("missing required --input <artifact-root>");
        }

        return output is null
            ? CliParseResult.Failure("missing required --output <directory>")
            : CliParseResult.Success(new CliCommand("review pack", output, InputDirectory: input));
    }

    private static CliParseResult TryParseAssetCurate(IReadOnlyList<string> args)
    {
        string? asset = null;
        string? reviewPack = null;
        string? output = null;

        for (var index = 2; index < args.Count; index++)
        {
            var current = args[index];

            switch (current)
            {
                case "--asset":
                    if (++index >= args.Count)
                    {
                        return CliParseResult.Failure("missing value for --asset");
                    }

                    asset = args[index];
                    if (string.IsNullOrWhiteSpace(asset))
                    {
                        return CliParseResult.Failure("--asset must not be empty");
                    }

                    break;

                case "--review-pack":
                    if (++index >= args.Count)
                    {
                        return CliParseResult.Failure("missing value for --review-pack");
                    }

                    reviewPack = args[index];
                    if (string.IsNullOrWhiteSpace(reviewPack))
                    {
                        return CliParseResult.Failure("--review-pack must not be empty");
                    }

                    break;

                case "--output":
                    if (++index >= args.Count)
                    {
                        return CliParseResult.Failure("missing value for --output");
                    }

                    output = args[index];
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        return CliParseResult.Failure("--output must not be empty");
                    }

                    break;

                default:
                    return CliParseResult.Failure($"unknown argument: {current}");
            }
        }

        if (asset is null)
        {
            return CliParseResult.Failure("missing required --asset <asset-id-or-path>");
        }

        if (reviewPack is null)
        {
            return CliParseResult.Failure("missing required --review-pack <review-pack-path>");
        }

        return output is null
            ? CliParseResult.Failure("missing required --output <directory>")
            : CliParseResult.Success(new CliCommand("asset curate", output, AssetTarget: asset, ReviewPackPath: reviewPack));
    }
}
