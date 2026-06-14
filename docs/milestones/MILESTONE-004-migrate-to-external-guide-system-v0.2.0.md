# Milestone 004 — Migrate to External Guide System v0.2.0

## Goal

Migrate `carlrabbit/dotnet-ai-first-2d-game-engine` from the old repository-local setup/engineering guide model to the external guide-system model, ending on guide system version `0.2.0`.

This milestone covers both consecutive migration targets:

1. adoption of the external guide-system baseline introduced by guide system `0.1.0`; and
2. adoption of the guide-sync queue and documentation-sync model introduced by guide system `0.2.0`.

The target repository must contain project truth only. External guides are used for planning, migration, documentation synchronization, and release readiness; they are not ordinary implementation authority.

## Repository role

```text
capability provider
```

The repository builds the engine/runtime/tooling capability. It is not yet a consumer game project.

## Confirmed or inferred guide profile

```text
guideRepository: carlrabbit/agentic-project-guides
guideSystemVersion: 0.2.0
repositoryProfiles:
  - artifact-first-agentic-authoring
  - runtime-tool
  - game-simulation
repositoryRole: capability-provider
```

This profile is represented as `.guide-profile.json` for traceability. Ordinary implementation agents must not be required to read `.guide-profile.json`.

## Confirmed or inferred maturity

```text
implementation-ready
artifact-first
```

The repository already has localized project-truth documentation for its engineering substrate, minimal deterministic runtime, and initial product CLI. It should now externalize guide-system traceability instead of relying on copied setup or engineering guide documents.

## Execution mode

```text
guide migration
```

This is not ordinary product implementation, runtime implementation, public documentation, release readiness, or broad documentation synchronization.

## Required authority

A later migration implementation agent must read only the following target-repository authority before applying this milestone:

```text
README.md
AGENTS.md
docs/RESEARCH.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/milestones/MILESTONE-004-migrate-to-external-guide-system-v0.2.0.md
```

The agent should inspect `.guide-profile.json` and `.guide-sync/pending/` only if they already exist in the target repository before migration.

Do not require the migration implementation agent to read copied guide documents under `docs/research/` unless the active repository docs contradict their non-authoritative status.

Do not require ordinary implementation agents to read:

```text
.guide-profile.json
.guide-sync/
docs/research/project-setup-guide-*.md
docs/research/engineering-guide-*.md
external guide repository internals
prompt templates
```

## Scope

Apply the external guide-system migration metadata and routing changes needed for guide system version `0.2.0`.

Create or replace:

```text
.guide-profile.json
AGENTS.md
docs/decisions/ADR-0009-adopt-external-guide-system-v0.2.0.md
.guide-sync/pending/<sync-hint>.md
```

Create `.guide-sync/pending/` by adding pending hint files. A separate `.gitkeep` file is not required when pending hint files exist.

## Non-goals

Do not perform any of the following in this milestone:

```text
runtime implementation
product CLI changes
source or test changes
build script changes
workflow YAML changes
public documentation activation
release readiness
package publishing setup
sample project setup
broad documentation synchronization
TBP creation
issue-template creation
copying guide documents
copying prompt templates
removing legacy research guide copies without manual review
```

Do not add non-root `README.md` files.

Do not make target-repository docs reference external guide documents as ordinary implementation authority.

## Migration classification

| Area | Classification | Required handling |
|---|---|---|
| External guide authority adoption | required | Adopt the external guide-system model for planning, migration, documentation synchronization, and release readiness only. Project docs remain project truth. |
| Guide system version target | required | Set target guide system version to `0.2.0` in `.guide-profile.json`. |
| `.guide-profile.json` | required | Create or update as traceability and guide-selection metadata. Ordinary implementation agents must ignore it. |
| Provider/consumer distinction | required | Record this repository as a capability provider. Do not model it as a consumer game repository. |
| Maturity/profile selection | required | Record implementation-ready/artifact-first maturity and runtime/tool + game/simulation + artifact-first/agentic-authoring profiles. |
| Copied setup/engineering guides under `docs/research/` | conditional | Retain as legacy research if already marked non-authoritative. Do not use as operational authority. Remove later only by manual review or documentation-sync task. |
| `.guide-sync/pending/` | required | Adopt now because deferred documentation synchronization hints must survive handoff. |
| Documentation-sync queue adoption | required | Add pending hint files under `.guide-sync/pending/`. Do not use the queue for implementation instructions or durable requirements. |
| `AGENTS.md` routing | required | Update if it does not explicitly tell ordinary implementation agents to ignore `.guide-profile.json` and `.guide-sync/`. |
| Guide references in project docs | manual-review | Review active docs for guide leakage. Correct only statements that make copied/external guides operational authority. Defer broad cleanup through `.guide-sync/pending/`. |
| Legacy setup/engineering guide leakage | manual-review | Ensure copied guides remain non-authoritative. Defer broad search/cleanup to documentation synchronization. |
| TBPs | no-op | Do not create TBPs. If TBPs are found later, treat as manual-review unless an explicit process-heavy mode activates them. |
| Issue templates | no-op | Do not create issue templates. If issue templates are found later, ensure they are not required by this migration. |
| Prompt template handling | deprecated | Do not copy prompt templates from the guide repository. Prompt templates belong in the external guide repository. |
| Chat-only deferred documentation hints | deprecated | Use `.guide-sync/pending/` for deferred documentation work that must survive handoff. |
| Public documentation maturity | no-op | Do not add public docs or release docs. The repository is not performing release-readiness work in this milestone. |
| Workflow/CI changes | no-op | Do not change workflows. This is a documentation/metadata migration package. |

## Focus areas

### Focus Area A — Add guide profile metadata

#### Goal

Create guide-selection and traceability metadata without making it ordinary implementation authority.

#### Scope

Create or update:

```text
.guide-profile.json
```

The file must state at minimum:

```text
guideSystemVersion: 0.2.0
guideRepository: carlrabbit/agentic-project-guides
targetRepository: carlrabbit/dotnet-ai-first-2d-game-engine
repositoryRole: capability-provider
repositoryProfiles: artifact-first-agentic-authoring, runtime-tool, game-simulation
maturityModes: implementation-ready, artifact-first
ordinaryImplementationAgents.mustRead: false
```

#### Validation tier

Tier 0 — Edit sanity.

#### Required validation

Run:

```bash
python3 -m json.tool .guide-profile.json >/dev/null
```

If Python is unavailable, use any available JSON parser and report the exact command used.

#### Direct documentation impact

None unless an active repository doc contradicts the new metadata.

#### Deferred documentation impact

A documentation synchronization pass may later mention the profile in planning or migration notes, but ordinary implementation docs must not make it required reading.

### Focus Area B — Update agent routing for guide metadata

#### Goal

Ensure ordinary implementation agents ignore `.guide-profile.json` and `.guide-sync/`, while guide migration and documentation-sync tasks may use them.

#### Scope

Replace or update:

```text
AGENTS.md
```

Required routing rules:

- ordinary implementation agents do not read `.guide-profile.json`;
- ordinary implementation agents do not read `.guide-sync/`;
- guide migration, planning, documentation synchronization, and release-readiness tasks may use external guide-system inputs;
- copied guide material under `docs/research/` remains non-authoritative;
- no TBP or issue-template dependency is introduced.

#### Validation tier

Tier 0 — Edit sanity.

#### Required validation

Inspect `AGENTS.md` and verify that it explicitly says ordinary implementation agents ignore:

```text
.guide-profile.json
.guide-sync/
```

#### Direct documentation impact

`AGENTS.md` is directly updated by this focus area.

#### Deferred documentation impact

A documentation synchronization pass may later normalize README/AGENTS wording, but only if the current wording becomes redundant or inconsistent.

### Focus Area C — Record the durable migration decision

#### Goal

Preserve the repository-level decision to use the external guide system without copying guide authority into this repository.

#### Scope

Create:

```text
docs/decisions/ADR-0009-adopt-external-guide-system-v0.2.0.md
```

The ADR must state:

- project docs contain project truth;
- external guides are planning/migration/documentation-sync/release-readiness inputs only;
- `.guide-profile.json` is metadata, not authority;
- `.guide-sync/` is deferred documentation synchronization metadata, not implementation authority;
- copied guide documents and prompt templates must not be introduced as operational docs.

#### Validation tier

Tier 0 — Edit sanity.

#### Required validation

Verify that the ADR exists and does not copy guide bodies or prompt templates.

#### Direct documentation impact

The ADR is part of this migration package.

#### Deferred documentation impact

Indexing the ADR in `docs/DECISIONS.md` is deferred through a guide-sync hint unless the implementation task explicitly includes index updates.

### Focus Area D — Adopt guide-sync pending hints

#### Goal

Create the v0.2.0 deferred documentation synchronization queue with pending hints that must survive handoff.

#### Scope

Create:

```text
.guide-sync/pending/2026-06-13-guide-system-index-sync.md
.guide-sync/pending/2026-06-13-legacy-guide-leakage-review.md
```

Do not create implementation instructions or durable project requirements under `.guide-sync/`.

#### Validation tier

Tier 0 — Edit sanity.

#### Required validation

Verify that each pending hint:

- has a clear purpose;
- is documentation synchronization metadata;
- has completion criteria;
- does not instruct ordinary implementation agents to change source code or runtime behavior.

#### Direct documentation impact

The pending hints are created by this migration package.

#### Deferred documentation impact

The pending hints are themselves deferred documentation synchronization metadata and must be consumed by a later documentation-sync pass.

### Focus Area E — Final migration validation

#### Goal

Prove that the migration package is structurally valid and does not break the current repository validation surface.

#### Scope

Run the required validation commands and provide an implementation summary.

#### Validation tier

Tier 2 — Standard local gate.

#### Required validation

Run:

```bash
python3 -m json.tool .guide-profile.json >/dev/null
./eng/check.sh
```

If `./eng/check.sh` cannot run for environmental reasons, report the exact command failure and concise reason. Do not substitute release validation.

## Validation expectations

Required validation:

| Validation | Tier | Required command |
|---|---:|---|
| Guide profile JSON parse | Tier 0 | `python3 -m json.tool .guide-profile.json >/dev/null` |
| Standard repository gate | Tier 2 | `./eng/check.sh` |

Do not require:

```text
release validation
package smoke tests
benchmarks
scenario validation
human review gates
packaged runtime validation
E2E tests
public documentation validation
workflow validation
```

## Direct documentation impact

This migration package directly changes or creates only:

```text
.guide-profile.json
AGENTS.md
docs/milestones/MILESTONE-004-migrate-to-external-guide-system-v0.2.0.md
docs/decisions/ADR-0009-adopt-external-guide-system-v0.2.0.md
.guide-sync/pending/*.md
```

Do not perform broad documentation synchronization as part of this migration implementation task.

Update another active project doc directly only if it contains a statement that becomes false or actively misleading after this migration.

## Deferred documentation synchronization impact

Deferred work is represented by files under:

```text
.guide-sync/pending/
```

A later documentation synchronization pass should consume these hints and then delete or narrow them according to their completion criteria.

Expected deferred work includes:

- index the migration milestone in `docs/MILESTONES.md`;
- index ADR-0009 in `docs/DECISIONS.md` after acceptance;
- review active docs for guide leakage or accidental implementation authority references;
- confirm copied guide research remains non-authoritative.

## Completion criteria

This milestone is complete when all of the following are true:

- `.guide-profile.json` exists and parses as JSON.
- `.guide-profile.json` declares guide system version `0.2.0`.
- `.guide-profile.json` identifies `carlrabbit/agentic-project-guides` as the guide repository.
- `.guide-profile.json` identifies this repository as a capability provider.
- `AGENTS.md` explicitly tells ordinary implementation agents to ignore `.guide-profile.json` and `.guide-sync/`.
- `docs/decisions/ADR-0009-adopt-external-guide-system-v0.2.0.md` exists.
- `.guide-sync/pending/` exists and contains the deferred documentation synchronization hints from this package.
- No copied guide documents are added.
- No copied prompt templates are added.
- No TBPs or issue templates are added.
- No broad unrelated documentation cleanup is performed.
- `python3 -m json.tool .guide-profile.json >/dev/null` succeeds, or an equivalent JSON parser command succeeds and is reported.
- `./eng/check.sh` succeeds, or the implementation summary reports the exact command failure and concise failure reason.

## Implementation summary requirements

The implementation agent’s final response must include:

```text
Files created/modified
Confirmed guide system version
Confirmed repository role
Confirmed repository profiles
Confirmed maturity modes
Validation commands executed
Validation results
Any deviations from this milestone and why
Deferred documentation synchronization hints created
Manual-review items left for documentation synchronization
```
