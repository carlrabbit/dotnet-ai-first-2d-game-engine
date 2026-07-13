# Guide Sync Hint — Milestone 014 Index and Cross-Link Synchronization

## Status

Pending.

## Purpose

After implementation stabilizes, synchronize indexes and cross-links for definitions, spawns, instantiation, spatial queries, triggers, interactions, artifacts, wrappers, ADR-0017, and Milestone 014.

## Review

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/SCENARIOS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/specs/runtime-principles.md
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/behavior-modules.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/map-content-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/review-pack-contract.md
docs/specs/entity-component-runtime-contract.md
docs/specs/mixed-world-projection-contract.md
docs/specs/continuous-kinematic-spatial-module-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/artifacts/entity-component-continuous-spatial-artifact-contract.md
```

## Completion criteria

Delete only when:

- Milestone 014 and ADR-0017 are indexed;
- new specs and artifact contract are indexed;
- entity-definition roots and validation commands are discoverable;
- scenarios and wrappers are indexed;
- docs distinguish definitions, spawns, runtime entities, and static objects;
- docs state interactions stop at `interaction.started`;
- unsupported inheritance, physical entity collision, static triggers, trigger-stayed, dialogue, inventory, rendering, and save/load are not documented as current.
