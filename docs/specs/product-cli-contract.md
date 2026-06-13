# Product CLI Contract

## Authority

This document is authoritative for the initial `agentic2d` product CLI behavior.

This document is not authoritative for repository engineering scripts. Engineering wrappers are defined by `docs/engineering/command-contract.md`.

## Purpose

The product CLI is the agent-facing and human-facing command surface for operating the engine/runtime without a graphical editor.

The product CLI must expose real engine behavior and produce structured evidence. It must not be a thin placeholder that succeeds without validating anything.

## Product CLI identity

The product command name is:

```text
agentic2d
```

Before packaging or tool installation exists, development invocation may use:

```bash
dotnet run --project src/Agentic2D.Tools -- <args>
```

## Initial supported commands

Milestone 003 supports this initial command set:

```text
agentic2d --help
agentic2d --version
agentic2d runtime smoke --output <directory>
agentic2d validate --output <directory>
```

## Command semantics

### `agentic2d --help`

Prints available commands and exits with code `0`.

It must not run the engine runtime.

### `agentic2d --version`

Prints the current CLI/runtime assembly version or a deterministic development version string and exits with code `0`.

It must not run the engine runtime.

### `agentic2d runtime smoke --output <directory>`

Runs the minimal deterministic runtime smoke path.

Required behavior:

- invokes the existing runtime implementation;
- produces `<directory>/result.json`;
- exits `0` when runtime smoke validation passes;
- exits `1` when runtime smoke validation completes but fails;
- exits `2` for invalid CLI usage;
- exits `3` for runtime execution errors.

### `agentic2d validate --output <directory>`

Runs current product-level validation for the repository maturity.

For Milestone 003, this command validates the minimal deterministic runtime through the product CLI. It may be narrow, but it must execute real engine behavior.

It must not claim to validate content, assets, maps, shaders, full scenarios, packaged runtime, public docs, or release artifacts.

## Required options

Artifact-producing commands must support:

```text
--output <directory>
```

The implementation may add aliases only if the canonical option remains supported.

## Output behavior

Artifact-producing commands must create:

```text
<output>/result.json
```

The result artifact is the source of truth for command outcome.

Stdout should be concise and must include the artifact path or output directory.

## Exit codes

| Exit code | Meaning |
|---:|---|
| 0 | Command completed and validation passed. |
| 1 | Command completed and validation failed. |
| 2 | Invalid command-line usage. |
| 3 | Runtime execution error or unhandled command failure. |

Invalid command names, missing required options, unknown options, and malformed option values must return exit code `2`.

## Determinism

Commands must keep semantic output deterministic for identical deterministic inputs and runtime implementation.

The result artifact may contain timestamps or durations, but tests must not use environment-specific values as semantic assertions.

## Relationship to `eng/`

`agentic2d` is the product/runtime CLI.

`eng/` scripts are repository engineering wrappers.

Engineering scripts may call the product CLI for validation, but product behavior must be documented here and in `docs/engineering/product-cli.md`, not hidden in `eng/` scripts.
