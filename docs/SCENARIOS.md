# Scenarios

## Authority

This document indexes deterministic scenario validation.

## Purpose

Scenarios validate behavior that unit tests alone cannot prove, including runtime state transitions, content linkage, spatial outcomes, interactions, and render projection.

## Current implemented smoke families

| Milestone | Representative scenarios |
|---|---|
| M002–M006 | `runtime.smoke` and authored scenario-runner validation. |
| M011 | Bounded asset/map/runtime inspection journey. |
| M012 | `behavior.grid-movement-smoke`, `behavior.grid-movement-rejected-smoke`. |
| M013 | Entity/component runtime and continuous kinematic movement/tree collision scenarios. |
| M018 | Consumer `scenario.minimal.smoke` through `agentic2d project run`. |
| M014 | `entity.definition-instantiation-smoke`, `trigger.enter-exit-smoke`, `interaction.npc-smoke`. |
| M015 | Headless rendering of `interaction.npc-smoke` and snapshot reconstruction. |
| M016 | `input.mapping-mixed-device-smoke`, `input.runtime-approach-and-interact-smoke`, and `input.semantic-replay-smoke`. |
| M017 | `animation-player-locomotion-smoke`, `animation-overlay-marker-smoke`, and `animation-semantic-replay-smoke`. |
| M031 | `scenario.m031.simulation-foundation.wood-workflow`: deterministic two-region harvest/deposit proof with a fresh-process save/load continuation. |
| M033 | `scenario.m033.multi-region-equivalence-and-switching`: three persistent regions, one detailed owner, abstract event advancement, repeated switches, and thirty-day controls. |
| M035 | `campaign.m035.heavy-internal-testing-readiness`: five-region supported-scale, fault, compatibility, transition, save-cycle, 365-day headless-soak, and four-hour graphical-soak readiness campaign. |
| M044 | `scenario.m044.canonical-save-resume-and-recovery`: process-separated canonical save continuation, identity preservation, product Continue, corruption recovery, and independent comparison. |

## Current commands

```bash
dotnet run --project src/Agentic2D.Tools -- scenario run <scenario-id-or-path> --output <directory>
dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario <id> --map <id> --output <directory>
dotnet run --project src/Agentic2D.Tools -- render project --scenario <id> --tick final --output <directory>
```

Graphical live and snapshot presentation is provided by `src/Agentic2D.DebugClient.Raylib`; it does not define scenario semantics.
dotnet run --project src/Agentic2D.Tools -- project run <project-or-workspace> --scenario <id> --output <run-directory>
dotnet run --project src/Agentic2D.Tools -- run inspect <run-directory> --output <directory>
dotnet run --project src/Agentic2D.Tools -- simulation wood-workflow --output artifacts/simulation/M031
dotnet run --project src/Agentic2D.Tools -- simulation run scenario.m033.multi-region-equivalence-and-switching --until 30d --mode abstract --output artifacts/simulation/M033
dotnet run --project src/Agentic2D.Tools -- simulation m035-readiness --output artifacts/readiness/M035

## Required scenario qualities

Scenarios use stable IDs, deterministic inputs and tick behavior, explicit assertions, expected events/artifacts, structured diagnostics, and declared human-review requirements.

Do not document future UI, save/load, animation, audio, performance, packaged-mode, or replay scenarios as implemented until a milestone establishes them.
