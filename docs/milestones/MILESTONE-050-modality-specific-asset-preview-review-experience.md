# Milestone 050 — Modality-Specific Asset Preview Review Experience

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Execution profile | ai-executed-broad |
| Baseline implementation model | GPT-5.6 Luna |
| Repository role | capability-provider |
| Profiles | artifact-first-agentic-authoring; runtime-tool; game-simulation |
| Maturity | implementation-ready; artifact-first |
| Validation | resumable-sharded; active Windows platform epoch |
| Human review | none owned by M050; existing M048 reviews remain the subjective gate |
| Execution prerequisite | M048 machine implementation current; `m048-smoke --verify` passing or only stale because of this correction |

M050 is a corrective milestone for the M048 human-review presentation layer.

M048 established the exact preview subject, real candidate preview process, review-experience registration, animation/audio behavior, and milestone-owned human-review questions. Human use of the implemented review experience exposed a remaining UX defect:

> the asset preview executable behaves as one union-of-all-controls screen, so image, animation, and audio reviews show controls that are irrelevant or non-functional for the current subject, and the reviewer cannot immediately determine what exactly is being judged.

M050 corrects that defect without changing M047/M048 candidate, decision, materialization, preview-subject, IPC, or review-decision authority.

## Goal

Make one generic asset-preview application present a **modality-specific review surface** whose visible controls and explanatory context match the exact current candidate.

The reviewer should immediately understand:

```text
what kind of asset is under review
which candidate/variant/correction is being judged
what the raw/base side means
what the processed/current-draft side means
what subjective question they are expected to answer
which controls are relevant to this modality
```

Irrelevant controls must not be shown.

## Primary Acceptance Question

> For each M048 image, animation, and audio review item, does the same generic asset-preview executable render a clearly identified modality-specific surface that exposes only meaningful controls for that exact review subject and gives the reviewer enough context to know what is being judged?

## Preconditions

M050 corrects the review surface after M048 implementation.

Before implementation, establish current M048 machine state:

```powershell
pwsh ./eng/suite.ps1 m048-smoke --verify
```

If M048 aggregate validation is stale solely because M050 changes preview/review source files, that is expected during implementation.

M050 does not require the three M048 human reviews to be approved before implementation. They are intentionally still pending because the experience being corrected is what those reviews judge.

## Problems Being Corrected

Current M048 review use revealed:

1. image, animation, and audio preview screens expose the same union of controls;
2. animation controls appear on audio/image screens even when they have no meaning;
3. audio controls appear on visual screens even when they have no meaning;
4. image filtering/overlay/source controls appear on audio screens;
5. control presence can suggest functionality that the current subject does not support;
6. the current screen emphasizes generic "ASSET PREVIEW" chrome instead of the actual review subject;
7. the reviewer is not told plainly what candidate, variant/correction, or raw-vs-processed distinction is being judged;
8. the subjective review question is separated from the preview context rather than being visible or directly represented in the preview experience;
9. one generic implementation surface was incorrectly interpreted as one generic visible control set.

## Architectural Decision

Authority: `docs/decisions/ADR-0061-asset-preview-application-is-generic-but-review-surfaces-are-modality-specific.md`.

Keep:

```text
one asset-preview executable
one Raylib/native lifecycle
one bundle loader
one exact materialization-subject resolver
one review-experience launch mechanism
shared layout/control primitives where useful
```

But render:

```text
mediaKind = image
    -> image review surface

mediaKind = animation
    -> animation review surface

mediaKind = audio
    -> audio review surface
```

The dispatch key is structured bundle/scene `mediaKind`.

Do not infer modality from:

```text
review ID text
candidate ID text
filename suffix heuristics
button availability
```

## Review Context Header

Authority: `docs/specs/asset-preview-review-surface-contract.md`.

Every review surface must present a bounded context block that makes the review subject understandable without inspecting JSON, hashes, or logs.

It includes human-readable equivalents of:

```text
Review type / modality
Candidate identity
Presentation role or purpose
Selected variant when any
Applied correction summary when any
Raw/base meaning
Processed/current-draft meaning
Subjective review question
```

Machine fingerprints may be available in diagnostics but are not primary reviewer-facing context.

The preview must not require the reviewer to infer the purpose from controls alone.

## Image Surface

The image surface shows only image-relevant controls.

Required capabilities when applicable to the current subject:

```text
Source / processed comparison
Region isolation or source context
Nearest/smooth presentation inspection when scaling is relevant
Overlay toggle when crop/bounds/pivot/anchor context exists
```

Rules:

- no audio playback controls;
- no animation play/pause/step/speed controls;
- do not show an overlay/filtering control when it has no effect for the current subject;
- raw/base and processed/current-draft labels must be semantically clear;
- the exact image/region under judgment remains central.

## Animation Surface

The animation surface shows only animation-relevant controls.

Required baseline:

```text
Play
Pause
Step
Reset
```

Optional only when meaningful:

```text
0.5x / 1x / 2x
frame-order or variant comparison
bounds/pivot overlay
```

Rules:

- controls must affect actual animation state;
- no audio playback controls;
- no generic image source/isolation/filtering controls unless they directly support the animation review question;
- current frame / frame count or equivalent bounded feedback should make playback state understandable;
- the surface identifies what ordering/variant/correction is being judged.

M050 does not redefine M048 animation semantics. It only presents them coherently.

## Audio Surface

The audio surface shows only audio-relevant controls.

Required baseline:

```text
Play Raw
Play Processed
Stop
```

Required context:

```text
raw/base duration
processed/current-draft duration
trim/variant summary when applicable
audio device readiness / playback diagnostic
```

Rules:

- no auto-play;
- no animation controls;
- no image filtering/source/overlay controls;
- a missing audio device is shown as a clear environment diagnostic, not as a pseudo-review state;
- when audio is available, raw and processed labels describe what difference is expected.

M050 does not add audio transforms beyond M047.

## Generic Preview Application Boundary

The preview application may share:

```text
window lifecycle
theme/high-contrast infrastructure
review-context header
candidate/bundle loading
diagnostic panel
capture support
button/layout helpers
native resource lifecycle
```

It must not share the visible union of modality-specific controls merely for implementation convenience.

A control that cannot affect the current review subject is absent, not disabled decorative chrome, unless disabled state itself communicates a material reason the reviewer needs to understand.

## Review Workbench Boundary

The generic M038 Review Workbench remains:

```text
question
optional Launch candidate preview
Reject
Accept
```

M050 does not change review decision semantics.

Launching the preview from the Review Workbench remains optional. Accept is not gated on launch state.

The child asset-preview window supplies the modality-specific review context and controls.

The existing three M048 review requests remain authoritative:

```text
review.m048.01-image-candidate-curation
review.m048.02-animation-candidate-curation
review.m048.03-audio-candidate-curation
```

M050 creates no duplicate human-review requests.

After M050 machine completion, those existing M048 reviews are the subjective acceptance path.

## Scope

M050 includes:

- structured media-kind dispatch in the actual asset-preview UI;
- modality-specific image review surface;
- modality-specific animation review surface;
- modality-specific audio review surface;
- explicit reviewer-facing context block;
- control relevance rules;
- meaningful labels for raw/base and processed/current-draft;
- modality-specific diagnostics;
- focused review fixture metadata needed to render context;
- validation proving irrelevant controls are absent;
- regression proof that actual candidate preview/bundle identity remains M048 authority;
- documentation required to make this presentation contract durable.

## Non-goals

M050 does not implement or change:

- M047 canonical candidate identity;
- M047 decision or promotion semantics;
- M047 processing operations;
- M048 materialization-subject semantics;
- M048 preview IPC identity semantics;
- M048 interactive decision acknowledgement rules;
- M048 review request/record semantics;
- the generic Review Workbench navigation/persistence model;
- new media formats;
- new image/audio/animation processing capabilities;
- runtime consumption of promoted assets;
- dependency-aware affected rebuild;
- gameplay binding;
- a plugin framework;
- broad visual redesign of unrelated Raylib clients.

Runtime promoted-content consumption and real affected rebuild remain the reserved M049 scope.

## Compatibility

Preserve:

```text
agentic2d.asset-preview-scene.v2
agentic2d.asset-preview-bundle.v1
M048 materializationSubjectFingerprint
M048 review IDs
.review request/record v2
review-run/review-check command semantics
M038 simple-review behavior
M029 session/input behavior
```

M050 may add optional presentation-only scene/bundle metadata when needed for human-readable review context, provided it does not alter canonical subject identity.

Do not encode presentation context by parsing opaque IDs.

## Required Project Authority

Read after `AGENTS.md` and this milestone:

1. `docs/milestones/MILESTONE-048-actual-candidate-preview-and-human-curation-experience.md`
2. `docs/specs/actual-candidate-preview-and-curation-contract.md`
3. `docs/specs/asset-preview-review-surface-contract.md`
4. `docs/specs/asset-preview-host-ipc-contract.md`
5. `docs/specs/simple-human-review-workbench-contract.md`
6. `docs/architecture/asset-workbench-and-preview-host-architecture.md`
7. `docs/decisions/ADR-0060-actual-candidate-preview-shares-m047-materialization-subject.md`
8. `docs/decisions/ADR-0061-asset-preview-application-is-generic-but-review-surfaces-are-modality-specific.md`
9. `docs/HUMAN-REVIEW.md`
10. `docs/engineering/human-review-workflow.md`
11. `docs/engineering/validation-tiers.md`
12. `eng/platform-verification.json`

Inspect live M048 Raylib preview implementation, review-experience registry, fixtures, source, and tests as required.

Ordinary implementation must not read `.guide-profile.json`, `.guide-sync/`, external guide material, or planning conversation.

## Outcome Obligations

### A. Structured modality dispatch

- preview surface is selected from structured `mediaKind`;
- deliberately misleading candidate/review IDs do not change selected surface;
- unsupported/unknown modality produces a diagnostic surface rather than an arbitrary control union.

### B. Image surface

- only image-relevant controls are present;
- no audio controls exist;
- no animation controls exist;
- context identifies candidate and image-specific comparison/correction meaning;
- controls shown have observable effect for the fixture.

### C. Animation surface

- only animation-relevant controls are present;
- no audio controls exist;
- generic image-only controls are absent unless explicitly meaningful;
- play/pause/step/reset affect actual animation;
- current playback/frame context is understandable;
- review context identifies the sequence/order/variant being judged.

### D. Audio surface

- only audio-relevant controls are present;
- no animation controls exist;
- no image filtering/overlay/source controls exist;
- raw/processed meanings and durations/correction summary are visible;
- audio readiness/failure is clearly environmental/operational;
- playback remains explicit/no-auto-play.

### E. Review context

For all three modalities, the surface provides enough visible context to answer:

```text
What am I reviewing?
What changed between raw/base and processed/current-draft?
What subjective question am I answering?
```

No hash comparison is required of the human.

### F. M048 regression

- exact candidate/bundle/materialization identity remains unchanged;
- existing review launch mapping remains exact;
- review decisions remain optional with respect to launching preview from the current workbench session;
- M048 existing human review IDs remain open/pending until the user decides them;
- no duplicate M050 review records are introduced.

## Validation

Execution mode:

```text
resumable-sharded
```

Receipt root:

```text
artifacts/validation/m050-smoke/
```

Evidence root:

```text
artifacts/assets/M050/
```

### Preconditions

```powershell
pwsh ./eng/suite.ps1 m048-smoke --verify
```

If this is stale only because M050 implementation edits fingerprinted preview files, continue with M050 shards and re-run M048 regression before completion.

### Plan

```powershell
pwsh ./eng/suite.ps1 m050-smoke --plan-json
```

### Required shards

```powershell
pwsh ./eng/suite.ps1 m050-smoke --shard modality-dispatch-and-context
pwsh ./eng/suite.ps1 m050-smoke --shard image-review-surface
pwsh ./eng/suite.ps1 m050-smoke --shard animation-review-surface
pwsh ./eng/suite.ps1 m050-smoke --shard audio-review-surface
pwsh ./eng/suite.ps1 m050-smoke --shard actual-platform-review-launch
pwsh ./eng/suite.ps1 m050-smoke --shard m048-review-regression
pwsh ./eng/suite.ps1 m050-smoke --shard evidence-integrity

pwsh ./eng/suite.ps1 m050-smoke --verify
```

### Aggregate rule

Only:

```powershell
pwsh ./eng/suite.ps1 m050-smoke --verify
```

over current fingerprinted receipts establishes M050 machine success.

### Standard gate

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

### Shard expectations

`modality-dispatch-and-context`:
- structured media-kind dispatch;
- misleading IDs cannot select modality;
- required context fields exist and are human-readable;
- unsupported modality yields diagnostic surface.

`image-review-surface`:
- observed image surface control inventory;
- image controls affect fixture state;
- animation/audio controls absent.

`animation-review-surface`:
- observed animation surface control inventory;
- play/pause/step/reset affect actual playback;
- audio controls absent;
- irrelevant image-only controls absent.

`audio-review-surface`:
- observed audio control inventory;
- raw/processed duration/correction context;
- manual playback path;
- animation/image-only controls absent;
- no-device diagnostic path remains truthful.

`actual-platform-review-launch`:
- active Windows graphical environment launches each of the three exact M048 review fixtures;
- capture/observation proves the selected modality-specific surface;
- human approval is not synthesized.

`m048-review-regression`:
- M048 exact subject/bundle/launch mapping remains valid;
- existing M048 review IDs remain unchanged;
- review-run semantics remain compatible;
- M048 affected machine shards/aggregate are current.

`evidence-integrity`:
- control presence/absence is derived from actual rendered surface model/observation, not producer-authored booleans alone;
- modality derives from structured media kind;
- fixture observations are tied to exact review ID and materialization subject.

## Human Review

M050 owns **no additional human review**.

Reason:

The subjective questions already belong to M048 and remain pending. M050 exists because those reviews exposed an agent-resolvable presentation defect before acceptance.

M050 machine validation establishes that the review experience is structurally fit to be judged.

After M050 is complete, the user returns to:

```powershell
pwsh ./eng/review-run.ps1 --milestone M048
```

and decides the existing three M048 review items.

M050 must not auto-approve, migrate, duplicate, or waive those reviews.

## Constrained Runtime / Platform Requirements

- machine semantics remain platform-neutral;
- actual-platform review-launch validation uses the active Windows graphical session;
- if the autonomous harness lacks an interactive desktop but the machine has one, the shard may be executed from the documented interactive Windows session and consumed through normal fingerprinted receipt machinery;
- do not fabricate graphical success from headless structural output;
- audio no-device behavior can be structurally validated, but subjective audio approval remains M048 human review.

## Documentation Impact

Planning authority added by this package:

```text
docs/milestones/MILESTONE-050-modality-specific-asset-preview-review-experience.md
docs/specs/asset-preview-review-surface-contract.md
docs/decisions/ADR-0061-asset-preview-application-is-generic-but-review-surfaces-are-modality-specific.md
```

Deferred synchronization hint:

```text
.guide-sync/pending/2026-09-30-m050-modality-specific-asset-preview-review-sync.md
```

## Persistent Execution Tractability

Implementation owns `.execution/M050.md`.

The ledger should map the seven validation shards and outcome obligations to bounded work packages.

Do not pre-author the ledger during planning.

## Completion Audit

Before `COMPLETE`, freshly reconcile milestone, execution ledger, live repository, evidence, and M048 review state.

Confirm at least:

1. one generic asset-preview executable remains;
2. visible surface dispatch uses structured media kind;
3. image surface contains no audio/animation controls;
4. animation surface contains no audio controls or irrelevant image-only controls;
5. audio surface contains no image/animation controls;
6. every visible control has a meaningful effect or diagnostic purpose for the current subject;
7. each surface visibly identifies what is being reviewed;
8. raw/base and processed/current-draft meanings are understandable;
9. subjective review question/context is visible or directly represented;
10. animation controls manipulate actual animation;
11. audio playback remains manual;
12. missing audio device is a clear diagnostic rather than misleading review content;
13. M048 exact candidate/materialization identity remains intact;
14. existing M048 review IDs/records remain unchanged;
15. M050 introduces no duplicate human review;
16. `m050-smoke --verify` passes;
17. affected M048 validation is current;
18. build/test/format/check pass;
19. no M049 runtime-consumption/affected-rebuild scope was pulled into M050.

## Escalation Boundary

Return to planning only if implementation requires changing:

- M047/M048 identity or decision semantics;
- preview IPC semantic identity;
- review decision semantics;
- the rule that preview launch is optional for human Accept/Reject;
- M048 human-review questions;
- media processing operations;
- generic Review Workbench authority;
- M049 runtime-consumption/affected-rebuild scope;
- supported media formats.

Do not escalate for concrete UI composition, layout dimensions, internal surface types, view-model mechanics, button implementation, scene metadata fields that are presentation-only, test organization, or fixture wording consistent with this contract.

## Baseline-Executability Audit

M050 is `ready`.

Resolved:

- architecture: generic executable, modality-specific visible surfaces;
- dispatch authority: structured media kind only;
- scope: review UX correction only;
- compatibility: M047/M048 semantic authority preserved;
- acceptance: explicit control-presence/absence and context obligations;
- validation: seven resumable shards plus M048 regression;
- human review: none duplicated; M048 remains owner of subjective acceptance;
- platform boundary: actual Windows launch required without conflating harness limitations with machine capability.

Remaining decisions are local UI implementation mechanics.

## Terminal Outcomes

M050 terminates as:

```text
Milestone status: COMPLETE
```

when all machine and documentation obligations pass.

If an external environment prevents required active-platform execution:

```text
Milestone status: BLOCKED
```

There is no normal `AWAITING HUMAN REVIEW` state for M050 because subjective review remains owned by M048.
