# Agent Instructions

## Default implementation path

Read:

- `README.md`;
- `docs/ENGINEERING.md`;
- `docs/engineering/command-contract.md`;
- the relevant milestone or task under `docs/milestones/`;
- only the authority documents explicitly listed there.

Use canonical `eng/` commands. Do not invent parallel validation flows when repository commands exist.

Do not read `.guide-profile.json` or `.guide-sync/` for ordinary implementation work. They are metadata for planning, migration, documentation synchronization, and release-readiness tasks.

## Task routing

### Implementation

Read the milestone focus areas and listed authority. Use focused validation first, then the required milestone gate. Do not perform broad documentation synchronization unless explicitly assigned.

Ignore:

- `.guide-profile.json`;
- `.guide-sync/`;
- external guide internals;
- prompt templates;
- copied material under `docs/research/`.

### Runtime, entities, behaviors, spatial systems, and interactions

Use the current contracts indexed in `docs/SPECS.md`.

Maintain these boundaries:

- runtime owns entity identity, lifecycle, components, snapshots, commands, and events;
- behavior code reads immutable snapshots and emits intents;
- spatial modules interpret spatial components but do not own entities;
- maps remain authored static content;
- interactive mutable objects are explicit runtime entities;
- interactions end at the currently implemented event boundary unless a later milestone extends them.

### Rendering

Read:

- `docs/specs/visual-definition-contract.md`;
- `docs/specs/render-projection-contract.md`;
- `docs/specs/raylib-debug-client-contract.md`;
- `docs/artifacts/render-projection-artifact-contract.md`.

Rendering is read-only. It must not mutate or advance runtime state.

Only `src/Agentic2D.DebugClient.Raylib` may reference raylib-cs. `Agentic2D.Tools`, runtime projects, authored content contracts, and backend-neutral rendering contracts must remain independent of native graphics dependencies.

Use headless projection validation everywhere. Run the raylib graphics smoke only in a documented graphics-capable environment.

### Scenario, content, and artifacts

Read:

- `docs/SCENARIOS.md`;
- `docs/CONTENT.md`;
- `docs/ARTIFACTS.md`;
- relevant domain and artifact contracts.

Generated outputs belong under `artifacts/` or `game/assets/generated/` and are not authored source unless explicitly declared as committed baselines.

### Planning and guide migration

Planning and migration tasks may read `.guide-profile.json`, `.guide-sync/`, and external guide-system material. Do not copy guide documents or prompt templates into this repository.

### Documentation synchronization

Read broadly across active project docs and `.guide-sync/pending/`. Resolve or narrow each hint. Delete a hint only when its completion criteria are satisfied.

### Human review

Read `docs/HUMAN-REVIEW.md`, applicable contracts, and generated review packs. Human review is required only where project truth explicitly requires it.

## Authority rule

`docs/research/` is non-authoritative traceability material.

Project-specific operational rules must exist in active repository docs before implementation agents rely on them.

## Hard constraints

- Do not add non-root `README.md` files.
- Do not add TBPs or issue templates by default.
- Do not copy setup guides, engineering guides, guide-system docs, or prompt templates.
- Do not add public docs, release structures, package publishing, or workflows unless explicitly activated.
- Do not treat `eng/` scripts as product APIs.
- Do not introduce implementation placeholders that produce success without validating meaningful state.

## Constrained validation

For resumable suites, run `--plan-json`, then each required `--shard <id>` in a separate invocation, then `--verify`. Only the verifier establishes aggregate success. Do not use backgrounding, detached processes, timeout inflation, or partial logs as proof.

Use `.review/` only through the canonical review commands. Required and blocking review must pass `./eng/review-check.sh`.

The canonical launcher platform is Bash on Linux. Native Windows and PowerShell are unsupported until implemented and tested.

`src/Agentic2D.Engineering` is repository engineering infrastructure, not a product runtime API.
