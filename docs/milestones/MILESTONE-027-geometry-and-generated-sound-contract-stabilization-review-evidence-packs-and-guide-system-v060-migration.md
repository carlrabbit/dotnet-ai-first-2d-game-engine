# Milestone 027 — Geometry and Generated-Sound Contract Stabilization, Review Evidence Packs, and Guide-System v0.6.0 Migration

## Goal

Stabilize the two consumer-authoring capabilities proven by M026, make their evidence independently inspectable, and migrate repository-local human review from repository-wide staleness to milestone-scoped completion gates.

```text
M026 geometry diagnostics
→ stable versioned authoring artifacts and diagnostics

M026 generated-sound linkage
→ stable linkage, provenance, inspection, and export validation

interactive review evidence
→ bounded durable consumer-authoring review packs

repository-wide review staleness
→ milestone-owned review requests and immutable completed records

.guide-profile.json 0.5.1
→ 0.6.0 after M027 migration acceptance
```

Primary acceptance question:

> Can authors, agents, and reviewers diagnose geometry and generated-sound problems from stable artifacts, preserve sufficient milestone evidence, and complete M027 without reopening historical reviews or reading the external guide repository?

## Current and target guide-system versions

```text
current recorded version: 0.5.1
target resolved latest version: 0.6.0
intermediate semantic update: 0.5.2
migration applicability: required because repository-local human review is active
```

Version 0.5.2 adds ephemeral numeric aliases for interactive review commands while canonical IDs remain durable.

Version 0.6.0 requires:

- every pending review belongs to one milestone;
- review requests and milestone checks use explicit milestone context, while interactive review targets use canonical IDs or current aliases;
- review approval gates only the owning milestone;
- completed records are immutable historical evidence;
- later commits do not stale completed reviews;
- future milestones declare their own review requirements;
- generic repository-wide approval staleness is removed.

The implementation agent must not read the external guide repository. This milestone and its listed authority localize all required decisions.

## Repository role and maturity

```text
repository role: capability-provider
bounded dogfood consumers:
  - Signal Passage
  - Autonomous Tic-Tac-Toe
profiles:
  - artifact-first-agentic-authoring
  - runtime-tool
  - game-simulation
maturity:
  - implementation-ready
  - artifact-first
execution mode:
  - ai-executed-broad
```

Broad execution is justified because M026 normalized the architecture, identified only geometry diagnostics and generated-sound linkage as `formalize-next`, supplied two consumer fixtures, and established resumable validation.

## Execution mode

```text
ai-executed-broad
```

Implement as four coherent transformation families:

1. stable geometry authoring contracts;
2. stable generated-sound linkage contracts;
3. guide-system v0.6.0 milestone-scoped review migration;
4. durable review packs, diagnostic integration, consumer migration, and aggregate proof.

## Change classification

### Required

- update `.guide-profile.json` from 0.5.1 to 0.6.0 after migration validation;
- record 0.5.2 and 0.6.0 as adopted;
- bind every pending review to one milestone;
- make review list/request/record/check milestone-scoped;
- stop invalidating completed records because later repository state changes;
- preserve completed M022, M025, and M026 decisions as historical evidence where decisions exist;
- preserve canonical IDs and ephemeral interactive aliases;
- stabilize geometry artifacts, diagnostics, ordering, and fingerprints;
- stabilize generated-sound linkage, provenance, diagnostics, and export inclusion;
- produce bounded durable authoring review packs;
- add resumable M027 validation.

### Conditional

- classify legacy pending reviews without milestone ownership;
- move already-decided pending requests to the repository's historical representation;
- retain evidence-refresh rules only for active M027 review before completion;
- capture graphical evidence only in a supported graphics environment;
- use explicit supported absence when capture is unavailable;
- temporarily read M026 artifact shapes through bounded migration adapters when needed.

### Deprecated

- repository-wide human approval state;
- generic `approved-but-stale` historical records;
- future-change re-review triggers on completed records;
- global perpetual `review-check`;
- reopening M022, M025, or M026 because M027 changes the repository;
- filename-only generated-sound linkage;
- unversioned geometry diagnostic artifacts;
- blocking interactive review without durable evidence manifests.

### Manual review

- assign ambiguous legacy pending requests;
- decide whether ambiguous items represent unfinished work or historical residue;
- approve M027 author-facing diagnostic stability and evidence-pack usability;
- confirm migration preserved historical decisions without inventing approval;
- decide any explicitly permitted waiver for unavailable graphical evidence.

### No-op

- automated resumable receipts remain fingerprinted and may stale;
- deterministic randomness and persistence policy remain unchanged;
- small performance workloads remain non-timing-authoritative;
- scaled workloads remain comparison authority;
- no third consumer is required;
- no guide documents are copied.

## Scope

### Geometry stabilization

- version inspection, preview, diagnostics, projection comparison, and review-pack schemas;
- stable diagnostic codes and severity;
- deterministic ordering and semantic fingerprints;
- stable source path and field-path representation;
- graphical capture metadata without pixel authority;
- one complete geometry review-pack command;
- migrate Signal Passage and Autonomous Tic-Tac-Toe;
- compatibility or precise migration diagnostics for M026 artifacts.

### Generated-sound stabilization

- version linkage, provenance, report, and review-pack schemas;
- stable identities for synthesis source, output, provenance, ordinary sound definition, and export member;
- field-level diagnostics;
- missing, stale, orphaned, duplicate, incompatible, and export-invalid validation;
- structured inspection and Markdown index;
- migrate both consumers;
- ordinary sound definitions remain runtime authority.

### Review migration

- milestone ownership in requests and records;
- milestone-scoped commands;
- immutable historical completed records;
- canonical IDs plus ephemeral aliases;
- legacy review inventory and classification;
- M027-only blocking review gate;
- durable evidence-pack references;
- active project documentation and tested engineering implementation updated consistently.

### Evidence and diagnostic integration

- bounded review-pack manifest;
- representative captures;
- explicit skip or missing-capture status;
- improved scenario assertion output;
- deterministic random trace display;
- persistence comparison output;
- source-to-artifact-to-evidence indexes.

## Non-goals

Do not add a third consumer, SDK, plugin loader, package distribution model, delayed-action scheduler, turn framework, generic AI framework, UI redesign, prompt framework, registration source generation, broad behavior analyzer, generic persistence framework, new scenario DSL, pixel-diff testing, aesthetic scoring, accessibility certification, automatic layout repair, runtime sound synthesis, new backend, heavyweight benchmarking, broad documentation cleanup, copied guides, TBPs, or issue templates.

## Focus Area 1 — Stable geometry authoring contracts

Stable schemas:

```text
agentic2d.geometry-inspection.v1
agentic2d.geometry-preview.v1
agentic2d.geometry-diagnostics.v1
agentic2d.geometry-projection-comparison.v1
agentic2d.geometry-review-pack.v1
```

Required fields include project, definition, part, source path, source field path, normalized geometry, bounds, anchor, offset, rotation, fill, outline, opacity, layer/order, preview background, diagnostic identity, remediation, source fingerprint, output fingerprint, capture status, and schema/tool version.

Required diagnostic meanings:

```text
GEO001 unknown geometry kind
GEO002 invalid or non-finite dimension
GEO003 invalid opacity
GEO004 invalid outline or ring width
GEO005 invalid polygon or point set
GEO006 unresolved visual reference
GEO007 low foreground/background distinguishability
GEO008 structural/capture metadata mismatch
GEO009 unsupported capture environment
```

Add a product command consistent with current CLI conventions:

```bash
agentic2d geometry review-pack <project> --output <directory>
```

The pack includes manifest, index, inspection, preview, diagnostics, projection comparison, capture status, representative images when available, and omissions.

Pixels are review evidence, never simulation authority.

## Focus Area 2 — Stable generated-sound linkage contracts

Stable schemas:

```text
agentic2d.generated-sound-linkage.v1
agentic2d.generated-sound-provenance.v1
agentic2d.generated-sound-linkage-report.v1
agentic2d.generated-sound-review-pack.v1
```

Authority:

```text
synthesis definition: generation source truth
generated WAV + provenance: derived artifact
ordinary sound definition: runtime authority
linkage: validation and inspection relationship
```

Required diagnostic meanings:

```text
SNDL001 generated output missing
SNDL002 output hash differs from provenance
SNDL003 sound definition references unknown output
SNDL004 generated output has no ordinary sound definition
SNDL005 duplicate incompatible linkage
SNDL006 stale synthesis-definition fingerprint
SNDL007 unknown synthesis definition
SNDL008 missing or malformed provenance
SNDL009 export omitted linked output
SNDL010 linkage schema/version unsupported
```

Add:

```bash
agentic2d sound linkage review-pack <project> --output <directory>
```

Reports identify exact source, output, provenance, runtime definition, export inclusion, consuming cues, hashes, diagnostics, and status.

## Focus Area 3 — Guide-system v0.6.0 review migration

Every pending request contains canonical ID, owning milestone ID and path, class, applicability, subject, evidence, criteria, acceptable decisions, reviewer role, waiver policy, and status.

Completed records contain canonical ID, owning milestone, decision, reviewer, evidence references/fingerprints, reviewed revision for provenance, conditions, and completion time.

Completed records are immutable historical evidence. Generic future-change re-review triggers are removed.

The full v0.6.0 command family is authoritative in `docs/engineering/human-review-workflow.md`. Requests and checks carry an explicit milestone selector; show, record, and reopen resolve a canonical review ID or the latest list alias. Interactive aliases are current-session conveniences only, while durable files and references use canonical IDs only.

Inspect `.review/pending/`, `.review/records/`, and `.review/closed/` when present. Classify each pending item:

```text
active-owned
historical-completed
unfinished-focused-work
ambiguous-manual-review
invalid
```

Produce:

```text
artifacts/review-migration/M027/review-migration-report.json
artifacts/review-migration/M027/review-migration-report.md
```

Do not create approvals. Do not ask humans to reapprove completed milestones solely because later commits occurred.

## Focus Area 4 — Durable evidence packs and diagnostics

Create packs under a stable artifact contract:

```text
review-pack/
├─ manifest.json
├─ index.md
├─ geometry/
├─ sound-linkage/
├─ scenarios/
├─ persistence/
├─ performance/
└─ captures/
```

Manifest records milestone, canonical review ID, source revision, schema versions, evidence paths, sizes, hashes, required/optional status, capture status, omissions, and pack fingerprint.

Scenario diagnostics show assertion path, expected, actual, relevant state, event/cue context, and deterministic random trace where relevant.

Persistence diagnostics show section/contributor, before/after state, pending delay, transient replay policy, and mismatch path.

Representative evidence only; exclude full exports, all frames, repeated logs, and unbounded streams.

## Implementation constraints

- project docs contain project truth only;
- ordinary agents ignore `.guide-profile.json`, `.guide-sync/`, external guides, copied guides, and prompt templates;
- review migration cannot create decisions;
- historical approvals remain historical;
- automated receipts remain fingerprinted;
- geometry remains presentation-only;
- rendering remains read-only;
- synthesis remains offline;
- ordinary sound definitions remain runtime authority;
- both consumers are compatibility fixtures;
- performance policy is unchanged;
- no broad extension API is introduced;
- `.guide-profile.json` becomes 0.6.0 only when migration acceptance is ready to pass.

## Required authority documents

Read only:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/SCENARIOS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/engineering/constrained-validation-execution.md
docs/engineering/human-review-workflow.md
docs/engineering/performance-measurement.md
docs/engineering/m027-guide-v060-review-migration.md
docs/specs/geometric-visual-definition-and-projection-contract.md
docs/specs/geometry-authoring-diagnostics-contract.md
docs/specs/generated-sound-linkage-contract.md
docs/specs/deterministic-offline-sound-synthesis-contract.md
docs/specs/sound-definition-and-command-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/canonical-save-snapshot-contract.md
docs/specs/save-load-and-resume-contract.md
docs/specs/scaled-performance-workload-contract.md
docs/artifacts/consumer-boundary-decision-report-artifact-contract.md
docs/artifacts/consumer-authoring-review-pack-artifact-contract.md
docs/decisions/ADR-0039-m027-combines-authoring-contract-stabilization-with-guide-system-v060-review-migration.md
docs/milestones/MILESTONE-025-reference-game-workspace-procedural-presentation-synthesized-sound-and-extension-discovery.md
docs/milestones/MILESTONE-026-consumer-authoring-diagnostics-generated-sound-linkage-scaled-performance-workloads-and-autonomous-tic-tac-toe-consumer-validation.md
docs/milestones/MILESTONE-027-geometry-and-generated-sound-contract-stabilization-review-evidence-packs-and-guide-system-v060-migration.md
```

Read `.review/` only for this migration and M027 review. Do not read the external guide repository.

## Files or areas likely affected

```text
.guide-profile.json
AGENTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/SPECS.md
docs/ARTIFACTS.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/human-review-workflow.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
.review/pending/
.review/records/
.review/closed/
src/Agentic2D.Validation
src/Agentic2D.Rendering
src/Agentic2D.Sound
src/Agentic2D.Tools
src/Agentic2D.Engineering
tests/unit/Agentic2D.Tests.Unit
eng/
consumers/signal-passage/
consumers/autonomous-tic-tac-toe/
artifacts/geometry/M027/
artifacts/sound-linkage/M027/
artifacts/review/M027/
artifacts/review-migration/M027/
artifacts/validation/m027-smoke/
```

Use actual current ownership; do not create empty projects.

## Validation tiers and concrete commands

### Review migration

```bash
./eng/build.sh
./eng/test-filter.sh Review
./eng/review-list.sh --milestone M027
./eng/review-show.sh review.m027.authoring-contracts-review-evidence-and-v060-migration
./eng/review-command-smoke.sh
./eng/review-alias-smoke.sh
./eng/review-migration-smoke.sh
./eng/review-check.sh --milestone M027
```

Before approval, the M027 check may report only the M027 review as pending. It must not report M022, M025, or M026 historical approvals as stale.

### Geometry

```bash
./eng/test-filter.sh Geometry
./eng/content-validate.sh visuals
./eng/geometry-diagnostics-smoke.sh
./eng/geometry-review-pack-smoke.sh
```

### Sound linkage

```bash
./eng/test-filter.sh SoundLinkage
./eng/content-validate.sh sounds
./eng/generated-sound-linkage-smoke.sh
./eng/generated-sound-review-pack-smoke.sh
```

### Evidence diagnostics

```bash
./eng/consumer-authoring-review-pack-smoke.sh
./eng/scenario-diagnostics-smoke.sh
./eng/persistence-diagnostics-smoke.sh
```

### Consumers and integration

```bash
./eng/signal-passage-smoke.sh
./eng/tic-tac-toe-smoke.sh
./eng/tic-tac-toe-persistence.sh
./eng/tic-tac-toe-export.sh
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/perf-smoke.sh
./eng/scaled-performance-smoke.sh
```

Use the nearest existing focused-test syntax when exact filters differ.

### Resumable aggregate

```bash
./eng/m027-smoke.sh --plan-json
./eng/m027-smoke.sh --shard review-migration
./eng/m027-smoke.sh --shard geometry-contracts
./eng/m027-smoke.sh --shard sound-linkage-contracts
./eng/m027-smoke.sh --shard review-packs
./eng/m027-smoke.sh --shard scenario-persistence-diagnostics
./eng/m027-smoke.sh --shard signal-passage
./eng/m027-smoke.sh --shard tic-tac-toe
./eng/m027-smoke.sh --shard performance-regression
./eng/m027-smoke.sh --shard documentation
./eng/m027-smoke.sh --shard human-review
./eng/m027-smoke.sh --shard integrated
./eng/m027-smoke.sh --verify
```

## Validation execution mode

```text
focused checks: direct
M027 aggregate: resumable-sharded
M027 authoring and migration acceptance: human-review, required and blocking
```

Receipt root:

```text
artifacts/validation/m027-smoke/
```

Fingerprint scope includes review implementation and state migration, stable geometry and sound schemas, both consumer fixtures, review-pack manifests, diagnostics, M027 review state, `.guide-profile.json`, and direct project docs.

Exclude completed historical review fingerprints as current gate inputs, non-semantic timestamps, absolute temporary paths, graphics device identity, observational timing values, and external guide state.

Only `./eng/m027-smoke.sh --verify` establishes aggregate success.

## Acceptance criteria

### Guide migration and review model

1. `.guide-profile.json` records 0.6.0 and includes 0.5.2 and 0.6.0.
2. Every pending review has one owning milestone.
3. Canonical IDs remain durable and numeric aliases remain ephemeral.
4. Requests and checks require explicit milestone context; review targets are canonical IDs or current aliases.
5. `review-check --milestone M027` evaluates only M027.
6. Completed M022, M025, and M026 decisions are not stale because of later commits.
7. Historical provenance is preserved.
8. Generic future-change triggers are removed from completed records.
9. No approval is fabricated.
10. Ambiguous legacy pending requests are manual-review items.
11. Migration reports exist in JSON and Markdown.
12. Active docs consistently describe milestone-scoped review.
13. Ordinary implementation agents remain isolated from guide metadata.

### Geometry

14. Required stable schemas are emitted.
15. Stable diagnostic meanings exist.
16. Ordering and fingerprints are deterministic.
17. Source field paths are present.
18. Capture status is explicit.
19. Missing graphics capability is represented accurately.
20. Review packs contain bounded evidence.
21. Both consumers pass.
22. M026 artifacts migrate or receive actionable diagnostics.
23. No pixel authority, aesthetic scoring, or layout mutation is introduced.

### Generated sound

24. Linkage and provenance schemas are versioned.
25. Stable diagnostic meanings exist.
26. Field-level inspection connects source, output, provenance, runtime definition, and export.
27. Missing output, hash drift, stale source, missing definition, invalid orphan, duplicate linkage, and export omission are detected.
28. Both consumers pass.
29. Runtime sound authority remains unchanged.

### Evidence and diagnostics

30. Review-pack schema exists.
31. Manifest hashes match members.
32. Omitted captures have explicit reasons.
33. Full exports and repetitive frames are excluded.
34. Scenario failures show expected and actual state paths.
35. Deterministic choices are inspectable.
36. Persistence comparison shows transient replay policy.
37. Evidence survives outside generated working directories.

### M027 review and aggregate

38. Canonical review ID is `review.m027.authoring-contracts-review-evidence-and-v060-migration`.
39. Request identifies M027 and its path.
40. Evidence covers both consumers and review migration.
41. Reviewer confirms diagnostic and evidence-pack usability.
42. Reviewer confirms historical approvals were preserved without perpetual staleness.
43. Completion decision is approved, or a waiver only after explicit request amendment.
44. `./eng/review-check.sh --milestone M027` passes.
45. Completed M027 record becomes historical.
46. M027 supports list, plan, shard, verify, and no-argument modes.
47. Receipts are atomic and fingerprinted.
48. Partial logs are not aggregate proof.
49. M019–M026 regressions remain valid or explicitly superseded.
50. Performance policy remains unchanged.
51. No broad extension platform or third consumer is introduced.
52. Final verifier passes.

## Direct documentation impact

Update active project truth only:

```text
AGENTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/SPECS.md
docs/ARTIFACTS.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/human-review-workflow.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/specs/geometry-authoring-diagnostics-contract.md
docs/specs/generated-sound-linkage-contract.md
```

Do not perform broad unrelated synchronization.

## Deferred documentation synchronization hints

```text
.guide-sync/pending/2026-07-18-m027-v060-review-model-and-authoring-contract-sync.md
.guide-sync/pending/2026-07-18-m027-consumer-evidence-roadmap-and-release-readiness-sync.md
```

Ordinary implementation agents do not read them.

## Human-review requirements

```text
applicability: required
blocking: yes
classes:
  - migration
  - artifact-quality
  - visual
canonical ID:
  review.m027.authoring-contracts-review-evidence-and-v060-migration
owning milestone: M027
exact check:
  ./eng/review-check.sh --milestone M027
acceptable completion:
  approved
  waived only after explicit request amendment
```

Required evidence includes review-migration reports, legacy classification, alias/canonical-ID tests, geometry and sound review packs for both consumers, representative graphical evidence or explicit supported absence, scenario and persistence examples, current non-review receipts, and direct documentation diff summary.

Future milestones declare their own review rather than reopening M027.

## Constrained-runtime handling

1. Run `./eng/m027-smoke.sh --plan-json`.
2. Execute non-review shards separately.
3. Stop on real failure.
4. Generate M027 review packs and migration report.
5. Create or refresh the M027 request.
6. Run `./eng/review-check.sh --milestone M027`.
7. If pending, report the canonical review ID and stop without claiming completion.
8. After decision, run the human-review shard.
9. Run `./eng/m027-smoke.sh --verify`.
10. Report exact verifier output.

Do not use backgrounding, detached processes, timeout inflation, or partial logs as proof.

## Rollback and recovery

- Do not mark `.guide-profile.json` 0.6.0 until migration validation is ready.
- Preserve a pre-migration review inventory and hashes.
- Never delete historical decisions.
- Leave ambiguous requests for manual review rather than guessing.
- Make review command changes one coherent reversible transformation.
- Readers may temporarily accept M026 artifact shapes; writers emit only stable M027 schemas.
- Failed schema migration leaves authored consumer source unchanged.
- Review packs are regenerable; durable records and source definitions are not silently rewritten.
- If M027 review remains pending, retain request and evidence and do not mark migration complete.

## Out-of-scope guide work

Do not read or modify the guide repository during implementation, copy guide documents or prompts, reference guide files as operational authority, add TBPs or issue templates, or perform a separate guide release.
