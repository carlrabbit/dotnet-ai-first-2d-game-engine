# Project Initialization Instructions

## Purpose

This document is the actionable handoff for initializing `dotnet-ai-first-2d-game-engine` as an AI-agent-friendly repository.

It intentionally stops before creating:

- executable `eng/` scripts;
- `.slnx` / `.sln` files;
- `.csproj` files;
- NuGet package references;
- CI workflows with real build commands.

Those are separate follow-up milestones. This package creates the target documentation and folder skeleton so the next agent can create the repository substrate without rediscovering the project model.

## Inputs used

The initialization is based on:

- `docs/research/project-setup-guide-v7.md`
- `docs/research/engineering-guide-v6.md`
- `docs/research/agentic-2d-game-engine-project-summary.md`

The research copies are non-authoritative. Active repository authority lives in the root docs and their indexed subdocuments.

## Project classification

Use the following repository classification:

```text
Name: dotnet-ai-first-2d-game-engine
Maturity: design-ready initialization skeleton
Primary profiles:
  - artifact-first / agentic authoring
  - runtime/tool
  - game/simulation
Secondary future profiles:
  - public package/tool
  - samples
  - release-ready package/runtime
Default stack direction:
  - .NET 10 or newer
  - C# primary behavior language
  - optional F# for rule-heavy modules later
  - MTP + TUnit for tests when projects are created
  - BenchmarkDotNet when performance work starts
  - Bun + Biome only if TypeScript tooling becomes necessary
Runtime evaluation direction:
  - raylib-cs spike first if rapid workbench/runtime proof is needed
  - MonoGame prototype as the serious .NET 2D runtime candidate
  - SDL3/Silk.NET only if lower-level control becomes necessary
```

## Non-goals for this initialization package

Do not implement engine code.

Do not choose the final renderer, physics library, shader pipeline, asset schema language, or component storage implementation.

Do not create placeholder C# projects with empty classes just to make the repository look complete.

Do not create local README files under subfolders.

Do not copy the complete external guide model into active docs. Extract only project-specific rules.

## Target repository structure

The initialized repository should have this shape:

```text
/
├─ .config/
├─ .github/
│  └─ workflows/
├─ artifacts/
├─ docs/
│  ├─ INITIALIZATION.md
│  ├─ TERMINOLOGY.md
│  ├─ SPECS.md
│  ├─ ARCHITECTURE.md
│  ├─ DECISIONS.md
│  ├─ ENGINEERING.md
│  ├─ MILESTONES.md
│  ├─ SCENARIOS.md
│  ├─ CONTENT.md
│  ├─ ARTIFACTS.md
│  ├─ HUMAN-REVIEW.md
│  ├─ RESEARCH.md
│  ├─ specs/
│  ├─ architecture/
│  ├─ decisions/
│  ├─ engineering/
│  ├─ milestones/
│  ├─ scenarios/
│  ├─ content/
│  ├─ artifacts/
│  ├─ review/
│  └─ research/
├─ eng/
├─ game/
│  ├─ design/
│  ├─ content/
│  ├─ assets/
│  ├─ behavior/
│  ├─ behavior-fsharp/
│  ├─ scenarios/
│  └─ artifacts/
├─ src/
├─ tests/
├─ .editorconfig        # future setup milestone
├─ Directory.Build.props # future setup milestone
├─ Directory.Packages.props # future setup milestone
├─ global.json          # future setup milestone
├─ AGENTS.md
└─ README.md
```

This package includes the folders and documentation files. It does not include the future .NET build files listed as future setup milestone outputs.

## Initialization phases for the next agent

### Phase 0 — Verify skeleton

Confirm that the following files exist:

```text
README.md
AGENTS.md
.gitignore
docs/INITIALIZATION.md
docs/ENGINEERING.md
docs/SPECS.md
docs/SCENARIOS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/research/project-setup-guide-v7.md
docs/research/engineering-guide-v6.md
docs/research/agentic-2d-game-engine-project-summary.md
```

Confirm that no non-root `README.md` file exists.

### Phase 1 — Create base engineering substrate

Only after a task explicitly requests it, create:

```text
.config/dotnet-tools.json
.editorconfig
Directory.Build.props
Directory.Packages.props
global.json
eng/common.sh
eng/restore.sh
eng/build.sh
eng/test.sh
eng/format.sh
eng/check.sh
```

Expected behavior:

- `eng/restore.sh` restores the solution after it exists.
- `eng/build.sh` builds the solution after it exists.
- `eng/test.sh` runs fast tests only after test projects exist.
- `eng/format.sh --verify` checks formatting after `.editorconfig` and .NET tooling exist.
- `eng/check.sh` is the standard local gate.

Until a solution exists, scripts may either fail clearly with `Solution not initialized yet` or be deferred entirely. Do not create misleading scripts that pass without validating anything.

### Phase 2 — Create initial .NET solution

Only after a task explicitly requests it, create an initial .NET solution using this intended project layout:

```text
src/Agentic2D.Contracts/
src/Agentic2D.Engine/
src/Agentic2D.Runtime/
src/Agentic2D.Tools/
src/Agentic2D.Validation/
src/Agentic2D.ScenarioRunner/
src/Agentic2D.AssetPipeline/
src/Agentic2D.SourceGen/
tests/unit/Agentic2D.Tests.Unit/
tests/integration/Agentic2D.Tests.Integration/
```

Do not create all projects if the milestone narrows the first vertical slice. Prefer the smallest useful solution that supports the milestone.

Recommended first project set:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
tests/unit/Agentic2D.Tests.Unit
```

Add `Agentic2D.Tools` only when the first CLI command is implemented.

Add `Agentic2D.AssetPipeline` only when asset inspection starts.

Add `Agentic2D.ScenarioRunner` only when deterministic scenarios start.

### Phase 3 — Define first product CLI contract

The project must be headless-first. Before implementing commands, document the intended product CLI in `docs/engineering/product-cli.md`.

Initial command candidates:

```text
agentic2d validate
agentic2d scenario run <scenario-id>
agentic2d asset inspect <path>
agentic2d map preview <map-id>
agentic2d content validate <scope>
```

For each command, define:

- purpose;
- inputs;
- deterministic behavior;
- output path;
- `result.json` shape;
- diagnostics behavior;
- exit codes;
- whether artifacts are required.

Do not implement commands without a command contract.

### Phase 4 — Establish scenario and artifact contracts

Before implementing scenario execution, update:

```text
docs/SCENARIOS.md
docs/scenarios/scenario-contract.md
docs/ARTIFACTS.md
docs/artifacts/report-contract.md
```

Minimum scenario fields:

```text
id
category
purpose
initial state
inputs
random seed policy
expected events/assertions
expected artifacts
debug-mode applicability
packaged-mode applicability
human review requirements
```

Minimum artifact fields:

```text
runId
command
scenarioId when applicable
status
diagnostics
artifacts
startedAt
completedAt
engineVersion when available
```

### Phase 5 — Establish content and asset authority

Before creating asset metadata or game content schemas, update:

```text
docs/CONTENT.md
docs/content/stable-ids.md
docs/content/asset-metadata.md
docs/content/provenance.md
docs/content/taxonomy-and-rules.md
```

Rules:

- visual labels are not physical behavior;
- AI-proposed semantic metadata is reviewable proposal data until approved;
- collision, walkability, interactability, progression blockers, and navigation-critical metadata require human review unless a project rule explicitly auto-approves them;
- generated assets and previews must be separated from hand-authored source.

### Phase 6 — Runtime evaluation

Runtime evaluation should be milestone-driven. Do not import runtime libraries during repository initialization.

Recommended order:

1. raylib-cs spike for fast proof of the agentic loop or asset curation workbench.
2. MonoGame prototype for the serious runtime candidate.
3. SDL3/Silk.NET spike only if maximum platform-layer control is required.

Each runtime spike must produce:

- a short decision note;
- a runnable smoke command;
- a validation result;
- known limitations;
- impact on asset, scenario, shader, and packaged-runtime strategy.

## Selected building blocks

The active initialization skeleton prepares for these Engineering Guide building blocks:

```text
BB00 Repository Base
BB01 .NET Solution          # deferred
BB02 Shared Build Configuration # deferred
BB03 EditorConfig and Style # deferred
BB04 MTP + TUnit Unit Tests # deferred
BB05 Test Guardrails        # deferred
BB08 Agent Instructions
BB13 Documentation Skeleton
BB21 Headless Product CLI   # prepared, not implemented
BB22 Structured Project Data
BB23 Scenario-Based Validation
BB24 Artifact Evidence and Reports
BB25 Generated/Derived Artifact Pipeline
BB26 Human Review Gates
BB27 Dual-Mode Runtime Validation
```

Do not apply these yet unless requested:

```text
BB09 Bun + Biome
BB10 Blazor Module
BB11 Playwright E2E Module
BB14 NuGet Packaging
BB15 Samples
BB18 GitHub Pages Website
BB19 Public Documentation
BB20 Release Readiness
```

## Completion checklist for this package

The package is complete when:

- the root folder is named `dotnet-ai-first-2d-game-engine`;
- the root contains `README.md`, `AGENTS.md`, `.gitignore`, `docs/`, `eng/`, `src/`, `tests/`, `game/`, and `artifacts/`;
- active docs exist for engineering, specs, scenarios, content, artifacts, and human review;
- research copies of the attached guides and project summary exist under `docs/research/`;
- no non-root `README.md` files exist;
- no executable `eng/` scripts exist yet;
- no `.csproj`, `.slnx`, or `.sln` files exist yet.
