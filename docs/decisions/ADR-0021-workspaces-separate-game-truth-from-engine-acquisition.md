# ADR-0021 — Workspaces Separate Game Truth from Engine Acquisition

## Status

Accepted for Milestone 018.

## Context

The product CLI currently operates primarily from the engine repository. Agents need a stable consumer workspace that can use engine source by reference, copy, or Git acquisition without mixing checkout composition into game-project truth.

## Decision

Use two manifests:

```text
agentic2d.project.json
  game/product truth

agentic2d.workspace.json
  engine acquisition, checkout composition, mutation policy, artifacts
```

Implement directory-reference, directory-copy, and exact-revision Git providers.

Support deterministic transactional workspace creation and validation.

Do not support NuGet, workspace update/migration, force overwrite, dynamic acquisition plugins, or portable SDK implementation in M018.

## Consequences

Positive:

- stable agent workflow across acquisition modes;
- source-based engine development remains first-class;
- consumer and provider responsibilities are explicit;
- integration tests can create real workspaces deterministically;
- future portable SDK can fit without changing game-project truth.

Costs:

- new manifests and provider logic;
- scaffolding/template maintenance;
- transactional filesystem behavior;
- explicit mutation-policy enforcement;
- no automatic workspace upgrade path.

## Rejected alternatives

NuGet-first consumption, one manifest mixing game and checkout concerns, Git submodules as the required model, in-place scaffold merging, force overwrite, automatic workspace updates, and dynamic provider plugins.
