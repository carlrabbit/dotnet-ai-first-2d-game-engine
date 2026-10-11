# Milestone 048 — Actual Candidate Preview and Human Curation Experience

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Execution profile | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Repository role | capability-provider |
| Profiles | artifact-first-agentic-authoring; runtime-tool; game-simulation |
| Maturity | implementation-ready; artifact-first |
| Validation | Tier 1–3 machine validation; resumable-sharded aggregate; active Windows platform epoch |
| Human review | blocking; visual/UX/audio; milestone-scoped |
| Execution prerequisite | M047 COMPLETE with current `m047-smoke --verify` |

M048 closes the preview and human-curation gap left deliberately by M047.

M047 established exact candidate/source/variant/decision/recipe/promotion authority. M048 does not redefine that authority. It makes the human workbench show and play the **same exact materialization subject** that an interactive approval decision will bind and that M047 promotion would later materialize.

## Goal

Make interactive asset curation truthful and reviewable:

```text
current canonical M047 candidate
+ operational variant/correction draft
        ↓
canonical materialization subject
        ↓
same M047 resolver + recipe planning/materializer
        ↓
disposable preview bundle
        ↓
actual engine rendering / animation / sound projection
        ↓
Raylib preview surface
        ↓
explicit human curation decision
        ↓
v2 decision whose candidate/variant/corrections derive
the same materialization-subject fingerprint
```

The exact candidate under review must be the media the human sees/hears.

A human must not be able to interactively approve candidate B while the preview displays or plays candidate A, an unrelated smoke fixture, the wrong variant, or stale pre-correction media.

## Primary Acceptance Question

> When a repository user curates an image, animation, or audio candidate, can the engine prove that the actual preview content, selected variant/corrections, committed v2 decision, and eventual M047 promotion input describe the same exact materialization subject — while the user receives a clear bounded visual/audio experience for making the subjective choice?

## Preconditions

Before M048 implementation begins:

```powershell
pwsh ./eng/suite.ps1 m047-smoke --verify
```

must pass against the current repository state.

If M047 is not current and passing, M048 is blocked by its explicit predecessor. Do not recreate candidate resolution, deterministic processing, or promotion locally to bypass M047.

Historical M029 milestone/review records remain immutable.

## Problems Being Corrected

Current M029/M047 repository truth still leaves these M048 problems:

1. the preview host renders fixed smoke animation/render/audio content and labels it with the active candidate;
2. the preview protocol does not bind displayed/played media to an M047 canonical candidate fingerprint;
3. workbench navigation can change candidates without a machine-proof chain from candidate to loaded preview content;
4. variant/correction decisions need a safe preview-before-commit path rather than immediately becoming durable authority;
5. preview restart/reconnect can preserve workbench state without proving that the reloaded preview is the same exact current subject;
6. approval-like interactive decisions are not guarded by a current matching preview acknowledgement;
7. group approval can otherwise become an unpreviewed human-approval shortcut;
8. image region, animation sequence, and audio trim/variant previews need modality-appropriate actual-engine presentation;
9. the generic M038 Review Workbench still renders placeholder review content and is hard-wired to M038 review IDs;
10. M048 requires milestone-owned subjective review without asking the human to verify hashes, schemas, determinism, or promotion correctness.

## Target State

### Authority layers

```text
M028
raw source + discovery observations + campaign proposals
        ↓
M047
canonical candidate + exact source/selection/variants
v2 decision + typed corrections
deterministic recipe/materializer
promotion authority
        ↓
M048
operational curation draft
materialization-subject fingerprint
disposable exact preview bundle
actual-engine audiovisual preview
interactive preview acknowledgement
        ↓
M047 v2 decision commit
```

M048 introduces no new approved-asset or gameplay authority.

### Process ownership

```text
Asset Workbench
  session / aliases / RDP-safe input
  operational curation draft
  decision intent
  preview acknowledgement tracking
          |
          | versioned local IPC
          v
Preview Host
  resolves/validates disposable preview bundle
  restartable playback/projection state
          |
          v
Actual engine presentation systems
  rendering
  animation
  sound projection
          |
          v
isolated Raylib preview surface
```

The preview host owns no durable curation decision.

The workbench owns no duplicate media resolver, image processor, animation interpreter, or audio processor.

## Canonical Materialization Subject

Authority: `docs/specs/actual-candidate-preview-and-curation-contract.md`.

M048 defines one canonical **materialization subject** for preview/decision comparison.

It contains:

```text
campaignId
candidateId
candidateFingerprint
selectedVariantId + selectedVariantFingerprint when any
ordered typed corrections
M047 recipe fingerprint
materializationSubjectFingerprint
```

The fingerprint is derived canonically from the same structured values used by M047 decision/recipe authority.

It excludes:

```text
session ID
alias generation
absolute asset-home path
preview process ID
window state
playback position
timestamps
temporary preview-bundle path
human-review record state
```

The v2 decision schema does not need a new persisted field solely for M048. A materialization-subject fingerprint must be independently derivable from the v2 decision's current candidate/variant/corrections plus its deterministic M047 recipe.

M048 validation compares derived values rather than trusting a duplicated stored success field.

## Operational Curation Draft

Interactive preview exploration is operational state, not durable decision authority.

A workbench session has or references one current curation draft for the active candidate:

```text
agentic2d.asset-curation-draft.v1
```

The draft contains at least:

```text
candidateId
candidateFingerprint
selectedVariantId/fingerprint when any
typed corrections
materializationSubjectFingerprint
preview status/acknowledged fingerprint
```

Rules:

- default draft is the candidate's default proposal with no additional corrections;
- selecting an alternative updates the draft and preview, not the durable decision log;
- editing a supported typed correction updates the draft and preview, not the durable decision log;
- navigating to another candidate changes the active draft subject explicitly;
- stale/unavailable candidate authority invalidates the draft's approval eligibility;
- draft state may be persisted with the workbench session for crash/restart usability, but it remains operational authoring state and is never promotion authority;
- partial text/input focus remains separate operational state;
- a draft may be discarded without altering durable v2 decision history.

Do not overload a final `choose-alternative` or `approve-with-corrections` decision merely to preview a choice.

## Preview Bundle

A preview is generated from a validated M047 candidate and the current curation draft.

Schema:

```text
agentic2d.asset-preview-subject.v1
agentic2d.asset-preview-bundle.v1
agentic2d.asset-preview-observation.v1
```

The bundle is disposable authoring infrastructure.

It contains or safely references the exact bounded media needed for preview:

```text
materialization subject
candidate/source provenance sufficient for diagnostics
base/raw candidate preview media
processed/current-draft preview media
exact media hashes
recipe fingerprint
modality metadata
backend-neutral projection inputs
```

Absolute machine paths may exist only in operational launch records outside semantic preview identity.

### Shared materializer rule

The processed preview derivative MUST use the same M047 candidate resolver, recipe semantics, and deterministic materializer used by promotion.

Do not maintain a second "preview crop", "preview trim", "preview frame order", or other independently implemented transform.

A draft that would be unsupported by M047 promotion is unsupported by preview approval and cannot be interactively approved.

Preview may materialize into session/cache space without creating or publishing a promoted generation.

## Actual-Engine Preview Requirement

The preview must use current engine presentation systems rather than unrelated fixtures.

### Image candidate

For `image-file` and `image-region` subjects:

- source context may be shown when useful;
- the exact candidate region is visibly distinguishable;
- processed/current-draft output is rendered from the M047 materialized bytes;
- raw/base versus processed/current-draft comparison is available;
- crop bounds, alpha-trim result, scale result, and pivot/anchor overlay are shown when relevant;
- the visible output under judgment is the actual candidate subject, not a fixed smoke atlas entity.

### Animation candidate

For `animation-sequence` subjects:

- frame sequence is the canonical candidate/selected-variant sequence;
- draft frame-order correction changes actual playback order;
- rendered frames use the actual candidate image regions/materialized media;
- playback is deterministic from semantic preview time;
- play/pause/reset and bounded frame-step inspection are available;
- the preview uses the engine animation/sampling path before adapter rendering.

No hard-coded `animation-semantic-replay-smoke` or unrelated scenario may stand in for the active candidate.

### Audio candidate

For `audio-file` subjects:

- audio source is the exact candidate or selected variant;
- processed comparison uses the exact M047 audio-copy/sample-frame-trim materialization;
- playback is manual; audio never auto-plays;
- raw/base and processed/current-draft A/B controls are available;
- visible duration/trim information is sufficient to understand which interval is being heard;
- sound command/projection uses the current engine sound path before adapter playback;
- no unrelated fixed cue such as a smoke footstep may stand in for the active candidate.

No audio-generation, resampling, gain, channel conversion, or other M047-unsupported processing is added.

## Preview Host Protocol v2

Authority: replacement `docs/specs/asset-preview-host-ipc-contract.md`.

Current M048 preview protocol:

```text
agentic2d.asset-preview-ipc.hello.v2
agentic2d.asset-preview-ipc.request.v2
agentic2d.asset-preview-ipc.response.v2
agentic2d.asset-preview-ipc.event.v2
agentic2d.asset-preview-scene.v2
agentic2d.asset-preview-capture.v2
```

The v2 load/compare request identifies the exact materialization subject and preview bundle fingerprint.

A successful load acknowledgement includes the exact acknowledged materialization-subject fingerprint and loaded media/projection fingerprints.

The workbench considers a draft preview-current only after this acknowledgement.

### Compatibility

Preview IPC is local operational coupling between components updated together.

M048 does not promise compatibility with the historical v1 preview IPC/scene schemas.

V1 requests may be rejected with a stable diagnostic. Do not add a compatibility translation that can lose exact candidate/subject identity.

M029 workbench session and input v1 remain current unless a direct contradiction requires an additive operational reference. Prefer separate `asset-curation-draft.v1` state rather than redefining durable M029 session/input semantics.

## Interactive Decision Guard

M048 does not change M047 v2 decision semantics.

It changes the interactive workbench precondition for approval-like actions.

The interactive workbench may commit:

```text
accept-proposal
choose-alternative
approve-with-corrections
presentation-only approval
```

only when:

```text
current candidate resolves
AND current draft is valid
AND preview host has acknowledged that exact materializationSubjectFingerprint
AND current draft still derives the same fingerprint at commit time
AND required consequence response is satisfied
```

If any term differs, approval is blocked with a corrective diagnostic and no durable v2 decision is written.

The commit path derives the v2 decision fields from the acknowledged current draft; it does not re-interpret free text.

### Non-approval actions

These may remain available when a successful audiovisual preview cannot be produced, subject to current M047 semantics:

```text
reject
defer
mark-unused
request-another-proposal
mark-source-unsuitable
```

A malformed/unavailable candidate therefore does not trap the reviewer in an unusable session.

### Headless authority

M047's explicit headless v2-decision path remains valid machine/authoring authority.

M048 does not claim that every headless decision was human-preview-backed.

Only the interactive workbench path claims the preview acknowledgement guard.

## Group Approval Boundary

Interactive group approval MUST NOT imply that unpreviewed candidates were human-reviewed.

A workbench group approval is preview-backed only when every exact candidate fingerprint covered by the group has a current successful preview acknowledgement for the exact subject to be approved.

If this condition is not met:

- block interactive group approval; or
- route the user through the unpreviewed members.

M048 does not require new group-approval UX beyond preventing the unsafe shortcut.

Headless group decision semantics remain M047 authority and are not relabeled as human-preview evidence.

## Candidate Change, Staleness, and Restart

### Candidate/source change

If current M047 resolution changes candidate, selected variant, correction validity, recipe, or source bytes:

```text
old preview acknowledgement
→ invalid
→ approval disabled
→ rebuild/reload exact current preview required
```

Never keep an acknowledgement current by candidate ID alone.

### Preview-host restart

Workbench input, session state, curation draft, and durable decisions survive preview-host failure.

On reconnect:

1. re-resolve current canonical candidate;
2. re-derive current materialization subject;
3. regenerate or revalidate preview bundle;
4. load through preview protocol v2;
5. require a new matching acknowledgement.

The prior process acknowledgement is not reused.

### Malformed media

Malformed or unsupported preview content yields a bounded diagnostic scene/state and leaves navigation/non-approval decisions usable.

It never fabricates a preview-current acknowledgement.

## Workbench Interaction Requirements

Preserve the M029 RDP/touch-safe input contract.

Primary operations remain usable through visible mouse/touch controls and explicit submitted text/canonical commands.

M048 curation additions must provide clear visible state for:

```text
active candidate
media kind
source-relative descriptive context
selected variant
structured corrections
raw/base vs processed/current-draft mode
preview loading/current/stale/error state
decision eligibility
```

Do not ask the user to inspect SHA-256 strings to determine correctness.

Hashes are machine evidence.

The candidate ID may be shown as identity but its text is not semantic instruction.

## Milestone Human Review

M048 has blocking human review because preview quality/usability/audio adequacy are irreducibly perceptual.

Machine validation proves identity and correctness before human review.

The human is never asked to determine:

```text
candidate fingerprint equality
source hash equality
recipe determinism
schema validity
IPC identity binding
staleness logic
promotion equivalence
artifact completeness
```

### Canonical review IDs

```text
review.m048.01-image-candidate-curation
review.m048.02-animation-candidate-curation
review.m048.03-audio-candidate-curation
```

Requests are included under `.review/pending/`.

### Review workflow

Normal reviewer entry point:

```powershell
pwsh ./eng/review-run.ps1 --milestone M048
```

Bash equivalent:

```bash
./eng/review-run.sh --milestone M048
```

Completion gate:

```powershell
pwsh ./eng/review-check.ps1 --milestone M048
```

or:

```bash
./eng/review-check.sh --milestone M048
```

### Review experience registration

The current generic simple-review implementation is hard-coded to M038 and displays placeholder content.

M048 must generalize it only as far as needed for explicit registered simple review experiences.

Required architecture:

```text
v2 .review request
      ↓
explicit bounded simple-review experience registry
      ↓
review ID / milestone
      ↓
readiness probe + actual content surface
      ↓
existing Review Workbench shell
Restart / Reject / Accept / async persistence
```

Rules:

- no reflection/plugin discovery framework;
- no new durable review-session schema;
- no new review decision semantics;
- M038 review IDs remain registered and current behavior remains compatible;
- M048 registers the three canonical review IDs;
- the M048 content surface reuses the same actual candidate-preview rendering/control implementation as the asset workbench;
- placeholder `LIVE REVIEW CONTENT` is not acceptable evidence for M048;
- milestone `review-run` still opens one normal Review Workbench over the deterministic item set;
- per-item content interactions remain within the simple-review limit.

### Review item interaction bounds

`review.m048.01-image-candidate-curation`:
- starts at an exact image/region candidate with a clearly different current-draft alternative/correction available;
- at most two reviewed-content interactions, such as changing/toggling the processed selection;
- outer Review Workbench Accept/Reject remains the milestone decision.

`review.m048.02-animation-candidate-curation`:
- starts playing or ready to play the actual animation candidate;
- at most two reviewed-content interactions such as pause/step or variant/order comparison.

`review.m048.03-audio-candidate-curation`:
- audio does not auto-play;
- at most two reviewed-content interactions, normally Play Raw and Play Processed;
- actual audio-output capability is required for this subjective item.

### Human acceptance questions

Image:
> Is it immediately clear which exact image/region and processed variant/correction are under review, and is the visual comparison usable for a curation decision?

Animation:
> Does the actual candidate animation and its selected frame-order/variant preview play clearly enough to judge the intended presentation?

Audio:
> Is manual raw/processed playback of the actual audio candidate clear and usable enough to judge the selected audio/trim result?

### Reviewer and decisions

Reviewer role:

```text
repository user
```

Level:

```text
blocking
```

Acceptable completion decision:

```text
approved
```

Reject records:

```text
changes-requested
```

Waiver policy:

```text
No implicit waiver. These three items are the perceptual capability M048 exists to establish.
```

## Human Review Readiness

Long machine validation is never run inside the graphical review workbench.

After `m048-smoke --verify`, a fast readiness check must establish for each open M048 review:

- request is current and simple-review eligible;
- explicit experience provider is registered;
- deterministic review fixture/bundle exists;
- fixture subject has passed machine identity/binding checks;
- active-platform graphics prerequisite is current for image/animation;
- audio review can reach actual audio playback controls;
- for the audio item, a usable audio output device is available at review time.

If actual audio output is unavailable, machine validation may still pass, but the audio human review is not review-ready. The milestone remains awaiting human review rather than fabricating an audio-quality judgment.

## Review Fixture

Human milestone review uses bounded deterministic repository-owned/generated fixture content, not an arbitrary user's shared asset library.

The fixture must include at least:

```text
one image/region candidate with visually distinguishable raw/processed state
one animation sequence with an observable order/variant distinction
one WAV/PCM audio candidate with an audible raw/processed trim or variant distinction
```

It is resolved through the same M047 candidate/recipe path as ordinary workbench content.

Review fixture content is not promoted game/runtime authority.

## Evidence

Authority: `docs/artifacts/asset-candidate-preview-and-curation-evidence-contract.md`.

Machine evidence proves:

```text
candidate/source identity
materialization-subject fingerprint
preview bundle/media hashes
engine projection inputs/results
preview-host acknowledgement
interactive decision subject
derived decision materialization-subject fingerprint
promotion-plan-derived materialization subject
staleness/restart behavior
actual review experience readiness
```

Producer-authored `sameCandidate=true`, `previewCurrent=true`, or similar booleans are not proof without independent comparison.

Human evidence is the actual launchable M048 review experience.

Screenshots may support visual diagnostics but are not a substitute for the launchable review item, especially for interaction/audio.

## Scope

M048 includes:

- canonical operational curation draft;
- materialization-subject derivation shared across preview/decision comparison;
- disposable exact preview bundle generation through M047;
- preview protocol/scene v2;
- actual image/region candidate preview;
- actual animation candidate preview;
- actual audio candidate playback/comparison;
- raw/base versus processed/current-draft comparison;
- supported variant/correction preview before durable decision;
- interactive approval guard against stale/mismatched preview;
- preview-backed group-approval protection;
- preview restart/reconnect/staleness correctness;
- current M029 RDP/mouse/touch workbench compatibility;
- bounded simple-review experience registry generalization;
- three M048 blocking subjective review items;
- independent machine evidence and resumable validation;
- focused direct documentation required by these contracts.

## Non-goals

M048 does not implement:

- new M047 candidate/decision/promotion schemas;
- new deterministic processing operations beyond M047;
- runtime/renderer/game consumption of promoted generations — M049;
- real dependency-aware affected rebuild — M049;
- packaging/export/release integration;
- gameplay bindings or gameplay semantics;
- raw asset creation;
- generative image/audio editing;
- audio gain/resampling/channel conversion;
- video/media formats beyond current PNG/WAV baseline;
- dynamic remote/multi-user asset service;
- replacement of M029 workbench session/alias/input architecture;
- replacement of repository-local human-review authority;
- a generic plugin framework for review experiences;
- broad documentation synchronization;
- Linux graphics catch-up during the active Windows epoch;
- TBPs or issue templates.

## Compatibility

### Preserved

```text
M047 canonical candidate semantics
M047 asset-review-decision.v2
M047 processing recipe/promotion authority
M029 workbench session v1
M029 workbench input v1
repository review request/record v2
M038 review records and completed historical reviews
```

### Replaced/advanced

Current asset preview operational schemas advance from v1 to v2 as specified by the preview IPC contract.

No v1 preview IPC compatibility is promised.

The operational curation draft is new and non-authoritative.

### No release compatibility promise

These asset-authoring/preview interfaces remain unreleased development tooling. M048 prioritizes truthful current authority over compatibility with historical preview fixtures.

## Required Project Authority

Read after `AGENTS.md` and this milestone:

1. `docs/milestones/MILESTONE-047-canonical-asset-candidate-decision-and-promotion-authority.md`
2. `docs/specs/canonical-asset-candidate-contract.md`
3. `docs/specs/asset-workbench-decision-and-consequence-contract.md`
4. `docs/specs/approved-asset-and-deterministic-promotion-contract.md`
5. `docs/specs/actual-candidate-preview-and-curation-contract.md`
6. `docs/specs/asset-workbench-session-and-alias-contract.md`
7. `docs/specs/asset-workbench-input-contract.md`
8. `docs/specs/asset-preview-host-ipc-contract.md`
9. `docs/architecture/asset-workbench-and-preview-host-architecture.md`
10. `docs/decisions/ADR-0041-asset-preview-host-is-a-separate-restartable-client-around-engine-systems.md`
11. `docs/decisions/ADR-0060-actual-candidate-preview-shares-m047-materialization-subject.md`
12. `docs/HUMAN-REVIEW.md`
13. `docs/specs/simple-human-review-workbench-contract.md`
14. `docs/engineering/human-review-workflow.md`
15. `docs/artifacts/asset-candidate-preview-and-curation-evidence-contract.md`
16. `docs/engineering/command-contract.md`
17. `docs/engineering/validation-tiers.md`
18. `eng/platform-verification.json`

Inspect live M047/M029 implementation, Raylib asset-preview/workbench code, review engineering infrastructure, and relevant tests as necessary.

Do not require ordinary implementation to read:

```text
.guide-profile.json
.guide-sync/
external guide repository
prompt templates
planning conversation
docs/research/ as authority
```

## Outcome Obligations

The implementation-owned execution ledger must cover these outcome areas; the milestone does not prescribe concrete work packages.

### A. Exact preview identity

For image, animation, and audio:

- canonical candidate fingerprint independently matches preview subject;
- selected variant/corrections independently match preview subject;
- processed preview hash equals the bytes produced by the M047 materializer for that subject;
- preview-host acknowledgement names that exact subject;
- a different candidate/variant cannot satisfy the comparison.

### B. Actual engine presentation

- image preview is actual candidate media through current rendering path;
- animation preview is actual candidate sequence through current animation/rendering path;
- audio preview is actual candidate media through current sound path;
- fixed smoke media cannot satisfy the acceptance case;
- no duplicate processing implementation exists solely for preview.

### C. Interactive decision binding

- preview exploration changes only operational draft state;
- approval-like interactive action requires exact current acknowledgement;
- durable v2 decision derives from acknowledged draft;
- stale/mismatched acknowledgement blocks decision;
- rejection/non-approval path remains usable for malformed/unavailable media;
- unsafe unpreviewed interactive group approval is prevented.

### D. Recovery and staleness

- preview-host restart requires fresh re-resolution/bundle/acknowledgement;
- workbench input/draft/decisions survive appropriate restart boundaries;
- changed source/candidate/variant/correction invalidates old acknowledgement;
- malformed preview leaves session recoverable.

### E. Curation usability surfaces

- active candidate, variant, corrections, preview status and decision eligibility are visibly distinguishable;
- image raw/processed and relevant overlays are available;
- animation playback/order inspection is available;
- audio manual A/B and trim/duration context are available;
- RDP/mouse/touch-safe input remains usable.

### F. Milestone human review infrastructure

- simple review runner is no longer M038-only internally;
- explicit M038 + M048 registered experiences work without new durable review schema;
- M048 review content is the actual candidate-preview implementation, not placeholder content;
- all three pending review requests are review-ready after machine prerequisites, subject to actual audio-output availability;
- M038 review mechanics remain regression-compatible.

### G. Evidence integrity

- structural machine conclusions derive from observed fingerprints/hashes/requests/responses/decisions;
- human review is limited to subjective visual/UX/audio questions;
- screenshots or JSON do not substitute for the launchable human experience.

## Validation

Execution mode:

```text
resumable-sharded
```

Machine receipt root:

```text
artifacts/validation/m048-smoke/
```

Domain evidence root:

```text
artifacts/assets/M048/
```

### Precondition

```powershell
pwsh ./eng/suite.ps1 m047-smoke --verify
```

### Plan

```powershell
pwsh ./eng/suite.ps1 m048-smoke --plan-json
```

### Required shards

```powershell
pwsh ./eng/suite.ps1 m048-smoke --shard m047-prerequisite-and-authority-regression
pwsh ./eng/suite.ps1 m048-smoke --shard preview-subject-and-bundle
pwsh ./eng/suite.ps1 m048-smoke --shard image-candidate-preview
pwsh ./eng/suite.ps1 m048-smoke --shard animation-candidate-preview
pwsh ./eng/suite.ps1 m048-smoke --shard audio-candidate-preview
pwsh ./eng/suite.ps1 m048-smoke --shard variant-correction-decision-binding
pwsh ./eng/suite.ps1 m048-smoke --shard preview-staleness-and-recovery
pwsh ./eng/suite.ps1 m048-smoke --shard workbench-input-and-group-preview-guard
pwsh ./eng/suite.ps1 m048-smoke --shard review-experience-registry-and-readiness
pwsh ./eng/suite.ps1 m048-smoke --shard active-platform-graphical-preview
pwsh ./eng/suite.ps1 m048-smoke --shard evidence-integrity
pwsh ./eng/suite.ps1 m048-smoke --shard predecessor-regression

pwsh ./eng/suite.ps1 m048-smoke --verify
```

Bash launchers expose equivalent semantics on a Linux active epoch; M048 current execution uses the active Windows/PowerShell epoch.

### Aggregate authority

Only:

```powershell
pwsh ./eng/suite.ps1 m048-smoke --verify
```

over current fingerprinted receipts establishes aggregate machine success.

Partial shard output, generated screenshots, existing artifacts, or successful child processes are not aggregate success.

### Standard gate

After aggregate machine verification:

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

### Shard contracts

`m047-prerequisite-and-authority-regression`:
- verifies current M047 aggregate authority and candidate/decision/materializer invariants needed by M048;
- M048 does not silently replace predecessor semantics.

`preview-subject-and-bundle`:
- derives materialization subject from canonical candidate/draft;
- validates bundle hashes/references/path independence;
- proves a deliberately different candidate/variant does not compare equal.

`image-candidate-preview`:
- exact image/region source and processed M047 derivative;
- source-context/selection/processed structural evidence;
- pivot/crop/trim/scale overlays where fixture-relevant;
- no fixed smoke substitute.

`animation-candidate-preview`:
- exact ordered candidate frames;
- selected order/variant affects actual playback projection;
- deterministic playback/step evidence;
- no fixed smoke animation substitute.

`audio-candidate-preview`:
- exact candidate/variant WAV;
- exact M047 processed trim/copy bytes;
- manual raw/processed command path;
- no auto-play;
- safe no-device machine behavior remains diagnostic, not subjective success.

`variant-correction-decision-binding`:
- operational draft before decision;
- approval guard requires matching acknowledgement;
- v2 decision independently derives same materialization subject;
- M047 promotion plan independently derives same subject/recipe;
- mismatch/stale preview rejects without durable approval.

`preview-staleness-and-recovery`:
- candidate/source/variant/correction changes invalidate acknowledgement;
- host restart requires fresh acknowledgement;
- malformed candidate does not terminate workbench;
- non-approval actions remain available.

`workbench-input-and-group-preview-guard`:
- RDP text, mouse/touch and headless canonical action equivalence remains;
- operational preview selections do not accidentally become decisions;
- interactive group approval cannot cover unpreviewed candidate fingerprints.

`review-experience-registry-and-readiness`:
- v2 review schema unchanged;
- M038 experiences remain registered;
- all three M048 experiences registered;
- actual M048 content providers replace placeholder content;
- request/evidence/readiness metadata are valid;
- long validation is not invoked inside review UI.

`active-platform-graphical-preview`:
- launches bounded Windows Raylib review/preview fixture;
- exercises actual image/animation content surface;
- proves graphics process actually ran and capture belongs to M048 fixture;
- does not claim audio quality from a screenshot.

`evidence-integrity`:
- independent comparer derives identity/binding/staleness conclusions;
- no producer-authored equality boolean or artifact existence can establish success.

`predecessor-regression`:
- current M047 verifier;
- focused M029 session/input/preview-recovery behaviors not contradicted by M048;
- review-workbench M038 regression;
- relevant rendering/animation/sound validation remains passing.

## Fingerprint and Receipt Scope

The engineering suite must fingerprint current repository implementation/authority and every declared fixture/input used by a shard.

A passing receipt becomes stale when relevant source, current authority, review request, fixture, or shard contract changes.

Human review records do not participate in machine aggregate success.

Completed M048 human review becomes immutable historical evidence after milestone completion.

## Direct Documentation Impact

Planning authority created/replaced by this package:

```text
docs/milestones/MILESTONE-048-actual-candidate-preview-and-human-curation-experience.md
docs/specs/actual-candidate-preview-and-curation-contract.md
docs/specs/asset-preview-host-ipc-contract.md
docs/architecture/asset-workbench-and-preview-host-architecture.md
docs/decisions/ADR-0041-asset-preview-host-is-a-separate-restartable-client-around-engine-systems.md
docs/decisions/ADR-0060-actual-candidate-preview-shares-m047-materialization-subject.md
docs/artifacts/asset-candidate-preview-and-curation-evidence-contract.md
.review/pending/review.m048.01-image-candidate-curation.json
.review/pending/review.m048.02-animation-candidate-curation.json
.review/pending/review.m048.03-audio-candidate-curation.json
```

Index/terminology merge instructions are in `M048-APPLICATION-INSTRUCTIONS.md`.

## Deferred Documentation Synchronization

The package includes:

```text
.guide-sync/pending/2026-08-28-m048-actual-candidate-preview-curation-sync.md
```

It is for a later documentation-synchronization task.

Ordinary M048 implementation must not read it.

## Constrained Runtime / Platform Requirements

Machine semantics are platform-neutral.

Current active development epoch is Windows.

M048 graphical human review requires:

- active-platform Raylib/native graphics capability;
- pointer/keyboard or RDP/touch interaction adequate for the workbench;
- for the audio review item, an actual usable audio output device.

Linux graphics/audio catch-up is not part of M048 while Linux is the inactive epoch.

No background/detached validation process may be used as aggregate proof. Preview-host child processes used by the implemented product/workbench architecture are permitted; their completion/identity evidence remains bounded and explicit.

## Persistent Execution Tractability

M048 is expected to touch asset authoring, preview IPC, Raylib preview presentation, engineering review registration, tests, and validation.

The implementation phase owns decomposition and repository-local execution state under the canonical methodology.

Planning does not pre-author `.execution/M048.md`.

The ready contract is intentionally organized by outcome obligations so the implementation agent can map each obligation to bounded work packages and evidence, reconcile coverage during execution, and freshly audit milestone completion without conversational memory.

## Completion Audit

Machine tests passing alone are not completion.

Before `COMPLETE`, freshly reconcile milestone obligations, implementation-owned execution ledger, live repository, generated evidence, and human review state.

Confirm at least:

1. M047 is current and passing.
2. exact image candidate/processed preview identity is mechanically proven.
3. exact animation candidate/frame-order preview identity is mechanically proven.
4. exact audio candidate/processed preview identity is mechanically proven.
5. preview transforms reuse M047 materialization semantics.
6. fixed smoke audiovisual content cannot satisfy active-candidate preview proof.
7. operational draft changes do not become durable decisions until explicit decision commit.
8. approval-like interactive decision requires current matching preview acknowledgement.
9. durable v2 decision derives the acknowledged current draft subject.
10. promotion-plan-derived subject matches decision/preview subject for the proof fixture.
11. stale candidate/source/variant/correction invalidates prior acknowledgement.
12. host restart requires fresh exact acknowledgement.
13. malformed/unavailable preview remains recoverable and permits non-approval actions.
14. interactive group approval cannot silently cover unpreviewed candidates.
15. M029 RDP/mouse/touch-safe input remains usable.
16. preview protocol is current v2 and v1 loss-of-identity compatibility is not silently retained.
17. review engineering supports explicit M038 and M048 experience registration without a new durable review framework.
18. all three M048 review requests have real purpose-built launchable experiences.
19. human review questions contain no machine-verifiable acceptance.
20. `m048-smoke --verify` passes current receipts.
21. build/test/format/check pass.
22. blocking human review passes:
    ```powershell
    pwsh ./eng/review-check.ps1 --milestone M048
    ```
23. historical M029/M038 completed records remain untouched.
24. no M049 runtime-consumption/affected-rebuild work was pulled into M048.
25. direct current docs are consistent with implemented M048 truth.

## Human Review Completion State

After machine implementation/validation is complete:

- if all three M048 review items are approved, completion may become `COMPLETE`;
- if required human review has not yet occurred, terminate `AWAITING HUMAN REVIEW`;
- if a reviewer records `changes-requested`, the implementation remains incomplete; agent-resolvable corrections must be made and machine readiness restored before presenting the review again;
- lack of an audio output device is an unavailable human-review environment, not permission to auto-approve audio quality.

## Escalation Boundary

Return to planning only if correct implementation requires a new material decision about:

- changing M047 candidate/decision/recipe/promotion semantics;
- adding new processing operations;
- changing the meaning of interactive human approval;
- allowing approval without exact preview acknowledgement;
- changing group-approval human-review semantics;
- introducing a new durable review schema/session;
- replacing the separate preview-host process model;
- adding a generic review plugin framework;
- adding new media formats/codecs;
- moving M049 runtime-consumption or dependency-rebuild scope into M048;
- changing release/public compatibility commitments;
- weakening the three blocking subjective review requirements.

Do not escalate for local types, APIs, IPC transport mechanics, temporary file layout, UI widget implementation, adapter rendering mechanics, test organization, fixture selection, execution work-package decomposition, or diagnostic wording where the above contract remains true.

## Baseline-Executability Audit

M048 is `ready`.

Planning has settled:

- architecture: M047 subject → shared materializer → disposable bundle → actual engine presentation → interactive preview guard;
- semantics: operational draft, exact acknowledgement, decision commit and group-approval rules;
- compatibility: M047 decision/promotion preserved; session/input v1 preserved; preview IPC advances to v2 without compatibility promise;
- scope: preview/curation only; M049 consumption/rebuild excluded;
- machine acceptance: exact identity, actual presentation paths, restart/staleness, decision binding and evidence integrity;
- human acceptance: three specific blocking perceptual questions;
- review mechanics: existing v2 review state and generic workbench shell with bounded explicit experience registry;
- validation: resumable 12-shard suite plus standard gate;
- platform requirements: active Windows graphics; actual audio device only for subjective audio review;
- execution tractability: outcome obligations can be mapped to implementation-owned persistent work packages/ledger.

Remaining choices are implementation mechanics.

No stronger implementation model is required to make an unresolved architecture, semantic, compatibility, scope, acceptance, validation, or review decision.

## Terminal Outcomes

Implementation terminates exactly as one of:

```text
Milestone status: COMPLETE
```

```text
Milestone status: AWAITING HUMAN REVIEW
```

```text
Milestone status: BLOCKED
```

`COMPLETE` requires both current machine completion and approved M048 blocking reviews.
