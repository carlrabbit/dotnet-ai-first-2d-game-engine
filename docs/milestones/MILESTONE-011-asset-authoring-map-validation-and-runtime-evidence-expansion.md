# Milestone 011 — Asset Authoring, Map Validation, and Runtime Evidence Expansion

## Goal

Complete a broad, bounded capability-provider journey that turns asset evidence into reviewed authored content, validates that content in a map, and exposes the resulting runtime state through deterministic inspection artifacts.

Required journey:

```text
inspect smoke asset
→ derive deterministic perception proposals
→ aggregate review evidence
→ apply an authored review-decision fixture safely
→ validate updated asset metadata
→ validate and inspect one map using stable tile references
→ inspect one scenario/runtime state associated with that map
→ aggregate the resulting evidence into a review pack
```

The milestone must extend existing foundations rather than create unrelated asset, map, and runtime subsystems.

## Repository role and maturity assumptions

Repository role: `capability-provider`.

The repository implements reusable engine/runtime/tooling capabilities. Dogfood use of repository-owned smoke fixtures is permitted only to validate those capabilities.

Maturity assumptions:

- implementation-ready;
- artifact-first;
- headless-first and CLI/API-first;
- Milestones 001 through 010 are implemented;
- current product capabilities include content validation, asset inspection, review-pack generation, and a generated non-mutating asset curation workbench;
- existing review-state, diagnostic, exit-code, and deterministic-artifact rules remain authoritative.

## Execution mode

`ai-executed-broad`

Implementation must proceed in the ordered focus-area sequence below. A later focus area must not redefine a shared contract established by an earlier focus area.

## Scope

This is a large but bounded milestone with four ordered focus areas:

1. asset review decisions and safe metadata mutation;
2. deterministic asset perception and semantic proposals;
3. map metadata, cross-content validation, and map inspection;
4. runtime inspection and structured state evidence.

Each focus area must provide a product CLI surface, permanent project-truth contracts, deterministic artifacts, focused tests, a meaningful engineering smoke wrapper, and review-pack integration where applicable.

## Non-goals

Do not implement:

- an interactive editor or browser-side source mutation;
- automatic semantic approval;
- hosted AI, LLM, vision, or external model dependencies;
- network access or general image recognition;
- renderer, raylib, MonoGame, Blazor, GUI, map rendering, physics, collision resolution, pathfinding, or navigation;
- animation, shader, atlas packing, raw asset generation, save games, packaged-runtime optimization, source generators, or plugin architecture;
- public docs, release packaging, workflows, TBPs, issue templates, broad documentation synchronization, or guide-system migration.

## Shared foundations

All focus areas must reuse or consistently extend:

```text
stable IDs
repository-relative paths
source revision fingerprints
review-state vocabulary
provenance
structured diagnostics
passed / failed / error status
CLI exit codes 0 / 1 / 2 / 3
deterministic ordering
artifact references
source/generated separation
```

Do not create separate incompatible diagnostic, provenance, fingerprint, or review-state models. A source fingerprint must be deterministic and content-based; SHA-256 lowercase hexadecimal is preferred.

## Focus areas

### 1. Asset review decisions and safe metadata mutation

Introduce `docs/specs/asset-review-decision-contract.md`.

Required authored smoke source:

```text
game/assets/reviews/tile-atlas-smoke.review.json
```

Required command:

```text
agentic2d asset review apply --decisions <review-file> [--dry-run] --output <directory>
```

Required invocations:

```bash
dotnet run --project src/Agentic2D.Tools -- asset review apply --decisions game/assets/reviews/tile-atlas-smoke.review.json --dry-run --output artifacts/asset-review/dry-run

dotnet run --project src/Agentic2D.Tools -- asset review apply --decisions game/assets/reviews/tile-atlas-smoke.review.json --output artifacts/asset-review/applied
```

Required behavior:

- validate the authored decision file and target metadata;
- compare the expected source fingerprint before mutation;
- reject stale decisions without modifying source;
- make `--dry-run` complete but non-mutating;
- apply decisions deterministically and preserve unrelated metadata;
- update review evidence and approved semantic fields consistently;
- write evidence for dry-run, stale-input, validation-failure, and unexpected-error cases;
- validate the proposed/applied metadata;
- never modify raw assets.

The smoke decision fixture may contain synthetic human-review evidence solely for capability validation. It must be clearly marked as a repository fixture.

Required wrapper:

```bash
./eng/asset-review-smoke.sh
```

It must test dry-run, isolated real apply, stale-fingerprint rejection, and post-apply validation without leaving the working tree dirty.

### 2. Deterministic asset perception and semantic proposals

Introduce `docs/specs/asset-perception-contract.md`.

Required command:

```text
agentic2d asset perceive <asset-id-or-path> --output <directory>
```

Required invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- asset perceive asset.tile-atlas-smoke --output artifacts/assets/perception/tile-atlas-smoke
```

Required per-tile observations:

- tile ID and grid coordinate;
- transparency/alpha coverage;
- occupied pixel bounds when non-empty;
- deterministic representative color values;
- exact duplicate grouping by pixel content;
- stable feature fingerprint.

Semantic proposals are optional. Any proposal must remain `proposed`, include origin, score/confidence, and evidence references, and never become approved gameplay truth automatically.

Required wrapper:

```bash
./eng/asset-perception-smoke.sh
```

Review packs and the generated curation workbench must recognize perception artifacts without treating proposals as approvals.

### 3. Map metadata, cross-content validation, and map inspection

Introduce `docs/specs/map-content-contract.md`.

Required authored source and ID:

```text
game/maps/smoke/map-smoke.map.json
map.smoke
```

The map must reference `asset.tile-atlas-smoke` and stable tile IDs.

Extend content validation to support:

```text
maps
*.map.json
```

Required invocations:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate maps --output artifacts/content/maps

dotnet run --project src/Agentic2D.Tools -- content validate game/maps/smoke/map-smoke.map.json --output artifacts/content/map-smoke
```

Required map command:

```text
agentic2d map inspect <map-id-or-path> --output <directory>
```

Required invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- map inspect map.smoke --output artifacts/maps/map-smoke
```

Required validation includes schema, stable IDs, dimensions, deterministic layer ordering, cell bounds, asset/tile reference resolution, duplicate IDs, supported layer kinds, marker bounds, and prevention of silently inferred gameplay semantics.

Required wrapper:

```bash
./eng/map-smoke.sh
```

Review packs must recognize map validation and inspection artifacts.

### 4. Runtime inspection and structured state evidence

Introduce `docs/specs/runtime-inspection-contract.md`.

Required command:

```text
agentic2d runtime inspect --scenario <scenario-id-or-path> [--map <map-id-or-path>] --output <directory>
```

Required invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario runtime.smoke --map map.smoke --output artifacts/runtime/inspect
```

Required evidence exposes runtime/scenario identity, optional validated map identity, final tick, entities, supported state, commands and outcomes, ordered events, assertions, final state, diagnostics, and content references.

The command may project existing runtime/scenario execution. It must not introduce a full ECS, renderer, map simulation, or physics model.

Required wrapper:

```bash
./eng/runtime-inspect-smoke.sh
```

Review packs must recognize runtime inspection artifacts.

## End-to-end smoke journey

Add:

```bash
./eng/m011-smoke.sh
```

It must execute, in dependency order:

```text
asset inspection
asset perception
review pack/workbench refresh when needed
asset review dry-run and isolated apply
asset content validation
map content validation
map inspection
runtime inspection
final review-pack generation
artifact existence and key-schema checks
```

It must fail on any failed prerequisite and leave authored repository sources unchanged after completion.

## Implementation constraints

- Product behavior belongs behind `agentic2d`; `eng/` scripts remain engineering wrappers.
- Authored review decisions and maps are source content.
- Perception, inspection, mutation reports, and review packs are generated artifacts.
- Source mutation must be explicit and must never occur from `asset curate` or generated HTML.
- Dry-run must never mutate source.
- Stale fingerprints must fail before mutation.
- File replacement must be atomic where practical; failed apply must not leave partial metadata.
- Preserve semantically unrelated JSON fields. Stable formatting is required; byte-for-byte preservation is not.
- Deterministic lists must be explicitly sorted.
- Wall-clock values must not participate in semantic comparisons.
- Reuse existing validation and artifact logic; do not fork duplicate implementations.
- No network dependency.
- Existing commands and fixtures remain compatible unless authority docs are updated in the same change.
- Ordinary implementation agents must not read `.guide-profile.json`, `.guide-sync/`, copied research guides, prompt templates, or the external guide repository.

## Required authority documents

Read only:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/CONTENT.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/specs/product-cli-contract.md
docs/specs/minimal-deterministic-runtime.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/asset-pipeline.md
docs/specs/asset-metadata-contract.md
docs/specs/review-pack-contract.md
docs/specs/asset-curation-workbench-contract.md
docs/specs/asset-review-decision-contract.md
docs/specs/asset-perception-contract.md
docs/specs/map-content-contract.md
docs/specs/runtime-inspection-contract.md
docs/artifacts/content-validation-artifact-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
docs/artifacts/review-pack-artifact-contract.md
docs/artifacts/asset-curation-workbench-artifact-contract.md
docs/artifacts/asset-authoring-artifact-contract.md
docs/artifacts/map-inspection-artifact-contract.md
docs/artifacts/runtime-inspection-artifact-contract.md
docs/decisions/ADR-0014-one-bounded-smoke-journey-for-broad-expansion.md
docs/milestones/MILESTONE-011-asset-authoring-map-validation-and-runtime-evidence-expansion.md
```

Do not read external guide-system documents for implementation.

## Files or areas likely affected

Likely source areas:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Tools
```

A bounded shared namespace/project may be introduced if justified, but do not create a project per command or artifact type.

Likely authored additions:

```text
game/assets/reviews/tile-atlas-smoke.review.json
game/maps/smoke/map-smoke.map.json
```

Likely tests:

```text
tests/unit/Agentic2D.Tests.Unit
```

Required wrappers:

```text
eng/asset-review-smoke.sh
eng/asset-perception-smoke.sh
eng/map-smoke.sh
eng/runtime-inspect-smoke.sh
eng/m011-smoke.sh
```

Direct docs likely affected:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/CONTENT.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/specs/content-validation-contract.md
docs/specs/review-pack-contract.md
docs/specs/asset-curation-workbench-contract.md
docs/MILESTONES.md
docs/DECISIONS.md
```

Update only where current statements would become false or the capability would be undiscoverable.

## Validation tiers and concrete commands

Final validation:

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
./eng/asset-review-smoke.sh
./eng/asset-perception-smoke.sh
./eng/content-validate.sh maps
./eng/map-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/m011-smoke.sh
```

Required direct checks:

```bash
dotnet run --project src/Agentic2D.Tools -- asset perceive asset.tile-atlas-smoke --output artifacts/assets/perception/tile-atlas-smoke

dotnet run --project src/Agentic2D.Tools -- asset review apply --decisions game/assets/reviews/tile-atlas-smoke.review.json --dry-run --output artifacts/asset-review/dry-run

dotnet run --project src/Agentic2D.Tools -- content validate maps --output artifacts/content/maps

dotnet run --project src/Agentic2D.Tools -- map inspect map.smoke --output artifacts/maps/map-smoke

dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario runtime.smoke --map map.smoke --output artifacts/runtime/inspect

dotnet run --project src/Agentic2D.Tools -- review pack --input artifacts --output artifacts/review/m011
```

## Acceptance criteria

### Shared

1. Existing Milestone 010 commands and gates remain functional.
2. New commands follow current status and exit-code policy.
3. Artifacts use repository-relative or output-relative references.
4. Deterministic fields are stable across equivalent runs.
5. Fingerprints, diagnostics, provenance, review states, and artifact references are consistent.
6. No generated HTML/workbench command mutates source.
7. `./eng/m011-smoke.sh` completes the journey and leaves the working tree unchanged.

### Asset review and mutation

8. An authored `agentic2d.asset-review-decisions.v1` smoke file exists.
9. Dry-run writes complete evidence and does not mutate source.
10. Real apply updates an isolated metadata copy deterministically.
11. A stale fingerprint prevents mutation and produces a stable diagnostic.
12. Failed application does not partially modify source.
13. Unrelated metadata survives application.
14. Applied physical/gameplay approval has explicit synthetic fixture review evidence and provenance.
15. Post-apply asset validation passes.

### Asset perception

16. `asset perceive` produces required deterministic artifacts.
17. Per-tile fingerprints and exact duplicate groups are stable.
18. Perception proposals are never represented as approvals.
19. Proposal score, origin, and evidence references are present when proposals exist.
20. Review packs and workbench can include perception evidence.

### Maps

21. `map.smoke` exists as authored `agentic2d.map.v1` content.
22. `content validate maps` and direct `.map.json` validation work.
23. Missing asset/tile references produce stable diagnostics.
24. `map inspect` produces required deterministic artifacts.
25. No rendering, physics, or pathfinding is implemented.
26. Review packs include map evidence.

### Runtime inspection

27. `runtime inspect` works for `runtime.smoke` with optional `map.smoke`.
28. Artifacts expose commands, outcomes, events, final state, assertions, and content references.
29. Equivalent runs produce semantically equivalent artifacts.
30. Failure paths still write result and diagnostic evidence.
31. Review packs include runtime inspection evidence.

### Documentation and scope

32. Direct project-truth docs are updated where required.
33. ADR-0014 is indexed after acceptance.
34. Milestone 011 is indexed after implementation.
35. No external guides, copied prompts, TBPs, issue templates, workflows, release docs, public docs, or planning-package implementation files are added.

## Direct documentation impact

Update direct docs only to:

- document new product commands and wrappers;
- extend content validation to maps;
- index the new specs and artifact contracts;
- document map/runtime-inspection boundaries;
- extend review-pack known artifact families;
- extend workbench inputs with perception evidence;
- index ADR-0014 and Milestone 011;
- update solution/project descriptions if a new shared project is created.

Do not perform unrelated cleanup.

## Deferred documentation synchronization hints

The package adds:

```text
.guide-sync/pending/2026-07-03-m011-index-and-crosslink-sync.md
.guide-sync/pending/2026-07-03-m011-human-review-and-evidence-followup.md
```

The implementation agent must not read them.

## Human review requirements

After automated validation, a human must inspect:

1. whether the synthetic review fixture is unmistakably non-production;
2. whether dry-run, stale rejection, and mutation evidence are understandable;
3. whether perception proposals are visibly distinct from approvals;
4. whether map diagnostics identify map, layer, cell, asset, and tile;
5. whether runtime inspection permits diagnosis without source reading;
6. whether the final review pack presents the complete journey coherently;
7. whether implementation avoided four incompatible mini-frameworks.

Human review does not judge artistic quality, production gameplay semantics, or renderer output.

## Out-of-scope guide migration work

No guide migration is part of this milestone. Do not change `.guide-profile.json`, copy guide documents, add prompt templates, or require ordinary agents to read `.guide-sync/`.
