# ADR-0007 — Expose the Minimal Runtime Through `Agentic2D.Tools`

## Status

Proposed for Milestone 002.

## Context

The repository now has the base engineering substrate established by Milestone 001: canonical `eng/` commands, shared .NET configuration, and a minimal solution with contracts, engine, and unit test projects.

The project thesis requires a headless-first, CLI/API-first, validation-first, and artifact-first engine. The next implementation slice should therefore prove real engine behavior through a product-facing command, not only through unit tests.

The existing candidate project layout identifies `Agentic2D.Tools` as the product CLI seam and keeps broader seams such as scenario runner, runtime host, source generator, asset pipeline, and packaged runtime deferred until milestones justify them.

## Decision

Milestone 002 introduces the minimal deterministic runtime and exposes it through a new `Agentic2D.Tools` console project.

The first product command is:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

This command runs a deterministic runtime smoke scenario and writes:

```text
artifacts/runtime-smoke/result.json
```

Milestone 002 does not create a separate `Agentic2D.ScenarioRunner` project, renderer, runtime host project, asset pipeline project, source generator project, or packaged runtime project.

## Consequences

The project gets a meaningful product CLI earlier than a full scenario system.

Agents can validate the first runtime behavior by executing a documented command and inspecting a structured artifact.

The CLI is constrained to one command group and one scenario-like smoke behavior, avoiding a broad command surface before semantics exist.

`Agentic2D.Tools` becomes a current project rather than a candidate future project after Milestone 002 is implemented.

## Alternatives considered

### Implement runtime only through unit tests

Rejected. Unit tests are necessary but insufficient for an agentic engine whose core workflow depends on headless product commands and generated artifacts.

### Create `Agentic2D.ScenarioRunner` immediately

Rejected for Milestone 002. The milestone needs one built-in runtime smoke scenario, not the full future scenario subsystem.

### Add `eng/cli-smoke.sh` immediately

Deferred by default. A permanent engineering wrapper may be useful later, but the first product CLI command can be validated through `dotnet run` and unit tests. If the implementation adds a wrapper, it must perform meaningful validation and update the engineering command contract directly.

### Start with asset pipeline or renderer work

Rejected. Asset and renderer work depend on a minimal runtime/result/command foundation but are not necessary to prove the first deterministic runtime behavior.

### Use a CLI framework package

Rejected by default. The first command surface is small enough for a hand-written parser. External CLI packages may be reconsidered when command complexity warrants them.
