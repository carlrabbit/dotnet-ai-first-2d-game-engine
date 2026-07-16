# Milestone 022 — Adopt Guide System v0.5.0: Constrained Validation and Human Review

## Goal

Migrate the repository from guide-system metadata version `0.3.0` to the latest resolved guide-system version `0.5.0` without copying external guides or making them implementation authority.

Adopt the repository-relevant v0.4.0 and v0.5.0 capabilities:

```text
repository-local review state
+ canonical review commands
+ thin engineering launchers
+ tested .NET engineering command host
+ explicit platform support
+ resumable validation suites
+ machine-readable validation plans
+ bounded shards
+ atomic fingerprinted receipts
+ fast aggregate verification
```

Ordinary implementation remains driven exclusively by localized project truth.

## Resolved guide version

```text
latest guide-system version: 0.5.0
current repository metadata version: 0.3.0
migration span: 0.3.0 → 0.4.0 → 0.5.0
```

This version was resolved during planning from the external guide repository README and changelog. Implementation agents do not need the external guide repository.

## Repository role and maturity assumptions

```text
role: capability-provider
profiles:
  - artifact-first-agentic-authoring
  - runtime-tool
  - game-simulation
maturity:
  - implementation-ready
  - artifact-first
execution mode: migration
```

M000–M021 are implemented. The repository has many focused and aggregate headless validation commands, Tier 5 human-review guidance, generated review packs, `.guide-profile.json`, and `.guide-sync/` metadata.

## Scope

1. Update guide-selection metadata from `0.3.0` to `0.5.0`.
2. Classify and document direct, resumable-sharded, CI-only, and human-review execution modes.
3. Add one tested .NET engineering command host for validation-plan, receipt, fingerprint, verifier, and review-state semantics.
4. Keep `eng/*.sh` launchers thin.
5. Convert selected broad suites to the resumable suite interface.
6. Add atomic fingerprinted receipts and fast verification.
7. Add repository-local `.review/` state and canonical review commands.
8. Declare and test actual platform support.
9. Update localized engineering and agent-routing truth.
10. Remove or de-authorize any remaining copied guide leakage.
11. Preserve implementation/documentation-sync separation.

## Migration classification

### Required

- update `.guide-profile.json` to `0.5.0`;
- add localized constrained-validation authority;
- add tested .NET engineering host for fingerprints, receipts, plans, verification, and review state;
- keep complex semantics out of shell;
- convert `m019-smoke`, `m020-smoke`, and `m021-smoke` to resumable-sharded suites;
- add `--list`, `--plan-json`, `--shard`, and `--verify`;
- add atomic receipts under `artifacts/validation/`;
- make `--verify` aggregate success authority;
- activate `.review/` because Tier 5 human review is part of current project truth;
- add review list/request/record/check commands;
- add one migration review request and require its approval before completion;
- update `AGENTS.md`, `docs/ENGINEERING.md`, command contract, validation tiers, human-review docs, milestone/decision indexes, and `.gitignore` directly where behavior changes;
- prohibit success claims based on partial aggregate output.

### Conditional

- convert `check.sh`, `product-validate.sh`, or other aggregate suites only when measurement or decomposition shows they may exceed bounded agent execution;
- convert older milestone suites when they remain active required regression gates and are not reliably bounded;
- add PowerShell launchers only if native Windows becomes an actively tested platform;
- add CI-only suites only when workflows are explicitly activated;
- retain optional review requests outside `.review/` when they do not block completion.

### Deprecated

- one long process as the sole proof of broad validation;
- aggregate success inferred from partial logs;
- backgrounding, `nohup`, `disown`, `setsid`, shell timeout inflation, or heartbeat-only workarounds;
- non-trivial JSON, fingerprint, receipt, or review-state logic in shell;
- copied setup/engineering guides as required reading;
- ordinary implementation agents reading `.guide-profile.json`, `.guide-sync/`, guide internals, or prompt templates.

### Manual review

- choose shard boundaries and runtime budgets;
- confirm repository fingerprint includes relevant untracked files without generated noise;
- verify atomic move behavior on supported filesystems;
- decide whether `check.sh` remains direct or becomes resumable;
- confirm the supported platform declaration;
- review `.review/` ergonomics and stale-record behavior;
- confirm no active documentation treats external guides as operational authority.

### No-op

- no TBPs are introduced;
- no issue templates are introduced;
- no public docs, release structure, package publication, portable SDK, or product capability work is introduced;
- no copied guide files are added;
- no PowerShell support is claimed merely by adding scripts;
- no product CLI commands are repurposed as the engineering API.

## Execution mode

```text
ai-executed-broad migration
```

The migration itself must be executable in bounded focus areas. Its final aggregate validation uses the new resumable model.

## Focus areas

### Focus Area 1 — Inventory and classify validation suites

Create a machine-readable inventory of canonical `eng/` commands containing:

- suite/command ID;
- purpose and validation tier;
- current composition;
- measured or estimated duration;
- output volume;
- artifact dependencies;
- execution mode;
- shard candidacy;
- direct dependencies;
- platform constraints.

Required initial decisions:

```text
m019-smoke: resumable-sharded
m020-smoke: resumable-sharded
m021-smoke: resumable-sharded
review-check: human-review
```

Assess `check.sh` and `product-validate.sh`; do not convert them automatically if they are reliably bounded.

### Focus Area 2 — Engineering command host

Add a dedicated project, recommended:

```text
src/Agentic2D.Engineering
```

It owns:

- suite registry;
- plan generation;
- shard dispatch metadata;
- repository/suite/command/input/result fingerprints;
- atomic receipt creation;
- receipt schema validation;
- fast verification;
- review request/record validation;
- stale review detection;
- structured diagnostics.

It must not become a product runtime API.

Add unit tests under the existing unit-test project or a focused engineering test project consistent with repository conventions.

### Focus Area 3 — Resumable suite contract

For each converted suite support:

```text
./eng/<suite>.sh --list
./eng/<suite>.sh --plan-json
./eng/<suite>.sh --shard <id>
./eng/<suite>.sh --verify
./eng/<suite>.sh
```

No-argument mode may run all shards and verify in unconstrained local/CI contexts.

`--plan-json` must be fast and side-effect-free except writing explicitly requested plan output.

Each shard is independently meaningful, bounded, and executable in a separate agent invocation.

Suggested initial shard decomposition:

```text
m019-smoke:
  sound
  damage-lifecycle
  inventory-collection
  integrated
  replay

m020-smoke:
  save-roundtrip
  incompatibility
  flags-conditions
  switches
  doors
  integrated-resume
  review

m021-smoke:
  effects
  particles
  camera
  ui-text
  interaction-surfaces
  integrated
  replay
  post-load
  review
```

Reuse existing focused wrappers where they already represent valid shards.

### Focus Area 4 — Fingerprints and atomic receipts

Receipts live under:

```text
artifacts/validation/<suite>/<shard>.json
```

The repository fingerprint includes HEAD, tracked changes, and relevant untracked project inputs while excluding generated/transient paths.

A shard removes or invalidates its previous receipt before execution and writes a passing receipt only after command and evidence validation succeed.

Use same-filesystem temporary files and atomic replacement.

`--verify` validates all current receipts and referenced artifacts without rerunning validation.

Add schemas or typed serializers consistent with repository conventions.

### Focus Area 5 — Repository-local review workflow

Create:

```text
.review/pending/
.review/records/
.review/evidence/
```

Add canonical commands:

```text
./eng/review-list.sh
./eng/review-request.sh
./eng/review-record.sh
./eng/review-check.sh
```

Create a migration review request covering:

- suite boundaries;
- receipt/fingerprint trustworthiness;
- review workflow usability;
- platform declaration;
- guide-leakage removal;
- ordinary-agent isolation.

`review-check` fails for unresolved, stale, malformed, superseded, or insufficiently evidenced required/blocking reviews.

Generated/large evidence remains under `artifacts/review/`.

### Focus Area 6 — Platform support

Declare actual support in localized project engineering docs.

Initial inferred policy:

```text
canonical launchers: Bash
required tested platform: Linux
optional graphics-capable environment: explicitly documented separately
native Windows/PowerShell: unsupported until implemented and tested
```

Do not add `.ps1` launchers in this migration unless native Windows support is explicitly selected and validated.

Test path normalization, executable permissions, Git fingerprint behavior, temporary files, and atomic replacement on the declared platform.

### Focus Area 7 — Localized documentation and leakage cleanup

Update only project truth needed by changed behavior:

- `AGENTS.md` with constrained-suite execution routing;
- `docs/ENGINEERING.md`;
- `docs/engineering/command-contract.md`;
- `docs/engineering/validation-tiers.md`;
- `docs/HUMAN-REVIEW.md`;
- `docs/MILESTONES.md`;
- `docs/DECISIONS.md`;
- `.gitignore`;
- relevant milestone validation sections or a shared cross-reference where required.

Search for and remove required-reading references to copied setup/engineering guides. Retained `docs/research/` copies remain explicitly non-authoritative or may be deleted in a later documentation-sync task.

Do not perform broad capability-document synchronization.

## Implementation constraints

- implementation agents read this milestone and only the listed target-repository authority;
- external guides are not required for implementation;
- no copied guide content enters the repository;
- `eng/` remains the stable engineering API;
- launchers forward arguments and exit codes;
- structured/fingerprint semantics live in tested .NET code;
- product CLI and engineering host remain distinct;
- receipts are generated, ignored, atomic, and fingerprinted;
- verification is fast and does not rerun validation;
- failed/interrupted shards cannot leave a passing receipt;
- human review state is small, durable project truth;
- generated review evidence remains under artifacts;
- only supported platforms are claimed;
- existing direct focused wrappers continue to work;
- migration must not change game-engine product behavior.

## Required authority documents

Read only:

```text
README.md
AGENTS.md
.guide-profile.json
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/validation-tiers.md
docs/engineering/constrained-validation-execution.md
docs/engineering/human-review-workflow.md
docs/HUMAN-REVIEW.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/decisions/ADR-0028-engineering-validation-uses-resumable-fingerprinted-suites.md
docs/decisions/ADR-0029-human-review-state-is-repository-local.md
docs/milestones/MILESTONE-019-sound-feedback-gameplay-state-lifecycle-items-and-collection.md
docs/milestones/MILESTONE-020-canonical-save-load-persistent-flags-doors-switches-and-resume-equivalence.md
docs/milestones/MILESTONE-021-deterministic-presentation-effects-camera-declarative-ui-text-and-semantic-interaction-surfaces.md
docs/milestones/MILESTONE-022-adopt-guide-system-v0.5.0-constrained-validation-and-human-review.md
```

The migration task may inspect current `eng/` scripts, engineering source/tests, `.review/` if created, and `.gitignore` as implementation inputs.

Do not read external guide documents during implementation.

## Files or areas likely affected

```text
.guide-profile.json
.review/
.gitignore
AGENTS.md
docs/ENGINEERING.md
docs/HUMAN-REVIEW.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/
docs/decisions/
eng/
src/Agentic2D.Engineering/
tests/unit/Agentic2D.Tests.Unit/
artifacts/validation/  # generated, ignored
artifacts/review/      # generated evidence
```

## Validation tiers and concrete commands

### Tier 0 — Package/document sanity

```bash
python -m json.tool .guide-profile.json >/dev/null
```

Use an existing repository-local JSON validator instead when available.

### Tier 1 — Engineering host and focused contracts

```bash
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
```

Add focused tests for plan, receipt, fingerprint, verifier, review state, and atomic failure behavior.

### Tier 2 — Resumable suite interface

For every converted suite:

```bash
./eng/<suite>.sh --list
./eng/<suite>.sh --plan-json
./eng/<suite>.sh --shard <each-required-id>
./eng/<suite>.sh --verify
```

Required suites:

```bash
./eng/m019-smoke.sh
./eng/m020-smoke.sh
./eng/m021-smoke.sh
```

### Tier 5 — Migration review

```bash
./eng/review-list.sh
./eng/review-check.sh
```

The required migration review must be approved and current.

### Final migration gate

Add:

```bash
./eng/guide-migration-v050.sh --plan-json
./eng/guide-migration-v050.sh --shard <each-required-id>
./eng/guide-migration-v050.sh --verify
```

Suggested shards:

```text
profile-and-docs
engineering-host-tests
m019-suite
m020-suite
m021-suite
review-workflow
platform-and-leakage
```

The final verifier is completion authority.

## Acceptance criteria

1. `.guide-profile.json` reports `0.5.0` and preserves repository role/profiles/maturity.
2. Ordinary implementation agents remain instructed not to read guide metadata or external guides.
3. No external guide or prompt template is copied.
4. Constrained execution modes are localized as project truth.
5. An engineering command host owns structured validation/review semantics.
6. Shell launchers are thin.
7. `m019-smoke`, `m020-smoke`, and `m021-smoke` expose the complete resumable interface.
8. `--plan-json` is valid, fast, machine-readable, and side-effect-free apart from explicit output.
9. Shards are bounded and independently invokable.
10. Previous shard receipts are invalidated before rerun.
11. Passing receipts are written atomically only after success.
12. Failed/interrupted shard tests leave no valid passing receipt.
13. Receipts include current repository, suite, command, and input fingerprints.
14. Repository fingerprints include relevant untracked project files.
15. Generated/transient paths do not invalidate receipts.
16. Source/config/content changes do invalidate relevant receipts.
17. `--verify` is fast and does not rerun validations.
18. Missing, malformed, stale, foreign, failed, or mismatched receipts fail verification.
19. No aggregate success is inferred from partial output.
20. Backgrounding and shell-lifetime escape techniques are absent.
21. `.review/` is activated with pending, records, and evidence structure.
22. Review commands validate schemas, state, evidence, and fingerprints.
23. Required/blocking unresolved or stale reviews fail `review-check`.
24. The migration review is approved and current.
25. Platform support is explicit and honestly tested.
26. PowerShell/native Windows support is not claimed without tests.
27. Existing focused validation commands remain usable.
28. Product CLI behavior is unchanged.
29. No TBPs, issue templates, public docs, release structure, SDK, packaging, or unrelated cleanup is introduced.
30. The final migration verifier passes.

## Direct documentation impact

Update the localized documents listed in Focus Area 7 only where required by implemented behavior.

## Deferred documentation synchronization hints

Created by this package:

```text
.guide-sync/pending/2026-07-16-guide-v050-index-and-validation-sync.md
.guide-sync/pending/2026-07-16-guide-v050-legacy-guide-and-review-followup.md
```

Ordinary implementation agents must not read them.

## Human review requirements

Migration-class review is required. Review the items listed in Focus Area 5 and record the reviewed revision/fingerprint and re-review triggers.

## Out-of-scope guide migration work

No future guide version is anticipated or implemented. Do not add speculative compatibility with later guide models. Do not modify the external guide repository.
