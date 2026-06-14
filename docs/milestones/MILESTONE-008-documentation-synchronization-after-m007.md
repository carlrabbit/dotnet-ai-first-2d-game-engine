# Milestone 008 — Documentation Synchronization After Milestone 007

## Goal

Synchronize repository documentation after the guide-system migration and Milestones 005, 006, and 007.

This is a documentation synchronization milestone. It must update project-truth indexes, cross-links, current-status summaries, and pending guide-sync hints so active documentation accurately reflects the implemented capabilities:

```text
Milestone 004: external guide-system metadata and routing
Milestone 005: scenario runner and runtime evidence foundation
Milestone 006: content schema validation foundation
Milestone 007: asset metadata and tile atlas curation slice
```

The synchronization must keep target-repository documentation focused on project truth. It must not copy external guide documents or make external guide-system documents operational authority for ordinary implementation work.

## Repository role and maturity assumptions

Repository role:

```text
capability-provider
```

The repository provides engine, runtime, CLI, validation, scenario, content, and asset-curation capabilities. It does not yet act as a consumer game repository.

Current maturity assumptions:

```text
implementation-ready
artifact-first
documentation-sync-needed
```

The milestone assumes that these implemented capabilities exist in active docs and code:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Tools
```

It also assumes these validation commands are implemented and meaningful:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/asset-inspect-smoke.sh
```

## Execution mode

```text
documentation-sync
```

This mode may read `.guide-profile.json` and `.guide-sync/pending/`.

This mode must not make ordinary implementation agents read `.guide-profile.json`, `.guide-sync/`, copied guide documents, external guide repository internals, or prompt templates.

## Scope

Synchronize active documentation and consume or narrow existing pending guide-sync hints.

The synchronization pass must inspect at least:

```text
README.md
AGENTS.md
.guide-profile.json
.guide-sync/pending/
docs/TERMINOLOGY.md
docs/SPECS.md
docs/CONTENT.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/RESEARCH.md
docs/specs/
docs/scenarios/
docs/artifacts/
docs/engineering/
docs/decisions/
docs/milestones/
```

The pass must resolve direct index and cross-link drift caused by:

```text
Milestone 004 guide-system migration
Milestone 005 scenario runner foundation
Milestone 006 content validation foundation
Milestone 007 asset metadata and tile atlas curation slice
```

## Non-goals

Do not perform any of the following:

```text
source code changes
test code changes
build script changes
workflow YAML changes
generated artifact changes
new runtime behavior
new product CLI behavior
new validation behavior
new asset metadata behavior
public documentation activation
release readiness
package publishing
TBP creation
issue-template creation
copied guide document creation
copied prompt-template creation
broad prose rewrite unrelated to synchronization
```

Do not introduce non-root `README.md` files.

Do not delete `docs/research/` copied guide material solely because it is legacy traceability material. It may remain if active docs clearly mark it non-authoritative.

## Focus areas

### Focus area A — Resolve milestone and decision index drift

Update repository indexes so implemented and planned milestones and accepted decisions are discoverable.

Review and update:

```text
docs/MILESTONES.md
docs/DECISIONS.md
```

Expected synchronization:

```text
Milestone 004 — guide-system migration
Milestone 005 — scenario runner and runtime evidence foundation
Milestone 006 — content schema validation foundation
Milestone 007 — asset metadata and tile atlas curation slice
Milestone 008 — documentation synchronization after Milestone 007
```

Decision index should include accepted decision records that exist in `docs/decisions/`, including at minimum:

```text
ADR-0009 — adopt external guide system v0.2.0
ADR-0010 — scenario runner before asset workbench
ADR-0011 — content validation before asset curation
ADR-0012 — asset metadata before visual workbench
```

If any listed milestone or decision file does not exist, do not invent its contents. Record the inconsistency in the implementation summary and narrow any affected pending sync hint instead of silently linking a missing file.

Validation tier:

```text
Tier 1 documentation consistency
```

Direct documentation impact:

```text
docs/MILESTONES.md
docs/DECISIONS.md
```

Deferred documentation impact:

Delete or narrow these hints when their completion criteria are satisfied:

```text
.guide-sync/pending/2026-06-13-guide-system-index-sync.md
.guide-sync/pending/2026-06-14-m006-content-validation-index-sync.md
.guide-sync/pending/2026-06-14-m007-asset-metadata-index-sync.md
```

### Focus area B — Synchronize spec, content, scenario, and artifact indexes

Ensure active authority indexes reflect implemented behavior and point to current project-truth contracts.

Review and update:

```text
docs/SPECS.md
docs/CONTENT.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
```

Expected spec index additions if the files exist:

```text
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/asset-metadata-contract.md
```

Expected artifact index additions if the files exist:

```text
docs/artifacts/scenario-runner-artifact-contract.md
docs/artifacts/content-validation-artifact-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
```

`docs/CONTENT.md` should remain concise but should reflect authored scenario JSON validation, authored asset metadata JSON validation, asset metadata source/generated separation, and visual-label proposal versus approved physical/gameplay metadata.

`docs/SCENARIOS.md` should reflect Milestone 005 scenario runner capability without claiming packaged-mode scenario validation exists.

`docs/ARTIFACTS.md` should reflect current scenario, content-validation, and asset-inspection artifacts without treating generated outputs as source truth.

Validation tier:

```text
Tier 1 documentation consistency
```

Direct documentation impact:

```text
docs/SPECS.md
docs/CONTENT.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
```

### Focus area C — Synchronize engineering and product command documentation

Ensure command documentation reflects implemented validation wrappers and product CLI capabilities without documenting future commands as supported.

Review and update:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
README.md
AGENTS.md
```

Expected implemented engineering wrappers:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/asset-inspect-smoke.sh
```

Expected implemented product CLI commands:

```text
agentic2d --help
agentic2d --version
agentic2d runtime smoke --output <directory>
agentic2d validate --output <directory>
agentic2d scenario run <scenario-id-or-path> --output <directory>
agentic2d content validate <scope-or-path> --output <directory>
agentic2d asset inspect <asset-id-or-path> --output <directory>
```

Do not document these as supported unless implemented:

```text
agentic2d map preview <map-id>
agentic2d package build
./eng/scenario-packaged.sh <scenario-id>
./eng/artifacts-validate.sh <artifact-path>
./eng/review-pack.sh <run-id-or-artifact-path>
```

`AGENTS.md` should keep the ordinary-agent routing rule: ordinary implementation agents ignore `.guide-profile.json`, `.guide-sync/`, external guide internals, prompt templates, and copied guide material.

Validation tier:

```text
Tier 1 documentation consistency
Tier 2 command contract verification
```

Direct documentation impact:

```text
README.md
AGENTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
```

### Focus area D — Resolve guide-sync pending hints

Consume, delete, or narrow pending `.guide-sync/pending/` files according to their completion criteria.

Read all current files under:

```text
.guide-sync/pending/
```

At minimum, inspect and resolve or narrow these known pending hints if present:

```text
.guide-sync/pending/2026-06-13-guide-system-index-sync.md
.guide-sync/pending/2026-06-13-legacy-guide-leakage-review.md
.guide-sync/pending/2026-06-14-m005-evidence-review-followup.md
.guide-sync/pending/2026-06-14-m006-content-validation-index-sync.md
.guide-sync/pending/2026-06-14-m006-validation-evidence-review.md
.guide-sync/pending/2026-06-14-m007-asset-metadata-index-sync.md
.guide-sync/pending/2026-06-14-m007-asset-semantic-review-followup.md
```

For each hint:

1. read its purpose and completion criteria;
2. decide whether the sync pass satisfies it;
3. delete it only if the completion criteria are met;
4. otherwise replace it with a narrower remaining-work hint.

Do not move permanent project truth into `.guide-sync/`. Durable project truth belongs in active docs under `docs/`.

Validation tier:

```text
Tier 1 documentation consistency
```

Direct documentation impact:

```text
.guide-sync/pending/
```

### Focus area E — Review legacy guide leakage and ordinary-agent routing

Verify active docs do not make copied guide documents, external guide docs, prompt templates, `.guide-profile.json`, or `.guide-sync/` ordinary implementation authority.

Review at least:

```text
README.md
AGENTS.md
docs/RESEARCH.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/milestones/*.md
docs/decisions/*.md
```

The sync pass must preserve these rules:

```text
docs/research/ is non-authoritative traceability material
external guides are planning/migration/documentation-sync/release-readiness inputs only
ordinary implementation agents use localized project docs and milestone-listed authority
.guide-profile.json is guide-selection metadata only
.guide-sync/ is deferred documentation synchronization metadata only
no copied prompt templates in this repository
no default TBPs or issue-template dependencies
```

Validation tier:

```text
Tier 1 documentation consistency
```

Direct documentation impact:

```text
README.md
AGENTS.md
docs/RESEARCH.md
```

only if they are stale or ambiguous.

### Focus area F — Human-review evidence follow-up

Review whether scenario, content-validation, and asset-inspection evidence docs need narrowing based on implementation outcomes.

Review existing contracts and any available implementation summaries for:

```text
scenario failure diagnostics
scenario event-log readability
content validation diagnostic usefulness
content validation item summaries
asset metadata source/proposal/approval boundary
asset inspection summaries
tile metadata readability
future human review pack criteria
```

Do not add public docs, release docs, UX docs, or visual workbench docs.

If evidence contracts are already sufficient, delete the relevant pending hints. If not, move durable rules into active project docs or narrow the pending hint.

Validation tier:

```text
Tier 1 documentation consistency
```

Direct documentation impact may include:

```text
docs/specs/scenario-runner-contract.md
docs/artifacts/scenario-runner-artifact-contract.md
docs/specs/content-validation-contract.md
docs/artifacts/content-validation-artifact-contract.md
docs/specs/asset-metadata-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
docs/CONTENT.md
```

## Implementation constraints

- Make only documentation and `.guide-sync/pending/` changes.
- Do not change implementation source, tests, generated artifacts, build scripts, workflows, or package metadata.
- Do not copy guide-system documents or prompt templates into the repository.
- Do not add TBPs or issue templates.
- Do not activate public docs or release docs.
- Do not make `.guide-profile.json` or `.guide-sync/` ordinary implementation authority.
- Keep updates precise. Avoid broad prose rewriting where an index or cross-link update is enough.
- Preserve repository-relative paths in documentation.
- Delete a pending sync hint only when its completion criteria are met.
- If a hint cannot be fully resolved, narrow it rather than leaving a broad stale hint.

## Required authority documents

The documentation-sync agent must read:

```text
README.md
AGENTS.md
.guide-profile.json
docs/TERMINOLOGY.md
docs/SPECS.md
docs/CONTENT.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/RESEARCH.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/asset-metadata-contract.md
docs/artifacts/scenario-runner-artifact-contract.md
docs/artifacts/content-validation-artifact-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
docs/decisions/ADR-0009-adopt-external-guide-system-v0.2.0.md
docs/decisions/ADR-0010-scenario-runner-before-asset-workbench.md
docs/decisions/ADR-0011-content-validation-before-asset-curation.md
docs/decisions/ADR-0012-asset-metadata-before-visual-workbench.md
.guide-sync/pending/
```

If a listed file is absent, the agent must not invent it. The agent must record the missing file in the implementation summary and either remove/narrow the affected reference or create/narrow a pending hint.

The agent may inspect other active docs when needed for cross-link consistency.

## Files or areas likely affected

Likely replacement files:

```text
README.md
AGENTS.md
docs/MILESTONES.md
docs/SPECS.md
docs/CONTENT.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
docs/ENGINEERING.md
docs/DECISIONS.md
docs/RESEARCH.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/asset-metadata-contract.md
docs/artifacts/scenario-runner-artifact-contract.md
docs/artifacts/content-validation-artifact-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
.guide-sync/pending/*
```

Do not edit source files, test files, generated artifacts, or workflow files.

## Validation tiers and concrete repository commands

### Tier 1 — Documentation consistency

Manual/documentation checks:

```text
all referenced docs exist or are explicitly marked missing with follow-up
all implemented milestones are indexed or intentionally excluded with reason
all accepted ADRs are indexed or intentionally excluded with reason
all current product CLI commands match docs/engineering/product-cli.md
all current eng wrappers match docs/engineering/command-contract.md
all pending guide-sync hints are deleted or narrowed according to completion criteria
no active doc makes external guides, prompt templates, copied guides, .guide-profile.json, or .guide-sync/ ordinary implementation authority
```

### Tier 2 — Repository local validation

Run:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/asset-inspect-smoke.sh
```

Do not require release validation.

If any command is missing or fails, record the exact command and failure in the implementation summary. Do not silently remove a documented command to make validation pass unless the command is genuinely not implemented and docs were stale.

## Acceptance criteria

This milestone is complete when:

- active documentation reflects the implemented state through Milestone 007;
- `docs/MILESTONES.md` indexes Milestones 004, 005, 006, 007, and this documentation-sync milestone, or explicitly records why any is excluded;
- `docs/SPECS.md` indexes implemented scenario runner, content validation, and asset metadata specs when the files exist;
- `docs/ARTIFACTS.md` indexes implemented scenario runner, content validation, and asset inspection artifact contracts when the files exist;
- `docs/DECISIONS.md` indexes ADR-0009 through ADR-0012 when the files exist;
- engineering and product CLI docs match implemented command behavior;
- `.guide-sync/pending/` contains only unresolved, narrow, specific hints;
- completed broad hints are deleted;
- no ordinary implementation path requires reading `.guide-profile.json`, `.guide-sync/`, copied guide docs, external guide internals, or prompt templates;
- no TBPs or issue templates are introduced;
- required validation commands are run and results are reported.

## Direct documentation impact

Expected direct documentation updates:

```text
README.md
AGENTS.md
docs/MILESTONES.md
docs/SPECS.md
docs/CONTENT.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
docs/ENGINEERING.md
docs/DECISIONS.md
docs/RESEARCH.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/asset-metadata-contract.md
docs/artifacts/scenario-runner-artifact-contract.md
docs/artifacts/content-validation-artifact-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
.guide-sync/pending/
```

Only update files that actually need synchronization.

## Deferred documentation synchronization hints

This milestone is itself a documentation synchronization pass. It should primarily consume or narrow existing pending hints instead of creating new broad hints.

Create a new `.guide-sync/pending/` file only if the pass discovers specific remaining documentation work that cannot safely be completed within this milestone.

Any new hint must include:

```text
specific remaining work
why it is not completed now
target documents
completion criteria
```

Ordinary implementation agents must continue to ignore `.guide-sync/`.

## Human review requirements

Human review is required for synchronization quality, not for validating implementation behavior.

The reviewer should check:

```text
are the current implemented capabilities understandable from active docs?
are milestone and decision indexes complete enough?
are scenario/content/asset evidence contracts discoverable?
did the sync pass avoid copying external guide authority into project docs?
did the sync pass delete completed hints and narrow unresolved hints?
are future commands clearly separated from supported commands?
```

Human review must not expand this milestone into public documentation, release readiness, or implementation work.

## Out-of-scope guide migration work

Do not perform a new guide-system migration.

Do not modify guide repository files.

Do not change guide-system version.

Do not copy guide-system docs, prompt templates, or migration prompts into this repository.

The existing `.guide-profile.json` is guide-selection metadata only. This milestone may read it but should not require ordinary implementation agents to read it.

## Implementation summary requirements

The documentation-sync agent must include a final implementation summary with:

```text
files changed
pending hints deleted
pending hints narrowed
pending hints left unchanged with reason
new pending hints created, if any
validation commands run
validation results
missing expected documents or commands, if any
human review notes or explicit statement that review is still pending
```

If validation could not be completed, the summary must state the exact command or check that could not be completed and why.
