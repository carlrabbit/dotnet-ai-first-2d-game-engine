# ADR-0006 — Establish the Engineering Substrate Before Engine Runtime Work

## Status

Proposed for Milestone 001.

## Context

The project is an AI-first 2D game engine. Later work will involve runtime semantics, scenario validation, asset workflows, generated artifacts, and human review gates.

Those later implementation tasks require a stable repository engineering API so agents can validate work through canonical commands rather than inferring build, test, and formatting behavior from project shape.

The current repository maturity is design-ready for the engine concept but not yet implementation-ready for ordinary .NET development.

## Decision

The first implementation milestone establishes the base engineering substrate before engine runtime work begins.

Milestone 001 creates:

```text
shared .NET repository configuration
canonical eng/ scripts
minimal .NET solution
minimal contracts and engine projects
minimal unit test project
one smoke unit test
```

Milestone 001 does not implement:

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

as the canonical validation surface after Milestone 001 is complete.

The engine runtime remains design-ready until the next product milestone.

The repository avoids premature dependencies on raylib-cs, MonoGame, SDL/Silk.NET, asset pipeline libraries, or source generator projects.

The first solution shape is intentionally small:

```text
Agentic2D.Contracts
Agentic2D.Engine
Agentic2D.Tests.Unit
```

Candidate future projects remain documented separately and are added only when a later milestone needs them.

## Alternatives considered

### Start with minimal deterministic runtime immediately

Rejected for Milestone 001. Runtime work would force agents to create build/test infrastructure and product semantics in the same task, increasing ambiguity and making validation less stable.

### Create the full candidate solution structure immediately

Rejected. The project summary contains a broad candidate project structure, but creating all projects up front would produce empty scaffolding and imply architectural decisions that have not yet been validated.

### Create a product CLI stub immediately

Rejected for Milestone 001. The project is headless-first, but a product CLI without runtime semantics is likely to become dead scaffolding. The CLI should be introduced when it can execute a meaningful validation or scenario command.

### Add GitHub Actions immediately

Deferred. CI should call `eng/` scripts, but the local engineering substrate should exist and pass first. Workflow creation can be a separate workflow/CI milestone.

## Follow-up

A later milestone should create the minimal deterministic runtime and may then introduce a product CLI around real engine behavior.
