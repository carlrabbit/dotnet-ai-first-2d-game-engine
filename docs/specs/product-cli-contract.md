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

## Supported commands
agentic2d workspace create <target> --template minimal-game ... --output <directory>
agentic2d workspace validate <workspace> --output <directory>
agentic2d project validate <project-or-workspace> --output <directory>
agentic2d project run <project-or-workspace> --scenario <id> --output <run-directory>
agentic2d run inspect <run-directory> --output <directory>
agentic2d run review <run-directory> --output <directory>

The current command set is:

```text
agentic2d --help
agentic2d --version
agentic2d runtime smoke --output <directory>
agentic2d runtime inspect --scenario <scenario-id-or-path> [--map <map-id-or-path>] --output <directory>
agentic2d validate --output <directory>
agentic2d scenario run <scenario-id-or-path> --output <directory>
agentic2d content validate <scope-or-path> --output <directory>
agentic2d asset inspect <asset-id-or-path> --output <directory>
agentic2d asset perceive <asset-id-or-path> --output <directory>
agentic2d asset review apply --decisions <review-file> [--dry-run] --output <directory>
agentic2d map inspect <map-id-or-path> --output <directory>
agentic2d review pack --input <artifact-root> --output <directory>
agentic2d asset curate --asset <asset-id-or-path> --review-pack <review-pack-path> --output <directory>
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

### `agentic2d runtime inspect --scenario <scenario-id-or-path> [--map <map-id-or-path>] --output <directory>`

Projects supported deterministic scenario execution into structured runtime state evidence.

Required behavior:

- resolves and validates the scenario before execution;
- optionally resolves and validates `map.smoke` or a repository-relative `.map.json` path as a validated content reference, not a simulated map runtime;
- produces `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/runtime-summary.json`, `<directory>/entities.json`, `<directory>/commands.jsonl`, `<directory>/events.jsonl`, `<directory>/final-state.json`, `<directory>/assertions.json`, and `<directory>/content-references.json`;
- exits `0` when inspection completes and all supported assertions pass;
- exits `1` when inspection completes but validation, projection, or assertions fail;
- exits `2` for invalid CLI usage;
- exits `3` for runtime execution errors, artifact writing failures, or unhandled command failures.

### `agentic2d validate --output <directory>`

Runs current product-level validation for the repository maturity.

For Milestone 003, this command validates the minimal deterministic runtime through the product CLI. It may be narrow, but it must execute real engine behavior.

It must not claim to validate content, assets, maps, shaders, full scenarios, packaged runtime, public docs, or release artifacts.

### `agentic2d scenario run <scenario-id-or-path> --output <directory>`

Runs an authored scenario through the scenario runner.

Required behavior:

- invokes scenario runner behavior rather than duplicating scenario execution inside CLI parsing;
- supports file-path execution for `game/scenarios/smoke/runtime-smoke.json`;
- supports scenario ID execution for `runtime.smoke`;
- produces `<directory>/result.json`, `<directory>/events.jsonl`, and `<directory>/diagnostics.json`;
- exits `0` when scenario execution completes and assertions pass;
- exits `1` when scenario execution completes but assertions fail;
- exits `2` for invalid CLI usage or invalid scenario input;
- exits `3` for runtime execution errors, artifact writing failures, or unhandled command failures.

### `agentic2d content validate <scope-or-path> --output <directory>`

Validates authored content without requiring runtime or scenario execution.

Required initial behavior:

- supports `scenarios` scope;
- supports `assets` scope;
- supports `maps` scope;
- supports validating `game/scenarios/smoke/runtime-smoke.json` by path;
- supports validating `game/assets/metadata/tile-atlas-smoke.asset.json` by path;
- supports validating `game/maps/smoke/map-smoke.map.json` by path;
- parses scenario JSON and validates the supported `agentic2d.scenario.v1` contract;
- parses asset metadata JSON and validates the supported `agentic2d.asset-metadata.v1` contract;
- parses map JSON and validates the supported `agentic2d.map.v1` contract;
- validates stable IDs, duplicate IDs, supported command and assertion types, references, artifact declarations, and `humanReview.required`;
- validates asset source references, tile grid declarations, duplicate tile IDs/coordinates, provenance, and review-gated semantic approvals;
- validates map dimensions, layer/marker identities, cell bounds, and stable asset/tile references;
- produces `<directory>/result.json`, `<directory>/diagnostics.json`, and `<directory>/validated-items.json`;
- exits `0` when validation completes and passes;
- exits `1` when validation completes and content contract validation fails;
- exits `2` for invalid CLI usage, unsupported scope, malformed option, or invalid target form;
- exits `3` for unexpected validation or artifact writing failures.

### `agentic2d asset inspect <asset-id-or-path> --output <directory>`

Inspects authored asset metadata and the referenced raw PNG structurally.

Required behavior:

- supports asset ID `asset.tile-atlas-smoke`;
- supports repository-relative `.asset.json` metadata paths;
- loads and validates asset metadata before structural inspection;
- parses the raw PNG header to observe image width and height;
- verifies declared tile atlas dimensions match the PNG dimensions;
- produces `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/asset-summary.json`, and `<directory>/tiles.json`;
- exits `0` when inspection completes and passes;
- exits `1` when inspection completes but metadata or structural consistency checks fail;
- exits `2` for invalid CLI usage, unsupported target form, or malformed option;
- exits `3` for unexpected IO, parsing, artifact writing, or command failure.

### `agentic2d asset perceive <asset-id-or-path> --output <directory>`

Produces deterministic local structural and bounded visual observations for a supported tile-atlas PNG.

Required behavior:

- supports asset ID `asset.tile-atlas-smoke` and repository-relative `.asset.json` metadata paths;
- loads and validates asset metadata before decoding pixels;
- decodes supported PNG pixels locally and offline with deterministic ordering;
- produces `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/tile-features.json`, and `<directory>/semantic-proposals.json`;
- never writes approved gameplay truth automatically from proposals;
- exits `0` when perception completes and passes;
- exits `1` when perception completes but validation or extraction fails;
- exits `2` for invalid CLI usage;
- exits `3` for unexpected IO, decode, artifact writing, or command failure.

### `agentic2d asset review apply --decisions <review-file> [--dry-run] --output <directory>`

Validates authored review decisions and safely applies supported metadata mutations.

Required behavior:

- validates the authored decision source and target metadata;
- enforces an expected source fingerprint before any mutation;
- supports complete non-mutating dry-run behavior;
- produces `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/mutation-plan.json`, and `<directory>/validation-result.json`, plus `proposed-metadata.json` for dry-run;
- preserves unrelated metadata fields and never mutates raw assets;
- exits `0` when review application or dry-run passes;
- exits `1` when validation, fingerprint, or post-apply validation fails;
- exits `2` for invalid CLI usage;
- exits `3` for mutation write failures, artifact writing failures, or unhandled command failures.

### `agentic2d map inspect <map-id-or-path> --output <directory>`

Validates and inspects supported authored map content.

Required behavior:

- supports map ID `map.smoke` and repository-relative `.map.json` paths;
- validates map content before emitting summaries and resolved references;
- produces `<directory>/result.json`, `<directory>/diagnostics.json`, `<directory>/map-summary.json`, `<directory>/layers.json`, and `<directory>/resolved-references.json`;
- exits `0` when inspection completes and passes;
- exits `1` when inspection completes but validation fails;
- exits `2` for invalid CLI usage;
- exits `3` for unexpected IO, serialization, artifact writing, or command failure.

### `agentic2d review pack --input <artifact-root> --output <directory>`

Aggregates existing generated evidence into a bounded review pack.

Required behavior:

- supports artifact root `artifacts`;
- discovers scenario runner, content validation, and asset inspection artifact groups by known contract shape;
- recognizes asset perception, asset review apply, map inspection, and runtime inspection artifact groups when present;
- does not interpret arbitrary unknown files as product evidence;
- produces `<directory>/review-summary.md`, `<directory>/review-manifest.json`, and `<directory>/diagnostics.json`;
- exits `0` when pack generation completes and no error diagnostics exist;
- exits `1` when known artifact groups report failed/error statuses or contract-level errors;
- exits `2` for invalid CLI usage;
- exits `3` for unexpected IO, serialization, artifact writing, or command failure.

### `agentic2d asset curate --asset <asset-id-or-path> --review-pack <review-pack-path> --output <directory>`

Generates static asset curation workbench artifacts for human inspection.

Required behavior:

- supports asset ID `asset.tile-atlas-smoke`;
- supports repository-relative metadata path `game/assets/metadata/tile-atlas-smoke.asset.json`;
- consumes a review pack directory or `review-manifest.json` path;
- produces `<directory>/index.html`, `<directory>/review-data.json`, and `<directory>/diagnostics.json`;
- keeps proposed visual labels separate from approved physical/gameplay behaviors;
- does not modify source asset metadata or raw asset files;
- exits `0` when workbench generation completes and no error diagnostics exist;
- exits `1` when review pack or asset evidence is missing or malformed;
- exits `2` for invalid CLI usage;
- exits `3` for unexpected IO, serialization, artifact writing, or command failure.

## Required options

Artifact-producing commands must support:

```text
--output <directory>
```

The implementation may add aliases only if the canonical option remains supported.

## Output behavior

Artifact-producing commands with product-result contracts create:

```text
<output>/result.json
```

Commands with command-specific artifact contracts may define a different source-of-truth artifact, such as `review-manifest.json` or `review-data.json`.

Stdout should be concise and must include the artifact path or output directory.

## Exit codes

| Exit code | Meaning |
|---:|---|
| 0 | Command completed and validation passed. |
| 1 | Command completed and validation failed. |
| 2 | Invalid command-line usage or invalid scenario input. |
| 3 | Runtime execution error, artifact writing failure, or unhandled command failure. |

Invalid command names, missing required options, unknown options, and malformed option values must return exit code `2`.

## Determinism

Commands must keep semantic output deterministic for identical deterministic inputs and runtime implementation.

The result artifact may contain timestamps or durations, but tests must not use environment-specific values as semantic assertions.

## Relationship to `eng/`

`agentic2d` is the product/runtime CLI.

`eng/` scripts are repository engineering wrappers.

Engineering scripts may call the product CLI for validation, but product behavior must be documented here and in `docs/engineering/product-cli.md`, not hidden in `eng/` scripts.
