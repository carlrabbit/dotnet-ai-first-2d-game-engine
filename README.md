# dotnet-ai-first-2d-game-engine

This repository contains a .NET-based, AI-first 2D game engine.

> Humans design, review, and make creative decisions. AI agents implement, modify, validate, and iterate through structured commands, deterministic scenarios, semantic content, and generated evidence.

The engine is headless-first, CLI/API-first, validation-first, and artifact-first. A graphical debug client exists, but it is an adapter over read-only rendering projections rather than the foundation of the runtime.

## Current maturity

- Repository role: capability provider.
- Maturity: implementation-ready and artifact-first.
- Profiles: artifact-first agentic authoring, runtime/tool, and game/simulation.
- Implemented milestone range: M000 through M016, including the guide-system M009 update.
- Current capability surface:
  - deterministic runtime and product CLI;
  - authored scenarios and content validation;
  - asset metadata, inspection, perception, review decisions, review packs, and static curation workbench;
  - authored maps, runtime inspection, behavior modules, grid and continuous spatial modules;
  - runtime entities and typed components;
  - authored entity definitions, transactional instantiation, provenance, spatial queries, triggers, and explicit interactions;
  - consumer workspace manifests, deterministic source acquisition, and a unified workspace/project/run workflow.
  - source-only workspace creation supports directory reference, deterministic directory copy, and exact-revision Git acquisition.
  - backend-neutral read-only rendering projection;
  - deterministic backend-neutral semantic input mapping, tick frames, synthetic sources, and hardware-free replay;
  - isolated raylib-cs debug client with live and snapshot modes.
  - optional deterministic simulation foundation with one partitioned world, semantic time, activities, reservations, canonical persistence, and a headless wood-workflow proof;
  - optional standalone discrete-event simulation, coarse abstract travel/activity execution, authoritative one-region detailed fidelity, transactional reconciliation, and bounded multi-region evidence;
  - optional environmental infrastructure and settlement operations capability with construction plans, water/food/comfort infrastructure, maintenance, reserve policies, causal alerts, and bounded three-region evidence.
  - engine-owned retained UI and player application shell foundations with safe saves, settings, display preview, and software-defined input bindings.

M036 establishes the supported development baseline as native Linux/Bash and Windows/PowerShell 7. Linux export remains supported; Windows development does not imply Windows export.

## Current solution shape

```text
dotnet-ai-first-2d-game-engine.slnx
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.Simulation
src/Agentic2D.Entities
src/Agentic2D.Behaviors
src/Agentic2D.Spatial.Grid
src/Agentic2D.Spatial.Continuous
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Rendering
src/Agentic2D.Input
src/Agentic2D.Tools
src/Agentic2D.DebugClient.Raylib
tests/unit/Agentic2D.Tests.Unit
```

The exact project list is authoritative in the solution and `docs/engineering/future-dotnet-solution.md`.

## Start here

For implementation work, read:

1. `AGENTS.md`;
2. the relevant milestone under `docs/milestones/`;
3. only the authority documents listed by that milestone;
4. `docs/ENGINEERING.md` and `docs/engineering/command-contract.md`.

Do not treat `docs/research/` as operational authority. Do not require ordinary implementation agents to read `.guide-profile.json`, `.guide-sync/`, external guide internals, or prompt templates.

## Canonical engineering commands

```bash
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
./eng/check.sh

# Windows development equivalents
pwsh ./eng/restore.ps1
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

Current capability wrappers are indexed in `docs/ENGINEERING.md`.

## Product CLI

During development:

```bash
dotnet run --project src/Agentic2D.Tools -- <args>
```
## Consumer workspace workflow

Use the product CLI to create and operate a game workspace without repository engineering wrappers:

```bash
dotnet run --project src/Agentic2D.Tools -- workspace create /tmp/minimal-game --template minimal-game --engine-directory "$PWD" --engine-placement reference --output artifacts/workspaces/create
dotnet run --project src/Agentic2D.Tools -- project run /tmp/minimal-game --scenario scenario.minimal.smoke --output /tmp/minimal-game/artifacts/runs/smoke
```

`agentic2d.project.json` contains game truth; `agentic2d.workspace.json` contains engine acquisition, workspace composition, mutation policy, wrappers, and artifact location. The engine-provider area is read-only by default. The source-only providers are `directory-reference`, `directory-copy`, and `git-clone`; workspace updates/migrations and portable SDK acquisition are unsupported.


Important current command families include:

```text
runtime smoke
validate
scenario run
content validate
asset inspect
asset review apply
asset perceive
review pack
curation build
map inspect
runtime inspect
render project
```

The product CLI is authoritative through `docs/specs/product-cli-contract.md` and `docs/engineering/product-cli.md`. `eng/` scripts are repository validation wrappers, not the product API.

## Rendering

Headless rendering projection is available through:

```bash
dotnet run --project src/Agentic2D.Tools --   render project   --scenario interaction.npc-smoke   --tick final   --output artifacts/render/interaction-npc-smoke
```

The optional graphical adapter is isolated in `src/Agentic2D.DebugClient.Raylib`. It pins Raylib-cs 8.0.0, uses native raylib 6.0, loads the checked-in smoke PNG atlas, and supports live scenario and recorded-snapshot modes.

Screenshots are created only through explicit `F12` or `--capture`. Structural JSON render artifacts are semantic evidence; PNG output is human-review evidence.

## Documentation model

Repository docs contain project truth. External guides are inputs only for planning, migration, documentation synchronization, and release readiness.

There are no repository-local TBPs or issue-template dependencies by default.
