# Agent Instructions

## Default implementation path

Read:

- `README.md`
- `docs/ENGINEERING.md`
- `docs/engineering/command-contract.md`
- the relevant milestone or task document under `docs/milestones/`
- the authority documents explicitly listed by that milestone or task

Use canonical `eng/` commands. Do not invent build, test, format, product-validation, or artifact-validation flows when repository commands exist.

Do not read `.guide-profile.json` or `.guide-sync/` for ordinary implementation work. Those paths are guide-system metadata for planning, migration, documentation synchronization, and release-readiness tasks only.

## Task-mode routing

### Implementation

Read the relevant milestone focus area, directly relevant specs, and engineering command contracts. Do not perform broad documentation synchronization unless explicitly requested.

Default validation is the validation tier named by the milestone. For ordinary implementation, prefer focused validation first and then the required local gate.

Ignore:

- `.guide-profile.json`
- `.guide-sync/`
- external guide repository internals
- prompt templates
- copied guide material under `docs/research/`

unless the task is explicitly a guide migration, documentation synchronization, planning, or release-readiness task.

### Product CLI/runtime work

Read:

- `docs/specs/runtime-principles.md`
- `docs/specs/minimal-deterministic-runtime.md` when changing the minimal runtime
- `docs/specs/product-cli-contract.md` when changing the product CLI
- `docs/engineering/product-cli.md`
- `docs/artifacts/runtime-result-contract.md` or `docs/artifacts/product-cli-result-contract.md` when changing result artifacts

Use:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
```

when the milestone requires product CLI validation.

### Scenario/content/artifact validation

Read:

- `docs/SCENARIOS.md`
- `docs/CONTENT.md`
- `docs/ARTIFACTS.md`
- relevant documents under `docs/scenarios/`, `docs/content/`, and `docs/artifacts/`

Only use scenario/content/artifact validation commands that exist and validate meaningful state.

### Planning and guide migration

Planning and guide migration tasks may use the external guide repository as planning input.

They may read:

- `.guide-profile.json`
- `.guide-sync/pending/`
- relevant migration milestone documents
- external guide-system material identified by the planning task

They must not copy guide documents or prompt templates into this repository.

### Documentation synchronization

Read broadly across relevant project docs.

Also read `.guide-sync/pending/` when the task is explicitly a documentation synchronization pass. Resolve or narrow each pending sync hint, and delete a hint only when its completion criteria are satisfied.

Do not add TBPs, issue templates, public docs, release docs, or workflow docs unless the synchronization task explicitly activates those layers.

### Human review

Read:

- `docs/HUMAN-REVIEW.md`
- relevant scenario/content/artifact documents
- generated review packs when they exist

Human review is required only where a milestone, spec, or review document explicitly requires it.

## Authority rule

Do not treat `docs/research/project-setup-guide-v7.md`, `docs/research/engineering-guide-v6.md`, `docs/research/agentic-2d-game-engine-project-summary.md`, or other copied guide material as operational authority.

Research documents are retained for traceability. Project-specific rules must exist in active repository docs before implementation agents rely on them.

External guide-system documents are planning, migration, documentation-sync, and release-readiness inputs only. Ordinary implementation agents use this repository's localized project docs.

`.guide-profile.json` is guide-selection metadata, not project authority.

`.guide-sync/` is deferred documentation synchronization metadata, not project authority and not implementation scope.

## Current hard constraints

- Do not add non-root `README.md` files.
- Do not add TBPs or issue templates by default.
- Do not copy setup guides, engineering guides, guide-system docs, or prompt templates into this repository.
- Do not add public documentation, release-readiness structure, samples, package publishing, or workflows unless a milestone explicitly activates them.
- Generated outputs belong under `artifacts/` or `game/assets/generated/` and must not be treated as hand-authored source.
- Do not treat `eng/` scripts as the product API. `eng/` scripts are repository engineering wrappers; `agentic2d` is the product/runtime CLI.
