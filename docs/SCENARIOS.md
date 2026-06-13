# Scenarios

## Authority

This document indexes deterministic scenario validation concepts and scenario documents.

## Purpose

Scenarios validate runtime, content, asset import, UI, save/load, packaged-mode equivalence, and other behavior that ordinary unit tests cannot prove alone.

The repository currently has a built-in minimal runtime smoke scenario. It does not yet have a full scenario runner project.

## Current scenario documents

| Document | Authority area |
|---|---|
| `docs/scenarios/scenario-contract.md` | Placeholder contract for future scenario definitions. |
| `docs/scenarios/minimal-runtime-scenarios.md` | `runtime.smoke` scenario semantics introduced by Milestone 002. |

## Initial scenario categories

```text
smoke
gameplay
UI
asset-import
map-validation
animation-validation
shader-material-preview
save-load
performance
soak
regression
```

## Required scenario fields

```text
id
category
purpose
initial state
inputs
random seed policy
expected events
expected assertions
expected artifacts
human review requirements
debug-mode applicability
packaged-mode applicability
```

## Current validation surface

Milestone 002 validates the minimal runtime smoke path through the product CLI development invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

Milestone 003 also exposes product CLI engineering wrappers:

```bash
./eng/cli-smoke.sh
./eng/product-validate.sh
```

## Future scenario command shape

A future scenario runner milestone may introduce:

```text
agentic2d scenario run <scenario-id> --output artifacts/scenarios/<run-id>
```

or engineering wrappers such as:

```text
./eng/scenario.sh <scenario-id>
./eng/scenario-smoke.sh
./eng/scenario-packaged.sh <scenario-id>
```

Do not document these future commands as supported until implemented by a later milestone.
