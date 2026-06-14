# Milestone 009 — Update to Latest External Guide System v0.3.0

## Goal

Adopt the latest available external guide-system model for this repository.

Resolved latest guide-system version:

```text
0.3.0
```

The update is narrow. The repository already uses external guide-system routing from the v0.2.0 migration and already keeps project truth localized. This milestone updates guide-selection metadata, records the v0.3.0 execution-handoff expectation, and creates deferred documentation synchronization hints where broad cleanup would be unsafe inside the migration itself.

The milestone must not copy guide documents or prompt templates into the repository.

## Repository role and maturity assumptions

Repository role:

```text
capability-provider
```

The repository builds the engine/runtime/tooling capability. It is not a capability-consumer game project.

Current profile:

```text
artifact-first-agentic-authoring
runtime-tool
game-simulation
```

Current maturity:

```text
implementation-ready
artifact-first
```

Current guide-system state:

```text
.guide-profile.json exists and currently declares guideSystemVersion 0.2.0
AGENTS.md already routes ordinary implementation agents to localized project docs
.guide-sync/ exists and is documentation-synchronization metadata
old copied setup/engineering guides may exist under docs/research and are non-authoritative traceability material
```

## Execution mode

```text
engineering-migration
```

Scope size:

```text
small-to-medium guide-system metadata and planning-workflow migration
```

Implementation autonomy:

The implementation agent may replace `.guide-profile.json`, add this milestone document, and add or narrow `.guide-sync/pending/` hints. It must not perform broad documentation synchronization unless explicitly instructed by this milestone.

## Latest guide version resolution

The planning agent resolved the latest guide version by inspecting the external guide repository.

Observed guide-system facts:

- `README.md` declares guide-system version `0.3.0`.
- `CHANGELOG.md` has latest entry `0.3.0`.
- `meta/VERSIONING.md` says minor versions are optional unless the repository uses the affected workflow.
- `meta/MIGRATION-MODEL.md` requires migration planning to resolve the latest version explicitly, inspect changelog and migration documents, classify changes, create a package, and generate an execution prompt.
- `migrations/guide-system-v0.2.0-to-v0.3.0.md` says product-repository changes are conditional and apply when disconnected planning and implementation agents are used.
- `templates/PROMPTS.md` says planning prompts should emit a filled execution prompt in chat and execution prompts should instruct implementation agents to start from the primary milestone, read only target-repository authority documents, not read the external guide repository, and run the specified validation commands.

This repository uses disconnected planning and implementation agents, so the v0.3.0 planning-handoff change applies.

## Scope

Apply a narrow latest-guide-system adoption/update:

```text
update .guide-profile.json guideSystemVersion to 0.3.0
record planning workflow metadata for execution prompt handoff
add this migration milestone
add deferred guide-sync hints for documentation indexes and prompt-handoff audit
preserve ordinary implementation-agent routing
preserve old copied guide docs as non-authoritative traceability only
```

## Non-goals

Do not implement any of the following:

```text
source code changes
test code changes
build or eng script changes
workflow or CI changes
TBPs
issue templates
copied guide documents
copied prompt templates
public documentation
release-readiness structure
broad documentation synchronization
removal of docs/research guide copies unless a separate documentation-sync task approves it
asset/runtime/content feature work
```

## Migration classification

| Class | Work item | Required action |
|---|---|---|
| Required | Latest guide version resolution | Record resolved latest version `0.3.0` in this milestone and update `.guide-profile.json`. |
| Required | Guide profile metadata | Replace `.guide-profile.json` with guide-system version `0.3.0` and planning workflow metadata. |
| Required | Implementation handoff policy | Ensure this migration package and future planning packages emit a filled execution prompt in chat for disconnected implementation agents. |
| Required | External guide authority boundary | Preserve the rule that implementation agents are not required to read the external guide repository. |
| Conditional | `.guide-sync/pending/` hints | Add hints only for deferred index sync and prompt-handoff audit. |
| Conditional | AGENTS.md update | No update required if AGENTS already tells ordinary implementation agents to ignore `.guide-profile.json`, `.guide-sync/`, external guide internals, prompt templates, and copied research guides. |
| Conditional | README.md update | No update required unless current start instructions become stale. |
| Conditional | docs/research guide copies | Retain as non-authoritative traceability unless active docs contradict that rule. |
| Deprecated | Planning packages without execution prompt | Stop producing future planning responses that include only a ZIP and no filled execution prompt. |
| Deprecated | Direct implementation prompts such as “upgrade to latest” | Use a planned migration package with explicit execution prompt instead. |
| Manual-review | Existing pending `.guide-sync/` hints | Review during a documentation-sync pass; delete only when completion criteria are met. |
| Manual-review | Guide leakage audit | Confirm no active docs make copied setup/engineering guides or external guides operational authority for ordinary implementation tasks. |
| No-op | TBPs, issue templates, workflows, public docs | Do not add or modify for this guide update. |
| No-op | Product code and validation behavior | No code or runtime behavior changes required. |

## Focus areas

### 1. Update guide profile metadata

Replace `.guide-profile.json` with a valid JSON document declaring:

```text
guideSystemVersion: 0.3.0
guideRepository: carlrabbit/agentic-project-guides
targetRepository: carlrabbit/dotnet-ai-first-2d-game-engine
repositoryRole: capability-provider
repositoryProfiles: artifact-first-agentic-authoring, runtime-tool, game-simulation
ordinaryImplementationAgents.mustRead: false
```

Add planning workflow metadata that records:

```text
requiresExecutionPromptHandoff: true
latestGuideVersionResolvedAtPlanningTime: true
externalGuideRepositoryRequiredForImplementation: false
```

The added metadata is traceability only. Ordinary implementation agents must not be required to read `.guide-profile.json`.

### 2. Preserve ordinary implementation-agent routing

Verify `AGENTS.md` continues to state that ordinary implementation agents:

```text
read README.md, docs/ENGINEERING.md, docs/engineering/command-contract.md, the relevant milestone, and authority docs listed by that milestone
ignore .guide-profile.json and .guide-sync/ unless the task explicitly requires planning, migration, documentation synchronization, or release readiness
do not treat docs/research copied guide material as operational authority
do not read external guide repository internals or prompt templates for ordinary implementation work
```

If this is already true, do not replace `AGENTS.md`.

### 3. Add deferred documentation synchronization hints

Add or keep `.guide-sync/pending/` hints for:

```text
indexing this guide-system v0.3.0 migration milestone and any related decision if one is later added
checking that planning packages after this migration include filled execution prompts in chat
confirming legacy guide leakage is still absent from active docs
```

Do not require ordinary implementation agents to read `.guide-sync/`.

### 4. Do not perform broad documentation synchronization

This is a migration package, not a documentation-sync pass.

Do not update all indexes now unless the migration implementation requires it for safety. Prefer `.guide-sync/pending/` hints for later documentation synchronization.

### 5. Validate migration safety

Run JSON validation for `.guide-profile.json` and the repository validation gates listed below.

## Implementation constraints

- Do not copy files from `carlrabbit/agentic-project-guides` into this repository.
- Do not reference external guide documents as ordinary implementation authority.
- Do not introduce repository-local prompt templates.
- Do not introduce TBPs or issue templates.
- Do not delete old copied research guides unless a separate documentation-sync milestone explicitly does so.
- Do not change product code, runtime behavior, validation behavior, or generated artifacts.
- Do not add source files, test files, workflows, package publishing, or public docs.

## Required authority documents

The implementation agent must read:

```text
README.md
AGENTS.md
.guide-profile.json
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/MILESTONES.md
docs/DECISIONS.md
this milestone document
```

The implementation agent may read:

```text
.guide-sync/pending/
docs/RESEARCH.md
docs/research/project-setup-guide-v7.md
docs/research/engineering-guide-v6.md
docs/research/agentic-2d-game-engine-project-summary.md
```

Only read the optional research files to confirm legacy status. Do not treat them as operational authority.

The implementation agent must not read the external guide repository to complete this migration. The latest version and required migration facts are already captured in this milestone.

## Files or areas likely affected

Direct files:

```text
.guide-profile.json
docs/milestones/MILESTONE-009-update-to-latest-guide-system-v0.3.0.md
.guide-sync/pending/2026-06-14-guide-system-v030-index-sync.md
.guide-sync/pending/2026-06-14-guide-system-v030-execution-handoff-audit.md
```

Conditional files only if current contents contradict this milestone:

```text
AGENTS.md
README.md
docs/MILESTONES.md
docs/DECISIONS.md
```

## Validation tiers and concrete repository commands

Tier 0 metadata validation:

```bash
python3 -m json.tool .guide-profile.json >/dev/null
```

Tier 2 standard local gate:

```bash
./eng/check.sh
```

Current product validation gates should still pass because this migration must not change product behavior:

```bash
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/asset-inspect-smoke.sh
```

If a command does not exist in the checked-out repository, do not create a placeholder. Report the missing command and run the existing documented gates.

## Acceptance criteria

This milestone is complete when:

- `.guide-profile.json` exists, parses as JSON, and declares `guideSystemVersion` as `0.3.0`.
- `.guide-profile.json` records the repository role, profiles, and ordinary implementation-agent ignore paths.
- `.guide-profile.json` records that planning workflow requires execution prompt handoff.
- This milestone document exists under `docs/milestones/`.
- At least one `.guide-sync/pending/` hint exists for indexing this migration.
- At least one `.guide-sync/pending/` hint exists for future execution-prompt handoff audit.
- Ordinary implementation routing still does not require `.guide-profile.json`, `.guide-sync/`, copied guide docs, external guide internals, or prompt templates.
- No guide documents or prompt templates are copied into the repository.
- No product source, test, workflow, package, public docs, TBP, or issue-template files are added.
- Required validation commands have passed or any missing existing command is reported without creating placeholders.

## Direct documentation impact

Direct documentation impact is limited to:

```text
this migration milestone
.guide-profile.json
.guide-sync/pending/ hints
```

Do not perform broad index cleanup inside this migration unless required for safety.

## Deferred documentation synchronization hints

This milestone creates pending hints for:

```text
.guide-sync/pending/2026-06-14-guide-system-v030-index-sync.md
.guide-sync/pending/2026-06-14-guide-system-v030-execution-handoff-audit.md
```

A later documentation-sync pass should update or confirm:

```text
docs/MILESTONES.md
docs/DECISIONS.md
docs/RESEARCH.md
README.md
AGENTS.md
existing .guide-sync/pending/ hints
```

Delete a hint only when its completion criteria are satisfied. Otherwise replace it with a narrower remaining-work hint.

## Human review requirements

Human review should confirm:

- `0.3.0` is the intended latest guide-system version adopted by this migration.
- The updated `.guide-profile.json` remains metadata only and does not become implementation authority.
- Future planning responses include filled execution prompts for disconnected implementation agents.
- The migration does not accidentally make implementation agents read the external guide repository.

## Out-of-scope guide migration work

Out of scope:

```text
migration to any guide-system version after 0.3.0
copying latest guide prompts into this repository
rewriting all past milestone packages
retroactively editing prior chat responses
removing research copies of old guides
broad documentation synchronization
release-readiness migration
```
