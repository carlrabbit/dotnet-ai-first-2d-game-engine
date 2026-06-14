# ADR-0010 — Build Scenario Runner and Runtime Evidence Before Asset Workbench

## Status

Proposed for Milestone 005.

## Context

The repository has established:

```text
base engineering substrate
minimal deterministic runtime
first product CLI surface around the runtime
```

The project also has a deferred candidate direction for an asset curation workbench. That workbench will eventually need structured content validation, repeatable asset import checks, generated artifacts, and human-reviewable evidence.

Starting asset curation before the general scenario/evidence path exists would likely create one-off asset commands and reports that later need to be reworked into the engine's artifact-first validation model.

## Decision

Milestone 005 builds the scenario runner and runtime evidence foundation before asset workbench implementation.

The milestone introduces:

```text
authored scenario JSON
scenario runner foundation
agentic2d scenario run <scenario-id-or-path> --output <directory>
scenario result artifacts
scenario event log
scenario diagnostics
scenario smoke engineering wrapper
```

The first scenario remains runtime-focused:

```text
runtime.smoke
```

It proves that the engine can execute named deterministic scenarios and produce reviewable evidence.

## Consequences

Future asset, map, animation, shader, UI, save/load, packaged-runtime, and performance work can use the same scenario/evidence foundation instead of inventing separate validation surfaces.

The product CLI becomes more useful for agents because it can run named scenarios rather than only hardcoded validation commands.

The repository gains a clearer separation between:

```text
runtime behavior
scenario source
scenario execution
artifact reporting
human evidence review
```

## Alternatives considered

### Start asset curation workbench now

Rejected for the next milestone. Asset curation is important, but it should consume a scenario/artifact foundation rather than define its own one-off evidence model.

### Expand runtime inspection first

Deferred. Runtime inspection is useful, but scenario execution creates the product-level validation surface that will organize future inspection outputs.

### Build full scenario system now

Rejected. Milestone 005 should create a foundation, not a full scenario ecosystem. It should support one authored smoke scenario, deterministic execution, and useful artifacts without introducing all future scenario categories.

### Keep using `runtime smoke` only

Rejected. The hardcoded runtime smoke path proved the runtime and CLI. The next step needs authored scenario input so agents can add future scenario cases without changing product CLI command shape each time.

## Non-goals

Milestone 005 does not decide:

```text
asset taxonomy
visual labeling
map schema
animation schema
shader preview model
packaged runtime representation
performance benchmark model
human visual review pack format
```
