# Scenarios

## Authority

This document indexes deterministic scenario validation concepts and scenario documents.

## Purpose

Scenarios validate runtime, content, asset import, UI, save/load, packaged-mode equivalence, and other behavior that ordinary unit tests cannot prove alone.

The repository currently has a built-in minimal runtime smoke path and an authored `runtime.smoke` scenario executed by the scenario runner foundation.

## Current scenario documents

| Document | Authority area |
|---|---|
| `docs/scenarios/scenario-contract.md` | Placeholder contract for future scenario definitions. |
| `docs/scenarios/minimal-runtime-scenarios.md` | `runtime.smoke` scenario semantics introduced by Milestone 002. |
| `docs/scenarios/scenario-runner-foundation.md` | Authored scenario runner foundation and `runtime.smoke` scenario source semantics introduced by Milestone 005. |

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

Milestone 005 exposes authored scenario execution:

```bash
dotnet run --project src/Agentic2D.Tools -- scenario run game/scenarios/smoke/runtime-smoke.json --output artifacts/scenarios/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- scenario run runtime.smoke --output artifacts/scenarios/runtime-smoke
./eng/scenario-smoke.sh
```

## Future scenario command shape

Future scenario runner milestones may introduce broader scenario wrappers such as:

```text
./eng/scenario.sh <scenario-id>
./eng/scenario-packaged.sh <scenario-id>
```

Do not document these future commands as supported until implemented by a later milestone.
