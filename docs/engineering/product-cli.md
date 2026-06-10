# Product CLI

## Authority

This document is authoritative for the product/runtime CLI command surface.

This document is not authoritative for repository engineering scripts under `eng/`; those are defined in `docs/engineering/command-contract.md`.

## Product CLI principle

The product CLI is the engine/runtime API for agents, CI, and humans. It is separate from `eng/` repository scripts.

The product CLI should expose meaningful engine behavior and produce machine-readable output where practical.

## Current command surface

Milestone 002 introduces the first required product CLI command through `Agentic2D.Tools`:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --output artifacts/runtime-smoke
```

Equivalent explicit form:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

## `runtime smoke`

### Purpose

Run the minimal deterministic runtime smoke scenario and write a machine-readable result artifact.

### Syntax

```text
runtime smoke [--ticks <positive-integer>] --output <directory>
```

### Defaults

```text
--ticks 3
```

There is no default output directory. The caller must provide `--output`.

### Deterministic behavior

The command must run the smoke behavior defined by:

```text
docs/specs/minimal-deterministic-runtime.md
docs/scenarios/minimal-runtime-scenarios.md
```

Meaningful result contents must be stable for identical arguments and source revision.

### Output path

The command writes:

```text
<output>/result.json
```

For the required smoke example:

```text
artifacts/runtime-smoke/result.json
```

### Artifact schema

The result artifact must conform to:

```text
docs/artifacts/runtime-result-contract.md
```

### Diagnostics behavior

The command must produce useful diagnostics for:

```text
unknown command
invalid --ticks value
missing --output value
runtime assertion failure
artifact write failure
```

Diagnostics may be printed to stderr. When execution reaches result creation, diagnostics should also appear in `result.json`.

### Exit codes

| Exit code | Meaning |
|---:|---|
| 0 | Command executed and runtime result status is `passed`. |
| 1 | Command executed but runtime result status is `failed`. |
| 2 | CLI usage error such as unknown command, invalid argument, missing argument, or invalid value. |
| 3 | Runtime or artifact write error. |

If the implementation uses a simpler initial exit-code model, it must still distinguish success from failure with `0` vs non-zero and document the exact mapping here.

### Required validation command

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

Then inspect:

```text
artifacts/runtime-smoke/result.json
```

Expected result:

```text
status == passed
finalTick == 3
entity.player final position == 1
expected events are present
```

## Future command candidates

These commands remain future candidates and must not be implemented by Milestone 002 unless separately scoped:

```text
agentic2d validate
agentic2d scenario run <scenario-id>
agentic2d asset inspect <path>
agentic2d map preview <map-id>
agentic2d content validate <scope>
```

## Command contract fields for future commands

Each future command must define:

- purpose;
- input syntax;
- deterministic behavior;
- output path;
- artifact schema;
- diagnostics behavior;
- exit codes;
- examples;
- validation command.
