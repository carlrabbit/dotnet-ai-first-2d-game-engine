# ADR-0008 — Product CLI Is the Agent-Facing Product API

## Status

Proposed for Milestone 003.

## Context

The project is headless-first and agentic. Agents need a stable command surface for invoking engine behavior and collecting structured evidence.

Milestone 001 established repository engineering commands under `eng/`.

Milestone 002 is expected to establish a minimal deterministic runtime and a first runtime execution path.

Without a clear boundary, future agents may treat `eng/` scripts as the product API or may add ad-hoc commands that do not produce stable artifacts.

## Decision

Milestone 003 introduces the `agentic2d` product CLI as the agent-facing product/runtime API.

`eng/` scripts remain repository engineering wrappers.

The first product CLI host is:

```text
src/Agentic2D.Tools
```

The first supported product commands are:

```text
agentic2d --help
agentic2d --version
agentic2d runtime smoke --output <directory>
agentic2d validate --output <directory>
```

Artifact-producing product commands must write:

```text
<output>/result.json
```

## Consequences

Agents can call product behavior through a documented product CLI rather than through internal test fixtures or ad-hoc `dotnet run` commands.

Repository validation can wrap product behavior through:

```text
./eng/cli-smoke.sh
./eng/product-validate.sh
```

Product behavior remains documented independently from engineering wrapper behavior.

The CLI becomes the natural expansion point for future scenario, asset, map, content, shader, and package commands, but those commands are not introduced in this milestone.

## Alternatives considered

### Continue using only unit tests

Rejected. Unit tests are necessary but do not provide the product command surface agents need for runtime operation and artifact collection.

### Make `eng/` scripts the product API

Rejected. `eng/` scripts are repository engineering commands. They are useful wrappers, but the product/runtime API should be independent of repository automation.

### Add a full scenario runner now

Rejected for Milestone 003. A full scenario runner should be a later milestone. This milestone only formalizes product CLI invocation around the minimal runtime.

### Package as a .NET tool now

Deferred. Tool packaging belongs to a later release/package maturity stage. Development invocation through `dotnet run --project src/Agentic2D.Tools -- <args>` is sufficient for this milestone.

## Follow-up

Future milestones may add:

```text
agentic2d scenario run <scenario-id>
agentic2d asset inspect <path>
agentic2d map preview <map-id>
agentic2d content validate <scope>
agentic2d package build
```

Each future command should have a documented command contract, artifact contract, validation command, and scope boundary before implementation.
