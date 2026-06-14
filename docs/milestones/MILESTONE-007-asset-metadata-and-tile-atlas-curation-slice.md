# Milestone 007 — Asset Metadata and Tile Atlas Curation Slice

## Goal

Introduce the first asset-pipeline vertical slice by making a raw 2D image asset addressable, inspectable, content-validated, and represented by authored asset metadata.

The milestone must prove this flow:

```text
raw PNG asset
→ authored asset metadata JSON
→ content validation for asset metadata
→ product CLI asset inspection
→ structural tile atlas report artifacts
→ human review of semantic metadata boundaries
```

The milestone is not a graphical workbench. It is the smallest durable asset-curation substrate needed before building asset preview, map authoring, animation, or semantic tagging workflows.

## Repository role and maturity assumptions

Repository role:

```text
capability-provider
```

This repository builds engine/runtime/tooling capability. Milestone 007 validates that the engine can represent and inspect asset metadata. It does not build a consumer game and does not curate a production asset library.

Current maturity assumption:

```text
implementation-ready for base engineering, minimal runtime, product CLI, scenario runner, and content validation
artifact-first
```

The milestone assumes Milestones 001–006 are implemented:

- canonical `eng/` scripts exist;
- `src/Agentic2D.Contracts`, `src/Agentic2D.Engine`, `src/Agentic2D.Tools`, `src/Agentic2D.ScenarioRunner`, and `src/Agentic2D.Validation` exist where prior milestones created them;
- `agentic2d content validate` supports scenario JSON;
- `./eng/check.sh`, `./eng/cli-smoke.sh`, `./eng/product-validate.sh`, `./eng/scenario-smoke.sh`, and `./eng/content-validate.sh scenarios` exist and validate meaningful state.

## Execution mode

```text
ai-executed-human-reviewed
```

Scope size:

```text
medium coherent vertical slice
```

Implementation autonomy:

The implementation agent may modify multiple components when the changes stay inside this milestone's asset metadata, content validation, CLI, artifact, and review boundaries.

Expected touched areas include `Agentic2D.Validation`, `Agentic2D.Tools`, optional asset-pipeline code, unit tests, sample authored asset content, sample raw asset fixture, direct command-contract documentation, and directly affected project docs.

The implementation agent must not expand the milestone into a graphical editor, broad asset library management, map editing, animation editing, shader/material preview, image recognition, AI tagging, packaged runtime, public docs, release work, or guide-system migration.

## Scope

Implement a first asset metadata and tile atlas curation slice.

Required authored source content:

```text
game/assets/raw/samples/tile-atlas-smoke.png
game/assets/metadata/tile-atlas-smoke.asset.json
```

The implementation may choose a different deterministic sample filename only when all direct docs, tests, and validation commands use the same path. The sample PNG may be tiny and visually trivial. It exists to validate structural asset handling, not artistic quality.

Required product capability:

```text
agentic2d asset inspect <asset-id-or-path> --output <directory>
```

Required development invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- asset inspect asset.tile-atlas-smoke --output artifacts/assets/tile-atlas-smoke
```

Required content validation expansion:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate assets --output artifacts/content/assets
```

Required engineering validation wrappers:

```bash
./eng/content-validate.sh assets
./eng/asset-inspect-smoke.sh
```

If `./eng/content-validate.sh` already accepts a generic scope parameter, extend it to support `assets`. If its implementation is hardcoded to `scenarios`, make the smallest meaningful change that preserves the existing `scenarios` behavior and adds `assets`.

## Non-goals

Do not implement any of the following in this milestone:

```text
graphical asset curation workbench
image recognition or AI visual tagging
semantic auto-approval
map editor or map validation
animation editor or animation validation
shader/material preview
sprite rendering runtime
raylib-cs integration
MonoGame integration
image transformation or asset generation
atlas packing
dependency graph beyond one metadata-to-raw-asset reference
asset import from external folders
recursive asset discovery beyond the documented sample scope
full JSON Schema registry
packaged-runtime validation
release readiness
public documentation
NuGet packaging
GitHub Actions workflows
TBPs
issue templates
copied guide documents
copied prompt templates
guide-system migration
```

## Focus areas

### A. Asset metadata contract and authored sample

Goal:

Create the first authored asset metadata file and, if no suitable raw image exists, a deterministic small PNG fixture for structural validation.

Scope:

- add one raw PNG fixture under `game/assets/raw/samples/`;
- add one authored metadata JSON file under `game/assets/metadata/`;
- use stable asset ID `asset.tile-atlas-smoke` unless implementation evidence proves a different ID is safer;
- model only structural and review-bound semantic metadata required by `docs/specs/asset-metadata-contract.md`.

Required metadata concepts:

```text
schema
id
kind
source.path
source.mediaType
tileAtlas.tileSize
tileAtlas.columns
tileAtlas.rows
tiles[]
provenance
semantics.visualLabelsProposed
semantics.physicalBehaviorsApproved
humanReview
```

Validation tier:

```text
Tier 1 focused implementation
```

Direct documentation impact:

Update only directly affected active docs if the implemented file names, ID, or contract details differ from this milestone or `docs/specs/asset-metadata-contract.md`.

Deferred documentation impact:

Index updates belong to `.guide-sync/pending/` unless required for implementation safety.

### B. Content validation expansion for asset metadata

Goal:

Extend the content validation foundation from scenarios to asset metadata.

Scope:

- support `agentic2d content validate assets --output <directory>`;
- support validating a single repository-relative `.asset.json` file;
- validate the authored asset metadata contract;
- detect stable ID violations, missing fields, duplicate tile IDs, invalid source references, invalid tile grid declarations, and invalid human review declarations;
- preserve existing scenario validation behavior.

Required diagnostics:

Use stable diagnostic IDs. Existing `CONTENT####` IDs may be reused where their meaning fits. Add asset-specific IDs only when needed, for example:

```text
ASSET0001 missing asset metadata field
ASSET0002 invalid asset source reference
ASSET0003 invalid tile grid
ASSET0004 duplicate tile ID
ASSET0005 semantic approval violation
ASSET0006 invalid provenance
```

The exact IDs may vary if they are stable, documented, and tested.

Validation tier:

```text
Tier 1 focused implementation
Tier 2 content validation gate when called by ./eng/content-validate.sh assets
```

Direct documentation impact:

Update `docs/specs/content-validation-contract.md`, `docs/CONTENT.md`, `docs/engineering/product-cli.md`, and `docs/engineering/command-contract.md` only where needed to document `assets` as a supported content validation scope.

Deferred documentation impact:

Broad index normalization belongs to `.guide-sync/pending/`.

### C. Product CLI asset inspection

Goal:

Expose the first asset inspection command through the product CLI.

Scope:

Implement:

```text
agentic2d asset inspect <asset-id-or-path> --output <directory>
```

Supported target forms:

1. asset ID `asset.tile-atlas-smoke`, resolved to `game/assets/metadata/tile-atlas-smoke.asset.json`;
2. repository-relative path to a supported `.asset.json` file.

The command must:

- load and validate the asset metadata;
- inspect the referenced raw PNG structurally;
- verify at minimum that the PNG path exists and its structural image size is compatible with the declared tile grid;
- produce asset inspection artifacts;
- return repository product CLI exit codes consistently.

Required development invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- asset inspect asset.tile-atlas-smoke --output artifacts/assets/tile-atlas-smoke
```

Exit code policy:

| Exit code | Meaning |
|---:|---|
| `0` | Asset inspection completed and passed. |
| `1` | Asset inspection completed but validation or consistency checks failed. |
| `2` | Invalid CLI usage, unsupported target form, or malformed option. |
| `3` | Unexpected IO, parsing, artifact writing, or command failure. |

Validation tier:

```text
Tier 1 focused implementation
Tier 2 asset smoke gate when called by ./eng/asset-inspect-smoke.sh
```

Direct documentation impact:

Update `docs/specs/product-cli-contract.md` and `docs/engineering/product-cli.md` only where needed to document the new product command after implementation.

Deferred documentation impact:

Index and broad cross-link updates belong to `.guide-sync/pending/`.

### D. Asset inspection artifact contract

Goal:

Produce machine-readable evidence for asset metadata and tile atlas inspection.

Scope:

A successful asset inspection must write:

```text
<output>/result.json
<output>/diagnostics.json
<output>/asset-summary.json
<output>/tiles.json
```

The artifact shape is governed by:

```text
docs/artifacts/asset-inspection-artifact-contract.md
```

The artifacts must contain enough evidence to answer:

```text
Which asset was inspected?
Which raw file was referenced?
What image dimensions were observed?
What tile grid was declared?
How many tiles were declared?
Which tiles have proposed visual labels?
Which physical/gameplay semantics are approved, proposed, or absent?
Were any review-gated fields present?
Which diagnostics explain failures?
```

Validation tier:

```text
Tier 1 focused implementation
Tier 2 asset smoke gate
```

Direct documentation impact:

Update only direct artifact docs needed to document actual fields and any deviation from the contract.

Deferred documentation impact:

Broader artifact index updates belong to `.guide-sync/pending/`.

### E. Human review boundary for semantic asset metadata

Goal:

Preserve the repository distinction between structural metadata, AI-proposed visual labels, and human-approved gameplay-relevant semantics.

Scope:

The implementation must enforce these rules:

- structural metadata may be automatically validated;
- visual labels may be represented as proposals;
- physical/gameplay semantics such as walkability, collision, navigation cost, damage, progression, or interactability must not be represented as approved unless the metadata explicitly records human review approval;
- the smoke asset should avoid approved high-impact semantics unless the metadata records a human-review placeholder consistent with the contract;
- automated validation must fail or warn when high-impact semantics are approved without review evidence, according to the asset metadata contract.

Human review for milestone acceptance should evaluate whether the metadata and artifacts make the semantic boundary clear. It is not required to judge artistic quality of the sample asset.

Validation tier:

```text
Tier 2 product/content validation gate
human review of semantic boundary and generated evidence quality
```

Direct documentation impact:

Update direct asset metadata docs if the implemented review fields differ.

Deferred documentation impact:

Any broader human-review documentation belongs to `.guide-sync/pending/`.

## Implementation constraints

- Use repository-local project docs as authority. Do not require the implementation agent to read external guide-system docs.
- Do not copy guide documents or prompt templates into this repository.
- Do not add TBPs or issue templates.
- Do not add non-root README files.
- Keep raw asset creation minimal and deterministic. The sample raw PNG is only a validation fixture.
- Prefer standard library PNG header inspection before adding image-processing dependencies. If an external package is necessary, document the reason in the implementation summary and keep the dependency narrow.
- Do not introduce renderer dependencies.
- Do not infer semantic labels from pixels. Any visual labels in the sample metadata are authored proposals.
- Do not mark physical/gameplay semantics as approved without explicit review evidence in the metadata.
- Preserve existing `scenarios` content validation behavior.
- All generated outputs must be written under the caller-provided output directory or `artifacts/` through engineering wrappers.

## Required authority documents

The implementation agent must read only these repository-local authority documents unless a focus area explicitly needs a directly referenced file:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/specs/asset-pipeline.md
docs/specs/asset-metadata-contract.md
docs/specs/content-validation-contract.md
docs/specs/product-cli-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
docs/artifacts/content-validation-artifact-contract.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/decisions/ADR-0012-asset-metadata-before-visual-workbench.md
```

The implementation agent must not read `.guide-profile.json`, `.guide-sync/`, external guide repository internals, copied guide material under `docs/research/`, prompt templates, TBPs, or issue templates for this ordinary implementation milestone.

## Files or areas likely affected

Likely source/content areas:

```text
src/Agentic2D.Contracts
src/Agentic2D.Validation
src/Agentic2D.Tools
src/Agentic2D.AssetPipeline          optional, only if justified by implementation shape
tests/unit/Agentic2D.Tests.Unit
game/assets/raw/samples/tile-atlas-smoke.png
game/assets/metadata/tile-atlas-smoke.asset.json
eng/content-validate.sh
eng/asset-inspect-smoke.sh
```

Likely direct documentation areas:

```text
docs/specs/content-validation-contract.md
docs/specs/product-cli-contract.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
```

Direct docs should be updated only when needed to document the supported commands, scopes, artifacts, and project layout actually introduced by this milestone.

## Validation tiers and concrete commands

Focused validation during implementation:

```bash
./eng/build.sh
./eng/test.sh
```

Required local gate:

```bash
./eng/check.sh
```

Required existing product and scenario gates:

```bash
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
```

Required new content validation gate:

```bash
./eng/content-validate.sh assets
```

Required new asset inspection gate:

```bash
./eng/asset-inspect-smoke.sh
```

Required direct product command checks:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate assets --output artifacts/content/assets
dotnet run --project src/Agentic2D.Tools -- asset inspect asset.tile-atlas-smoke --output artifacts/assets/tile-atlas-smoke
```

Release, packaging, public documentation, packaged-runtime, visual regression, and workflow validation are not required.

## Acceptance criteria

Milestone 007 is complete when all criteria are met:

- one authored asset metadata file exists for `asset.tile-atlas-smoke` or an explicitly documented equivalent stable ID;
- one deterministic raw PNG fixture exists and is referenced by the metadata;
- content validation supports `assets` scope;
- content validation supports direct validation of the sample `.asset.json` file;
- existing `scenarios` validation still passes;
- `agentic2d asset inspect <asset-id-or-path> --output <directory>` is implemented;
- asset inspection supports the sample asset ID and a repository-relative metadata path;
- asset inspection writes `result.json`, `diagnostics.json`, `asset-summary.json`, and `tiles.json`;
- artifacts conform to `docs/artifacts/asset-inspection-artifact-contract.md` or direct docs explain any intentional deviation;
- structural checks verify that declared tile grid metadata is compatible with the raw PNG fixture;
- diagnostics are structured and stable enough for tests;
- tests cover passing metadata, missing raw source, invalid tile grid, duplicate tile ID, invalid semantic approval, CLI usage errors, and deterministic artifact semantics;
- `./eng/check.sh` passes;
- `./eng/cli-smoke.sh` passes;
- `./eng/product-validate.sh` passes;
- `./eng/scenario-smoke.sh` passes;
- `./eng/content-validate.sh scenarios` passes;
- `./eng/content-validate.sh assets` passes;
- `./eng/asset-inspect-smoke.sh` passes;
- human review confirms the structural/visual/physical semantic boundary is visible in metadata and artifacts;
- no renderer, image recognition, workbench UI, atlas packing, package/release, TBP, issue-template, workflow, or copied-guide scope is introduced.

## Direct documentation impact

The implementation may directly update only docs needed for implementation safety and truthfulness:

```text
docs/specs/content-validation-contract.md
docs/specs/product-cli-contract.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
```

Direct updates should state only project truth introduced by the implementation.

Do not perform broad index normalization or unrelated cleanup as part of the implementation unless the validation commands or implementation safety require it.

## Deferred documentation synchronization hints

The planning package includes `.guide-sync/pending/` hints for later documentation synchronization.

The implementation agent must not read `.guide-sync/` for ordinary implementation. A later documentation-sync pass should consume those hints after implementation and human review.

Expected deferred sync topics:

- index Milestone 007 and ADR-0012;
- index new asset metadata and asset inspection artifact docs;
- reconcile `docs/CONTENT.md`, `docs/ARTIFACTS.md`, `docs/SPECS.md`, and `docs/ENGINEERING.md` after implementation;
- record any human-review findings about semantic metadata boundaries.

## Human review requirements

Human review is required for milestone acceptance, but not to decide whether automated validation passes.

Human review must evaluate:

- whether asset metadata separates structural, visual, physical, gameplay, and review fields clearly;
- whether AI-proposed visual labels are visibly proposals rather than approved source truth;
- whether physical/gameplay semantics require explicit review evidence;
- whether inspection artifacts are useful enough for future agents and humans;
- whether the sample asset fixture is sufficient without drifting into art production or graphical workbench scope.

Human review must not require:

- judging visual quality of the sample PNG;
- finalizing visual taxonomy;
- approving gameplay semantics for a real game;
- validating map design, animation, shader, or renderer behavior.

## Out-of-scope guide migration work

This is not a guide migration milestone.

Do not modify:

```text
.guide-profile.json
external guide references
prompt templates
TBPs
issue templates
docs/research guide copies
```

If guide-system or broad documentation synchronization issues are noticed, record them only through `.guide-sync/pending/` in a separate planning or documentation-sync task.

## Implementation summary requirements

The implementation summary must include:

- asset metadata source path and stable asset ID;
- raw PNG fixture path and structural dimensions;
- product CLI commands added or changed;
- engineering wrappers added or changed;
- artifact files produced by the asset inspection smoke command;
- validation commands run and exact results;
- tests added for valid and invalid asset metadata;
- any package dependency added and why;
- direct docs updated;
- semantic boundary/human-review behavior;
- deviations from this milestone, if any;
- deferred documentation sync notes, if any.
