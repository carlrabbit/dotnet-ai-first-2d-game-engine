# Milestone 028 — Shared Asset Library, Reusable Discovery Profiles, and Campaign Foundations

## Goal

Create the reusable local asset-authoring foundation for multiple Agentic2D games:

```text
shared machine-local raw asset library
→ deterministic reusable discovery profile
→ game-specific campaign overlay
→ bounded candidate batches
→ headless review evidence
→ retained reusable annotations and game-local decisions
```

M028 establishes the provider capability and authority boundaries needed by the later interactive workbench milestone. It does not yet deliver the complete persistent audiovisual workbench or integrate curated assets into the reference game.

Primary acceptance question:

> Can one local source library be discovered once, safely refreshed or cleaned, reused by two different game campaigns without leaking game semantics into shared metadata, and inspected through deterministic headless evidence?

## Repository role and maturity assumptions

```text
repository role: capability-provider
bounded dogfood role: two campaign fixtures; no full reference-game replacement
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation
maturity: implementation-ready, artifact-first
execution mode: ai-executed-broad
```

The current guide-system version is `0.6.0`, and the latest resolved guide-system version is also `0.6.0`. No guide migration is part of M028.

## Execution mode

`ai-executed-broad`

Implement as five coherent transformation families:

1. shared local asset-home resolution and source registry;
2. deterministic image/audio discovery profiles and disposable generated metadata;
3. retained reusable human annotations;
4. game-local campaigns, batches, proposals, and headless review packs;
5. resumable provider validation and milestone-owned human review.

Do not split by arbitrary file groups.

## Scope

### Shared local asset home

Provide one machine-local, non-source-controlled asset home.

Linux default:

```text
${XDG_DATA_HOME:-$HOME/.local/share}/agentic2d/assets
```

Override:

```text
AGENTIC2D_ASSET_HOME
```

Initial assumptions: one user, one machine, local filesystem, no server, no authentication, no remote synchronization, no multi-user locking, and atomic durable metadata writes. Raw assets and discovery metadata are never runtime dependencies.

The asset home contains raw sources, reusable discovery profiles, retained reusable annotations, generated previews, temporary sessions, and disposable cache data.

### Source registry and identity

Register local raw sources without copying them into games by default. Source identity is stable and independent of absolute path. A source revision uses deterministic package- and file-level fingerprints. Changed bytes create a new current fingerprint and never silently reuse stale discovery metadata.

### Reusable discovery profiles

Build deterministic profiles for:

- one grid-based image or spritesheet;
- one irregular-region image;
- one animation candidate;
- one bounded audio library or audio group.

Profiles include source/file fingerprints, relative inventory, dimensions, transparency, grid candidates, irregular regions, duplicate groups, animation candidates, audio properties, likely license files, diagnostics, contact sheets, overlays, waveforms, bounded preview audio, and producer/schema versions.

Profiles must not assert final project-specific meaning.

### Disposable generated metadata

Generated observations and previews are rebuildable. They may be removed when raw fingerprints change, schema compatibility is unsupported, relevant tooling changed, the user requests cleanup/refresh, or metadata is malformed. Do not build a general migration framework for rebuildable observations.

### Retained reusable annotations

Human reusable corrections are separate from disposable metadata. They may correct grids/groups, exclude files, fix ordering, or associate license observations. They reference source identity and compatible fingerprint scope, record reason and provenance, survive generated cleanup, and never encode game-specific gameplay meaning.

### Campaign and batch foundations

A game-local campaign references shared profiles but owns target game/workspace, profile fingerprints, requested presentation roles, proposed interpretations, taxonomy/choice vocabulary, batch definitions, review policy, future integration targets, fallback policy, and completion criteria.

Campaign interpretation never mutates base discovery. Batches are bounded, independently inventoryable, proposable, validatable, review-packable, and retryable.

M028 must prove two campaigns reusing the same profile differently.

### Headless review evidence

Generate deterministic packs with source/profile summaries, indexed contact sheets, region overlays, duplicate groups, animation candidates, waveform images, preview WAVs, campaign proposals, candidate groups, unresolved decisions, dependency impact, diagnostics, manifest, and hashes.

Headless evidence is required. Live preview is deferred to M029.

## Non-goals

Do not implement the complete interactive workbench, persistent preview window, preview-host process, IPC, live A/B comparison, numeric workbench aliases, final semantic approval, asset promotion, reference-game replacement, portable profile bundles, remote sharing, multi-user database, SQLite/server storage without evidence, marketplace, AI image generation, graphical editor, professional audio/vector editor, every commercial format, runtime dependency on asset home, source-controlled raw library, broad guide migration, TBPs, or issue templates.

## Focus Area 1 — Shared asset home and source registry

Authority: `docs/specs/shared-asset-home-and-source-registry-contract.md`.

## Focus Area 0 — M011 Asset Capability Audit and Generalization

Blocking before discovery reuse: audit `AssetInspector`, `AssetPerceiver` (the M011 perception reporter), `AssetReviewApplier`/review projection, `AssetReviewDecisionSource` validator, `AssetCurationWorkbenchGenerator`, and the review-pack workflow index. Each receives exactly one evidence-backed disposition: `reuse-as-is`, `generalize`, `replace`, `retain-for-compatibility`, or `deprecate` in `artifacts/assets/M028/m011-capability-audit.{json,md}`. Search production code for smoke names, fixture dimensions/counts/IDs/categories, tile coordinates, and animation sequences. Fixtures may contain expectations; production discovery may not select behavior from them.

M011 established the initial asset-workflow architecture and artifact flow. M028 is responsible for proving and delivering generalized production-capable discovery over previously unseen image and audio libraries. Discovery means analysis from source bytes and structural metadata; filename-derived evidence is explicitly labeled and is never content perception. Production discovery accepts arbitrary registered paths, derives candidate IDs from source/content geometry, emits structured diagnostics for unsupported input, and must not report successful discovery when no meaningful byte-derived analysis occurred.

Preserve these authority classes:

```text
asset-home/registry
asset-home/sources
asset-home/profiles
asset-home/annotations
asset-home/previews
asset-home/sessions
asset-home/cache
```

Required product CLI surface, normalized to current style:

```bash
agentic2d asset home inspect --output <directory>
agentic2d asset home clean --stale --output <directory>
agentic2d asset source add <path> [--name <display-name>] --output <directory>
agentic2d asset source list --output <directory>
agentic2d asset source show <source-id> --output <directory>
agentic2d asset source refresh <source-id> --output <directory>
agentic2d asset source clean <source-id> --generated-only --output <directory>
```

Cleanup never removes raw bytes or retained annotations. Failed refresh preserves the last valid profile pointer.

## Focus Area 2 — Reusable discovery profiles

Authority: `docs/specs/reusable-asset-discovery-profile-contract.md`.

Schemas:

```text
agentic2d.asset-source.v1
agentic2d.asset-source-file.v1
agentic2d.asset-discovery-profile.v1
agentic2d.asset-image-observation.v1
agentic2d.asset-audio-observation.v1
agentic2d.asset-region-candidate.v1
agentic2d.asset-duplicate-group.v1
agentic2d.asset-animation-candidate.v1
agentic2d.asset-license-observation.v1
```

Outputs:

```text
source-profile.json
source-files.jsonl
image-observations.jsonl
audio-observations.jsonl
region-candidates.jsonl
duplicate-groups.json
animation-candidates.json
license-observations.json
discovery-diagnostics.json
```

Reuse or evolve M011 asset inspection/perception; do not duplicate decoding, duplicate grouping, or proposal engines.

Commands:

```bash
agentic2d asset source profile build <source-id> --output <directory>
agentic2d asset source profile inspect <source-id> --output <directory>
```

## Focus Area 3 — Reusable annotations and stale-data policy

Authority: `docs/specs/reusable-asset-annotation-and-cleanup-contract.md`.

Allowed actions:

```text
exclude-file
correct-grid
reject-grid
split-region-group
merge-region-group
reject-animation-group
correct-animation-order
exclude-audio
correct-source-scope
associate-license-observation
note
```

Commands:

```bash
agentic2d asset source annotation list <source-id> --output <directory>
agentic2d asset source annotation apply <source-id> --decisions <file> --output <directory>
agentic2d asset source annotation remove <source-id> <annotation-id> --output <directory>
```

The initial interface may use explicit IDs/files. M029 adds numbered UX.

## Focus Area 4 — Campaigns, batches, and proposal authority

Authority: `docs/specs/asset-campaign-and-batch-contract.md`.

Schemas:

```text
agentic2d.asset-campaign.v1
agentic2d.asset-campaign-status.v1
agentic2d.asset-batch.v1
agentic2d.asset-candidate-group.v1
agentic2d.asset-campaign-proposal.v1
agentic2d.asset-unresolved-decision.v1
```

Outputs:

```text
campaign.json
campaign-status.json
proposal-summary.json
candidate-groups.json
unresolved-decisions.json
dependency-impact.json
```

Required separation:

```text
shared profile: observed facts and reusable annotations
campaign: relevance and game-specific proposals
future approved asset definition: authoritative presentation meaning
future gameplay binding: explicit behavior and consequences
```

Presentation proposals must not silently apply collision, walkability, damage, interaction, collection, or progression behavior.

Commands:

```bash
agentic2d asset campaign validate <campaign-id-or-path> --output <directory>
agentic2d asset campaign status <campaign-id-or-path> --output <directory>
agentic2d asset campaign propose <campaign-id-or-path> --output <directory>
agentic2d asset batch inventory <batch-id-or-path> --output <directory>
agentic2d asset batch propose <batch-id-or-path> --output <directory>
agentic2d asset batch validate <batch-id-or-path> --output <directory>
agentic2d asset batch review-pack <batch-id-or-path> --output <directory>
```

Proof: two campaigns reference one profile revision, select/rank/group at least one candidate differently, remain independent, and do not mutate shared profile authority.

## Focus Area 5 — Headless evidence and M029 readiness

Authority: `docs/artifacts/asset-discovery-and-campaign-review-pack-artifact-contract.md`.

Pack:

```text
asset-review-pack/
├─ manifest.json
├─ index.md
├─ source/
├─ discovery/
├─ campaign/
├─ images/
├─ audio/
└─ diagnostics/
```

Required visual evidence: source preview, indexed contact sheet, candidate-region overlay, duplicate groups, animation candidates, uncertainty overlay.

Required audio evidence when applicable: audio properties, waveform preview, bounded raw preview WAV, comparison summary.

Also produce an M029-readiness report describing supported sources, commands, annotation behavior, unresolved UX decisions, preview-host inputs, playback/capture needs, promotion inputs, and known scale limits.

## Implementation constraints

- Shared asset home is authoring infrastructure, not runtime authority.
- Raw assets/profiles are not source controlled by default.
- Games remain independently buildable after future promotion.
- M028 does not promote assets.
- Generated metadata is disposable; retained annotations require explicit removal.
- Campaign interpretation never mutates shared discovery.
- Absolute local paths never become committed semantics.
- Source changes never silently reuse stale profiles.
- Audit M011 asset capabilities before reuse. Reuse validated infrastructure and contracts. Generalize or replace fixture-shaped implementations. Preserve compatibility only where it has a concrete consumer. Do not create a parallel subsystem merely to avoid correcting M011.
- Actual PNG/WAV processing paths are reused where available.
- Headless operation is mandatory; no network/model service is required.
- M028 review gates M028 only and becomes historical after completion.

## Required authority documents

Read only:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/specs/product-cli-contract.md
docs/specs/asset-metadata-contract.md
docs/specs/asset-review-contract.md
docs/specs/shared-asset-home-and-source-registry-contract.md
docs/specs/reusable-asset-discovery-profile-contract.md
docs/specs/reusable-asset-annotation-and-cleanup-contract.md
docs/specs/asset-campaign-and-batch-contract.md
docs/artifacts/product-cli-result-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
docs/artifacts/asset-authoring-artifact-contract.md
docs/artifacts/asset-curation-workbench-artifact-contract.md
docs/artifacts/asset-discovery-and-campaign-review-pack-artifact-contract.md
docs/engineering/product-cli.md
docs/engineering/command-contract.md
docs/engineering/validation-tiers.md
docs/engineering/constrained-validation-execution.md
docs/engineering/human-review-workflow.md
docs/decisions/ADR-0040-shared-local-asset-home-is-authoring-infrastructure-not-game-authority.md
docs/milestones/MILESTONE-011-asset-perception-review-application-and-static-curation-workbench.md
docs/milestones/MILESTONE-025-reference-game-workspace-procedural-presentation-synthesized-sound-and-extension-discovery.md
docs/milestones/MILESTONE-026-consumer-authoring-diagnostics-generated-sound-linkage-scaled-performance-workloads-and-autonomous-tic-tac-toe-consumer-validation.md
docs/milestones/MILESTONE-027-geometry-and-generated-sound-contract-stabilization-review-evidence-packs-and-guide-system-v060-migration.md
docs/milestones/MILESTONE-028-shared-asset-library-reusable-discovery-profiles-and-campaign-foundations.md
```

Read `.review/` only for M028 review. Do not read `.guide-sync/` or the external guide repository.

## Files or areas likely affected

```text
src/Agentic2D.Tools
src/Agentic2D.Engineering
existing asset inspection/perception implementation
existing PNG/WAV processing implementation
tests/unit/
tests/integration/
eng/
game/assets/ or bounded campaign fixtures
artifacts/assets/M028/
artifacts/validation/m028-smoke/
.review/pending/
.review/records/
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/product-cli.md
```

Use actual current ownership. Do not create empty parallel projects.

## Validation tiers and concrete commands

### Tier 1 — Asset home and registry

```bash
./eng/build.sh
./eng/test-filter.sh AssetHome
./eng/asset-home-smoke.sh
./eng/asset-source-registry-smoke.sh
```

Validate defaults/override, unavailable-source diagnostics, atomic writes, no path leakage, fingerprint changes, and failed-refresh recovery.

### Tier 1 — Discovery

```bash
./eng/test-filter.sh AssetDiscovery
./eng/asset-source-profile-smoke.sh
```

Validate grid image, irregular regions, animation proposal, WAV library, deterministic output, duplicate groups, license observations, and no game semantics.

### Tier 1 — Cleanup and annotations

```bash
./eng/test-filter.sh AssetAnnotation
./eng/asset-source-cleanup-smoke.sh
./eng/asset-source-annotation-smoke.sh
```

Validate generated cleanup, stale cleanup, annotation preservation/removal, incompatibility diagnostics, and rejection of game-specific semantics.

### Tier 1 — Campaigns and batches

```bash
./eng/test-filter.sh AssetCampaign
./eng/asset-campaign-smoke.sh
./eng/asset-batch-smoke.sh
```

Validate two campaigns reusing one profile differently, no profile mutation, bounded outputs, unresolved decisions, dependency preview, and no promotion.

### Tier 2 — Headless evidence

```bash
./eng/asset-discovery-review-pack-smoke.sh
./eng/review-pack-smoke.sh
./eng/product-validate.sh
./eng/cli-smoke.sh
```

### Tier 2 — Existing asset regression

```bash
./eng/asset-inspect-smoke.sh
./eng/asset-perception-smoke.sh
./eng/asset-review-smoke.sh
./eng/asset-curation-smoke.sh
./eng/content-validate.sh assets
./eng/check.sh
```

### Tier 5 — Human review

```bash
./eng/review-list.sh --milestone M028
./eng/review-show.sh review.m028.shared-asset-library-discovery-and-campaign-foundation
./eng/review-check.sh --milestone M028
```

Before approval, check fails only because M028 is unresolved.

## Validation execution mode

```text
focused provider validation: direct
aggregate M028 suite: resumable-sharded
M028 evidence/authority acceptance: blocking human review
```

Suite: `./eng/m028-smoke.sh`

Plan: `./eng/m028-smoke.sh --plan-json`

Shards:

```text
asset-home
source-registry
image-discovery
audio-discovery
annotations-cleanup
campaign-reuse
batch-proposals
headless-review-pack
m011-regression
documentation
human-review
integrated
```

Receipts: `artifacts/validation/m028-smoke/`

Fingerprint source registry, discovery/annotation/campaign schemas and implementation, bounded fixtures, review-pack producer, direct docs, and M028 review state. Exclude absolute asset-home paths, timestamps, session IDs, non-semantic encoding metadata, wall-clock duration, external guide state, and intentionally external raw bytes while retaining their fingerprints.

Only `./eng/m028-smoke.sh --verify` establishes aggregate success.

## Acceptance criteria

1. Linux default and environment override resolve correctly.
2. Asset home is not implicitly created inside a game repository.
3. Registry writes are atomic and source IDs are path-independent.
4. Asset home is not a runtime/export dependency.
5. Package/file fingerprints are deterministic; changed bytes change fingerprints.
6. Stale profiles are not silently current; failed refresh preserves prior valid profile.
7. Cleanup never removes raw bytes.
8. Grid, irregular-region, animation, audio, duplicate, and license discovery succeed deterministically.
9. Profiles contain facts/proposals, never approved gameplay semantics.
10. Generated metadata can be cleaned and rebuilt.
11. Retained annotations survive cleanup and require explicit removal.
12. Incompatible annotations diagnose rather than silently retarget.
13. Reusable annotations reject game-specific behavior.
14. Two campaigns reuse one profile differently without mutating it.
15. Batches are independently inventoryable, proposable, validatable, and review-packable.
16. Presentation proposals do not silently add gameplay behavior.
17. M028 creates no approved game asset definitions or runtime bindings.
18. Review-pack paths/hashes are safe and correct; visual/audio evidence exists and is bounded.
19. Evidence is inspectable without the live asset home.
20. M011 asset inspection, perception, review application, and static curation remain valid.
21. M029-readiness report exists and portable profile bundles remain deferred.
22. Review ID is `review.m028.shared-asset-library-discovery-and-campaign-foundation`, owned only by M028.
23. Reviewer confirms authority boundaries, cleanup safety, retained annotations, campaign separation, and readiness for M029.
24. Acceptable completion is `approved`; no implicit waiver.
25. `review-check --milestone M028` passes after approval.
26. M028 exposes list/plan/shard/verify/no-argument modes with atomic fingerprinted receipts.
27. Partial logs are not aggregate proof and final verifier passes.

## Direct documentation impact

Update only affected project truth:

```text
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
```

Do not perform broad unrelated synchronization.

## Deferred documentation synchronization hints

```text
.guide-sync/pending/2026-07-19-m028-shared-asset-library-and-discovery-sync.md
.guide-sync/pending/2026-07-19-m028-m029-m030-asset-curation-train-sync.md
```

Ordinary implementation agents do not read them.

## Human-review requirements

```text
applicability: blocking
classes: semantic, artifact-quality
canonical ID: review.m028.shared-asset-library-discovery-and-campaign-foundation
owning milestone: M028
request: .review/pending/review.m028.shared-asset-library-discovery-and-campaign-foundation.json
record: .review/records/review.m028.shared-asset-library-discovery-and-campaign-foundation.json
reviewer: repository user
acceptable completion: approved
waiver: none implicit
check: ./eng/review-check.sh --milestone M028
```

Evidence: asset-home inspection; registry/fingerprints; grid/irregular/animation/audio profiles; cleanup and annotation before/after; two-campaign reuse; headless review pack; M011 receipts; M029-readiness report; docs diff summary.

Future M029/M030 reviews are new reviews and do not reopen M028.

## Constrained-runtime handling

1. Run `./eng/m028-smoke.sh --plan-json`.
2. Run each non-review shard separately.
3. Stop on a real failure and inspect receipts.
4. Generate review evidence.
5. Run review list/show/check for M028.
6. When pending, report the canonical ID and stop without claiming completion.
7. After human decision, run `human-review` shard.
8. Run `./eng/m028-smoke.sh --verify` and report exact output.

Do not use backgrounding, detached processes, timeout inflation, or partial logs as proof.

## Recovery and rollback

Preserve raw bytes and the last valid registry/profile pointer. Generated profiles/previews/cache may be rebuilt. Retained annotations require explicit removal. Failed campaign generation must not mutate shared profiles or committed campaign source. Do not add a general metadata migration system merely to retain rebuildable data.

## Out-of-scope guide migration work

None. Target and latest guide-system versions are both `0.6.0`. Do not modify `.guide-profile.json`, copy guide documents, or require external guide access.
