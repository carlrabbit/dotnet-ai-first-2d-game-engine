# Milestone 010 — Asset Review Pack and Curation Workbench Foundation

## Goal

Introduce the first bounded human-review surface for existing engine evidence by adding:

- a review pack capability that aggregates current scenario, content-validation, and asset-inspection artifacts into a machine-readable manifest and human-readable summary;
- a minimal generated asset curation workbench artifact for the existing tile-atlas smoke asset;
- explicit review-state handling for proposed asset semantics without adding image recognition, interactive editing, or renderer integration.

The milestone must make asset evidence easier for humans and agents to inspect while preserving the repository's artifact-first, headless-first model.

## Repository role and maturity assumptions

Repository role: capability provider.

The repository provides engine/runtime/tooling capabilities. It is not yet a consumer game project.

Maturity assumptions:

- implementation-ready;
- artifact-first;
- guide-system metadata is at version `0.3.0`;
- Milestones 001 through 009 are implemented;
- current product CLI capabilities include runtime smoke, scenario run, content validate, and asset inspect;
- authored asset metadata and asset inspection exist for `asset.tile-atlas-smoke`.

This milestone validates capability implementation. It may dogfood the repository's own smoke scenario and smoke asset, but the dogfood scope is limited to proving the review-pack and curation-workbench capabilities.

## Execution mode

`ai-executed-human-reviewed`

The milestone is suitable for AI execution because the current product CLI, artifact contracts, content validation, and asset metadata boundaries are explicit. Human review is required for evidence usefulness and semantic review boundaries, not for approving arbitrary generated asset semantics.

## Scope

This milestone is a medium-to-large coherent vertical slice.

In scope:

- review pack product CLI command;
- review pack engineering smoke wrapper;
- review pack artifact contract implementation;
- generated static asset curation workbench artifact for the existing smoke tile atlas;
- asset curation workbench engineering smoke wrapper;
- workbench artifact contract implementation;
- review status model for proposed/approved/rejected/needs-revision semantics;
- tests covering deterministic artifact generation and failure evidence;
- direct documentation updates required to make implemented commands and contracts discoverable.

The implementation may touch multiple product components when needed to keep the vertical slice coherent.

## Non-goals

Do not implement:

- AI image recognition;
- automatic visual tagging from pixels;
- interactive editor state mutation;
- browser-hosted application architecture;
- Blazor, raylib, MonoGame, renderer, or GUI integration;
- map editing or map preview;
- animation editing or shader preview;
- atlas packing or raw asset generation beyond any existing smoke fixture;
- save/load systems;
- packaged runtime validation;
- public docs, release docs, package publishing, workflow files, TBPs, or issue templates;
- broad documentation synchronization outside the direct documentation impact section;
- external guide-system migration.

## Focus areas

### 1. Review pack product capability

Add a product CLI command:

```text
agentic2d review pack --input <artifact-root> --output <directory>
```

Required development invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- review pack --input artifacts --output artifacts/review/latest
```

The command must aggregate only artifact families currently supported by active repository contracts:

- scenario runner artifacts;
- content validation artifacts;
- asset inspection artifacts.

The command must not perform arbitrary recursive interpretation of unknown file types. Unknown artifact folders may be ignored with an info or warning diagnostic, but known malformed artifact groups must produce stable diagnostics.

Required output files:

```text
<output>/review-summary.md
<output>/review-manifest.json
<output>/diagnostics.json
```

Optional output file:

```text
<output>/artifact-index.json
```

### 2. Review pack artifact contract

Implement the contract in:

```text
docs/artifacts/review-pack-artifact-contract.md
```

The generated review pack must include enough evidence for a human or agent to answer:

- which artifact groups were included;
- which commands or capabilities produced them;
- which source item IDs are represented;
- which diagnostics need attention;
- which assets require semantic review;
- which source and generated artifact paths are relevant.

Review pack artifacts must use repository-relative or output-relative references. Avoid absolute paths.

### 3. Minimal asset curation workbench artifact

Add a product CLI command:

```text
agentic2d asset curate --asset <asset-id-or-path> --review-pack <review-pack-path> --output <directory>
```

Required development invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- asset curate --asset asset.tile-atlas-smoke --review-pack artifacts/review/latest --output artifacts/workbench/asset-curation
```

The workbench is a generated artifact, not a persistent editor application.

Required output files:

```text
<output>/index.html
<output>/review-data.json
<output>/diagnostics.json
```

Optional output file:

```text
<output>/review-board.md
```

The generated HTML may be minimal and static. It must not require a server, network access, package install, browser automation, or JavaScript build pipeline.

### 4. Asset curation workbench contract

Implement the contract in:

```text
docs/specs/asset-curation-workbench-contract.md
docs/artifacts/asset-curation-workbench-artifact-contract.md
```

The workbench must present the existing smoke asset's structural evidence and semantic review state. It may display data from:

- asset metadata;
- content validation artifacts;
- asset inspection artifacts;
- review pack manifest;
- tile summaries.

The workbench must keep visual proposals and gameplay/physical approvals distinct.

### 5. Review-state model

Support this bounded review state vocabulary for generated review data:

```text
proposed
approved
rejected
needs-revision
not-required
```

Initial behavior:

- proposed visual labels may appear as `proposed`;
- approved physical/gameplay behavior must only appear as `approved` when source metadata contains human review evidence;
- absent physical/gameplay approvals should appear as `not-required` or equivalent non-approved state;
- the tool must not auto-promote proposed visual labels to approved gameplay semantics.

### 6. Engineering wrappers

Add meaningful engineering wrappers:

```bash
./eng/review-pack-smoke.sh
./eng/asset-curation-smoke.sh
```

Expected behavior:

```text
./eng/review-pack-smoke.sh
  runs the existing smoke artifact generators needed for the pack when necessary, runs `agentic2d review pack`, and verifies review pack artifacts exist.

./eng/asset-curation-smoke.sh
  runs or reuses the smoke review pack, runs `agentic2d asset curate`, and verifies workbench artifacts exist.
```

The wrappers must fail clearly when prerequisites fail. Do not create success-only placeholder scripts.

### 7. Tests

Add focused tests for:

- review pack generation from current smoke artifacts;
- deterministic review manifest semantic fields;
- missing or malformed known artifact group diagnostics;
- review summary contains useful high-level sections;
- asset curation workbench generation for `asset.tile-atlas-smoke`;
- review data preserves visual-label proposal state;
- review data does not approve physical/gameplay semantics without review evidence;
- engineering wrappers fail when the product command fails.

## Implementation constraints

- Product behavior belongs behind `agentic2d`, not only `eng/` wrappers.
- `eng/` wrappers are validation conveniences and must not become the product API.
- All generated review/workbench artifacts must be deterministic for semantic fields.
- Avoid absolute paths in artifacts unless explicitly marked volatile and excluded from tests.
- Do not introduce external web, UI, renderer, browser automation, or Node/Bun/npm dependencies.
- Do not require network access.
- Do not require image recognition or pixel semantic interpretation.
- Do not mutate source asset metadata as part of curation.
- If HTML is generated, keep it static and self-contained enough for manual opening from disk.
- Use existing content validation and asset inspection where appropriate; do not duplicate divergent validation rules.
- Do not require ordinary implementation agents to read `.guide-profile.json`, `.guide-sync/`, copied guide material, or the external guide repository.

## Required authority documents

The implementation agent must read these documents after unpacking the package:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/specs/product-cli-contract.md
docs/specs/content-validation-contract.md
docs/specs/asset-metadata-contract.md
docs/specs/asset-pipeline.md
docs/specs/review-pack-contract.md
docs/specs/asset-curation-workbench-contract.md
docs/artifacts/content-validation-artifact-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
docs/artifacts/review-pack-artifact-contract.md
docs/artifacts/asset-curation-workbench-artifact-contract.md
docs/decisions/ADR-0013-review-pack-before-interactive-curation-editor.md
docs/milestones/MILESTONE-010-asset-review-pack-and-curation-workbench-foundation.md
```

Do not read the external guide repository for implementation. The guide system was used during planning only.

## Files or areas likely affected

Likely implementation areas:

```text
src/Agentic2D.Tools
src/Agentic2D.Validation
src/Agentic2D.ScenarioRunner
src/Agentic2D.Contracts
```

Optional new project or namespace if justified:

```text
src/Agentic2D.Review
```

Authored smoke content likely reused:

```text
game/scenarios/smoke/runtime-smoke.json
game/assets/metadata/tile-atlas-smoke.asset.json
game/assets/raw/samples/tile-atlas-smoke.png
```

Tests likely affected:

```text
tests/unit/Agentic2D.Tests.Unit
```

Engineering scripts likely added:

```text
eng/review-pack-smoke.sh
eng/asset-curation-smoke.sh
```

Direct documentation likely updated:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/SPECS.md
docs/ARTIFACTS.md
docs/DECISIONS.md
docs/MILESTONES.md
```

## Validation tiers and concrete repository commands

Run focused validation while implementing, then run the full local gate.

Required final validation:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/asset-inspect-smoke.sh
./eng/review-pack-smoke.sh
./eng/asset-curation-smoke.sh
```

Direct product CLI smoke commands that must work by completion:

```bash
dotnet run --project src/Agentic2D.Tools -- review pack --input artifacts --output artifacts/review/latest
dotnet run --project src/Agentic2D.Tools -- asset curate --asset asset.tile-atlas-smoke --review-pack artifacts/review/latest --output artifacts/workbench/asset-curation
```

If one of the pre-existing wrappers is absent in the checked-out repository, do not create a placeholder to satisfy this milestone. Report the missing prerequisite and run the available documented gates.

## Acceptance criteria

The milestone is complete when all of these are true:

1. `agentic2d review pack --input <artifact-root> --output <directory>` exists and is documented.
2. `agentic2d review pack` produces `review-summary.md`, `review-manifest.json`, and `diagnostics.json`.
3. `review-manifest.json` follows `docs/artifacts/review-pack-artifact-contract.md`.
4. `review-summary.md` is useful to a human reviewer without reading implementation source code.
5. The review pack includes scenario, content validation, and asset inspection evidence when those artifacts are present.
6. Known malformed or missing artifact groups produce stable diagnostics.
7. `agentic2d asset curate --asset <asset-id-or-path> --review-pack <review-pack-path> --output <directory>` exists and is documented.
8. `agentic2d asset curate` produces `index.html`, `review-data.json`, and `diagnostics.json` for `asset.tile-atlas-smoke`.
9. `review-data.json` follows `docs/artifacts/asset-curation-workbench-artifact-contract.md`.
10. The generated workbench keeps proposed visual labels separate from approved physical/gameplay behaviors.
11. No physical/gameplay behavior is marked approved without human review evidence in source metadata.
12. `./eng/review-pack-smoke.sh` exists and validates meaningful state.
13. `./eng/asset-curation-smoke.sh` exists and validates meaningful state.
14. All required final validation commands pass.
15. Direct documentation impact is applied without broad unrelated documentation cleanup.
16. No external guide documents, prompt templates, TBPs, issue templates, workflow files, public docs, release docs, or implementation-source files are added by the planning package.

## Direct documentation impact

Update direct docs only as needed to make implemented behavior discoverable and authoritative:

- add Milestone 010 to `docs/MILESTONES.md` after implementation;
- add `docs/specs/review-pack-contract.md` and `docs/specs/asset-curation-workbench-contract.md` to `docs/SPECS.md`;
- add `docs/artifacts/review-pack-artifact-contract.md` and `docs/artifacts/asset-curation-workbench-artifact-contract.md` to `docs/ARTIFACTS.md`;
- add ADR-0013 to `docs/DECISIONS.md`;
- update `docs/ENGINEERING.md`, `docs/engineering/command-contract.md`, and `docs/engineering/product-cli.md` for new commands and wrappers after implementation;
- update `docs/CONTENT.md` only if the review-state vocabulary becomes durable project-truth content behavior rather than only workbench artifact behavior.

Do not perform broad guide migration or documentation synchronization as part of this implementation milestone.

## Deferred documentation synchronization hints

This package adds deferred synchronization hints under:

```text
.guide-sync/pending/
```

The implementation agent is not required to read `.guide-sync/` for this milestone.

Deferred hints cover:

- post-implementation index synchronization for Milestone 010;
- post-implementation human review of review-pack and workbench evidence usefulness.

## Human review requirements

Human review is required after implementation to assess:

- whether `review-summary.md` is understandable and useful;
- whether `review-manifest.json` gives agents enough structured evidence;
- whether `index.html` or the generated workbench artifact is useful enough for a first curation surface;
- whether visual proposals and approved physical/gameplay behavior are clearly separated;
- whether any generated text could mislead a human into thinking proposed semantics are already approved.

Human review is not required to decide whether validation commands pass.

The milestone must not ask humans to approve gameplay semantics unless source metadata explicitly contains review evidence or the review state records that approval remains pending.

## Out-of-scope guide migration work

No guide-system migration is part of this milestone.

Do not modify:

```text
.guide-profile.json
```

Do not copy external guide documents or prompt templates into the repository.

Do not make target repository docs reference external guide documents as operational implementation authority.
