# ADR-0007 — Expose the Minimal Runtime Through `Agentic2D.Tools`

## Status

Accepted.

## Context

The repository has the base engineering substrate established by Milestone 001: canonical `eng/` commands, shared .NET configuration, and a minimal solution with contracts, engine, and unit test projects.

The project thesis requires a headless-first, CLI/API-first, validation-first, and artifact-first engine. The next implementation slice therefore needed to prove real engine behavior through a product-facing command, not only through unit tests.

The candidate project layout identified `Agentic2D.Tools` as the product CLI seam and kept broader seams such as scenario runner, runtime host, source generator, asset pipeline, and packaged runtime deferred until milestones justify them.

## Decision

Milestone 002 introduced the minimal deterministic runtime and exposed it through `Agentic2D.Tools`.

The runtime smoke execution path is:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

This command runs a deterministic runtime smoke scenario and writes:

```text
artifacts/runtime-smoke/result.json
```

Milestone 002 did not create a separate `Agentic2D.ScenarioRunner` project, renderer, runtime host project, asset pipeline project, source generator project, or packaged runtime project.

## Consequences

The project gets a meaningful runtime smoke path earlier than a full scenario system.

Agents can validate the first runtime behavior by executing a documented command and inspecting a structured artifact.

The CLI exposure is constrained to one command group and one scenario-like smoke behavior, avoiding a broad command surface before semantics exist.

`Agentic2D.Tools` is now a current project rather than a candidate future project.

## Alternatives considered

### Implement runtime only through unit tests

Rejected. Unit tests are necessary but insufficient for an agentic engine whose core workflow depends on headless product commands and generated artifacts.

### Create `Agentic2D.ScenarioRunner` immediately

Rejected for Milestone 002. The milestone needed one built-in runtime smoke scenario, not the full future scenario subsystem.

### Add `eng/cli-smoke.sh` immediately

Deferred from Milestone 002 and later introduced by Milestone 003.

### Start with asset pipeline or renderer work

Rejected. Asset and renderer work depend on a minimal runtime/result/command foundation but are not necessary to prove the first deterministic runtime behavior.

### Use a CLI framework package

Rejected by default. The first command surface is small enough for a hand-written parser. External CLI packages may be reconsidered when command complexity warrants them.
