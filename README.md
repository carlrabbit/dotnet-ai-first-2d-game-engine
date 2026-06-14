# dotnet-ai-first-2d-game-engine

This repository contains a .NET-based, AI-first 2D game engine.

The project thesis:

> Humans design, review, and make creative decisions. AI agents implement, modify, validate, and iterate through structured commands, deterministic scenarios, semantic content, and generated evidence.

The engine is intended to be headless-first, CLI/API-first, validation-first, and artifact-first. A graphical editor may exist later, but it is not the foundation of the repository.

## Current maturity

- Repository maturity: implementation-ready for the base engineering substrate, minimal deterministic runtime, and first product CLI surface.
- Target profile: artifact-first / agentic authoring + runtime/tool + game/simulation.
- Current product role: capability provider. The repository builds the engine/runtime/tooling capability; it is not yet a full consumer game project.

## Current repository shape

The current solution contains:

```text
dotnet-ai-first-2d-game-engine.slnx
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.ScenarioRunner
src/Agentic2D.Tools
tests/unit/Agentic2D.Tests.Unit
```

`Agentic2D.Tools` hosts the first development product CLI surface.

## Start here

For implementation work, read:

1. `AGENTS.md`
2. the relevant milestone under `docs/milestones/`
3. the authority documents listed by that milestone
4. `docs/ENGINEERING.md` and `docs/engineering/command-contract.md` for validation commands

Do not treat `docs/research/` as operational authority. Research copies are retained only for traceability.

## Engineering commands

Use repository engineering commands instead of inventing local validation flows:

```bash
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
./eng/check.sh
```

Product CLI validation wrappers also exist for the current CLI surface:

```bash
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
```

## Product CLI

During development, invoke the product CLI through:

```bash
dotnet run --project src/Agentic2D.Tools -- <args>
```

Current supported product CLI commands are documented in `docs/engineering/product-cli.md` and `docs/specs/product-cli-contract.md`.

Examples:

```bash
dotnet run --project src/Agentic2D.Tools -- --help
dotnet run --project src/Agentic2D.Tools -- runtime smoke --output artifacts/cli/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- validate --output artifacts/cli/validate
dotnet run --project src/Agentic2D.Tools -- scenario run game/scenarios/smoke/runtime-smoke.json --output artifacts/scenarios/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- content validate assets --output artifacts/content/assets
dotnet run --project src/Agentic2D.Tools -- asset inspect asset.tile-atlas-smoke --output artifacts/assets/tile-atlas-smoke
```

Artifact-producing CLI commands write structured evidence under the requested output directory.

## Documentation model

Repository docs contain project truth. External setup, engineering, or guide-system documents are not required reading for ordinary implementation agents.

There are no repository-local TBPs or issue-template dependencies by default.
