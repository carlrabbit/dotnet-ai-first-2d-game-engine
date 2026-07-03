# Runtime Inspection Contract

## Authority

Authoritative for deterministic structured runtime inspection. It extends the minimal runtime without defining a full ECS, renderer, physics engine, or packaged runtime.

## Purpose

Make executed state diagnosable without source-code reading or console-log scraping.

## Command

```text
agentic2d runtime inspect --scenario <scenario-id-or-path> [--map <map-id-or-path>] --output <directory>
```

Required invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario runtime.smoke --map map.smoke --output artifacts/runtime/inspect
```

## Behavior

Resolve/validate scenario and optional map, execute supported deterministic behavior, project commands/outcomes/events/assertions/final state/content references, and write evidence on pass or failure. Passing a map initially means validated content reference, not map simulation.

## Required projections

- scenario ID/path and optional map ID/path;
- final tick;
- stable entity IDs and supported state, initially logical position;
- submitted commands and accepted/rejected outcomes;
- ordered events;
- assertions and outcomes;
- final state;
- diagnostics and artifact/source references.

## Diagnostics

| ID | Meaning |
|---|---|
| `INSPECT0001` | Scenario resolution/validation failed. |
| `INSPECT0002` | Map resolution/validation failed. |
| `INSPECT0003` | Runtime execution failed. |
| `INSPECT0004` | Command projection inconsistent. |
| `INSPECT0005` | Event/final-state projection inconsistent. |
| `INSPECT0006` | Artifact writing failed. |

## Determinism

Equivalent inputs produce equivalent artifacts. Events follow runtime occurrence; other lists use deterministic ordering.
