# ADR-0006 — Establish the Engineering Substrate Before Engine Runtime Work

## Status

Accepted.

## Context

The project is an AI-first 2D game engine. Later work involves runtime semantics, scenario validation, asset workflows, generated artifacts, and human review gates.

Those later implementation tasks require a stable repository engineering API so agents can validate work through canonical commands rather than inferring build, test, and formatting behavior from project shape.

## Decision

The first implementation milestone established the base engineering substrate before engine runtime work began.

Milestone 001 created:

```text
shared .NET repository configuration
canonical eng/ scripts
minimal .NET solution
minimal contracts and engine projects
minimal unit test project
one smoke unit test
```

Milestone 001 did not implement:

```text
runtime tick loop
command/event/query model
scenario runner
asset pipeline
renderer
product CLI
source generators
benchmarks
packaging
public documentation
```

## Consequences

Implementation agents can use:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
./eng/check.sh
```

as the canonical validation surface after Milestone 001.

The first solution shape was intentionally small:

```text
Agentic2D.Contracts
Agentic2D.Engine
Agentic2D.Tests.Unit
```

Candidate future projects remain documented separately and are added only when a later milestone needs them.

## Alternatives considered

### Start with minimal deterministic runtime immediately

Rejected for Milestone 001. Runtime work would have forced agents to create build/test infrastructure and product semantics in the same task, increasing ambiguity and making validation less stable.

### Create the full candidate solution structure immediately

Rejected. Creating all projects up front would have produced empty scaffolding and implied architectural decisions that had not yet been validated.

### Create a product CLI stub immediately

Rejected for Milestone 001. The project is headless-first, but a product CLI without runtime semantics is likely to become dead scaffolding.

### Add GitHub Actions immediately

Deferred. CI should call `eng/` scripts, but the local engineering substrate needed to exist and pass first. Workflow creation can be a separate workflow/CI milestone.
