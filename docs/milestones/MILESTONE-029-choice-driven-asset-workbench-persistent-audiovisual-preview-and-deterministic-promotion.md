# Milestone 029 — Choice-Driven Asset Workbench, Persistent Audiovisual Preview, and Deterministic Promotion

## Goal

Turn the M028 asset home, discovery profiles, annotations, campaigns, and headless evidence into a complete provider-side curation workflow:

```text
M028 profile and campaign proposal
→ resumable workbench session
→ RDP-safe text-stream and mouse/touch navigation
→ persistent actual-engine audiovisual preview
→ consequence-aware human decisions
→ deterministic approved asset definitions and derivatives
→ M030-ready promotion workspace
```

M029 completes the provider-side workbench and promotion capability. It does not integrate promoted assets into Signal Passage or another game. Consumer bindings, procedural fallback, scenarios, save/load compatibility, Linux export closure, game-level performance, and integrated audiovisual review belong to M030.

Primary acceptance question:

> Can a human curate image and audio candidates through a reliable editable text-entry and mouse/touch workflow, use one persistent actual-engine preview, recover from process failures, and deterministically materialize approved project assets with complete provenance?

## Repository role and maturity assumptions

```text
repository role: capability-provider
bounded dogfood: M028 campaign fixtures and one promotion workspace
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation, UI/component
maturity: implementation-ready, artifact-first
execution mode: ai-executed-broad
guide-system current/latest: 0.6.0 / 0.6.0
```

No guide migration is part of M029.

## Execution mode

`ai-executed-broad`

Implement as seven coherent transformations:

1. persistent workbench sessions and ephemeral numbered aliases;
2. RDP-, virtual-keyboard-, mouse-, and touch-safe input;
3. guided navigation, search, comparison, and batch review;
4. separate restartable preview host using actual engine systems;
5. history-preserving decisions and consequence confirmation;
6. deterministic atomic promotion and affected rebuild;
7. resumable validation, blocking human review, and M030 handoff.

## Scope

### Workbench entry point

Provide a repository-consistent form of:

```bash
agentic2d asset workbench [--campaign <id-or-path>] [--session <id>] [--headless] [--output <directory>]
```

It resolves the M028 asset home, loads or creates one session, validates campaign/profile fingerprints, starts or reconnects to one preview host unless headless, reuses one normal preview window, writes decisions incrementally, and resumes after workbench or preview-host restart.

### Primary input model

The workbench must not depend on raw physical key-down events for ordinary interaction.

Primary input paths:

```text
editable text-entry control with explicit submission
mouse/touch selection of visible choices
canonical command dispatch shared by both paths
```

Optional physical keyboard shortcuts may exist, but they are accelerators only.

The text-entry control must:

- remain visibly editable before submission;
- buffer text rather than immediately acting on individual key presses;
- accept virtual-keyboard text, composition input where supported, and paste;
- support Backspace/Delete correction;
- support Enter submission;
- provide a clickable/touchable Submit button;
- provide Clear/Cancel behavior;
- preserve or explicitly clear partial input after focus loss according to documented operational policy;
- never silently submit on focus changes;
- retain invalid input for correction;
- display validation errors without recording a decision;
- remain usable when scan-code or raw key events are missing or unreliable.

Every visible numbered choice must also be mouse/touch selectable.

Required interaction equivalence:

```text
enter "2" and submit
click visible choice 2
invoke canonical command for choice 2
→ same validated workbench action
```

### Human-oriented navigation

Provide coherent commands equivalent to:

```bash
agentic2d asset workbench
agentic2d asset workbench resume <session-id>
agentic2d asset workbench status <session-id> --output <directory>
agentic2d asset workbench close <session-id> --output <directory>

agentic2d asset source list
agentic2d asset source open <number-or-id>
agentic2d asset campaign list
agentic2d asset campaign open <number-or-id>
agentic2d asset batch list
agentic2d asset batch open <number-or-id>
agentic2d asset candidate list
agentic2d asset candidate open <number-or-id>
agentic2d asset decision list
agentic2d asset decision show <number-or-id>
agentic2d asset approved list
agentic2d asset approved show <number-or-id>
agentic2d asset find <text>
agentic2d asset recent
```

The same bounded input field may accept:

```text
2
open 2
back
next
previous
find chest
recent
help
cancel
```

This is not a general shell or unrestricted natural-language interface.

Humans must not normally type canonical IDs, long paths, source rectangles, taxonomy paths, render layers, or provenance fields.

### Required decisions

```text
accept-proposal
choose-alternative
approve-with-corrections
reject
defer
mark-unused
split-group
merge-group
approve-group
review-exceptions
request-another-proposal
mark-source-unsuitable
```

Free text is bounded to reasons, notes, display names, and explicit creation of a new concept.

### Consequence-aware approval

Before approving collision, walkability, damage, interaction, collection, progression, render-layer, animation-event, or sound-cue implications, show consequences and require:

```text
confirm
change consequence
approve presentation only
cancel
```

Presentation identity and gameplay binding remain separate. M029 may emit future-binding suggestions, but it must not silently author gameplay behavior or runtime bindings.

### Persistent preview host

Provide one separate restartable preview-host process per active session, using a versioned local IPC contract and a Unix domain socket on Linux unless an existing local transport is more consistent.

The host must use actual engine:

- content loading;
- render projection;
- raylib rendering;
- animation evaluation;
- sound projection;
- audio adapter.

It owns temporary preview worlds, comparison layouts, playback state, and capture state. It does not own durable decisions or workbench input state.

Required visual functions:

- neutral and high-contrast backgrounds;
- source and isolated-region views;
- nearest-neighbor/smooth comparison where supported;
- pivot, bounds, grid, and padding overlays;
- side-by-side comparison;
- animation play, pause, step, and bounded speed presets;
- screenshot capture;
- malformed-candidate diagnostic scene.

Required audio functions:

- raw and processed preview;
- bounded playback;
- stop/replay;
- A/B selection;
- waveform and media properties;
- no auto-play;
- safe no-device behavior.

### Sessions, input state, and aliases

Session state stores canonical context, active batch/candidate, input-state reference, alias generation, preview endpoint, decision-log path, promotion-plan path, and recovery diagnostics.

Input state is operational and may contain:

- current text buffer;
- validation message;
- current menu/list generation;
- focus/composition status where supported;
- last submitted canonical command.

Partial input is never decision authority.

Numeric aliases are ephemeral and scoped to one successful list context. They never appear in decisions, approved definitions, manifests, receipts, committed content, or cross-session references.

Stale aliases fail safely and instruct the user to list again.

### Search and recent items

Local bounded search covers source names, campaign vocabulary, proposal text, evidence, approved display names, decision reasons, and media properties. It must not require a network service.

Recent items are operational convenience only.

### Durable decision records

Write:

```text
review-session.json
review-decisions.jsonl
review-summary.md
review-diagnostics.json
```

Each decision records canonical IDs, session/campaign/batch/candidate references, source/profile fingerprints, action, selected alternative, corrections, consequences shown and confirmed, reason, actor when available, sequence, superseded decision, status, and provenance.

Corrections and reversals preserve history. Preview failure, input-focus loss, or interrupted promotion must not lose accepted decisions.

### Approved assets and promotion

Approved authority families:

```text
approved image region
approved sprite
approved animation
approved audio
approved presentation role
processing recipe
provenance
future-binding suggestion
```

Provide commands equivalent to:

```bash
agentic2d asset batch apply-review <batch> <decision-log> --output <directory>
agentic2d asset batch promotion-plan <batch> --output <directory>
agentic2d asset batch promote <batch> --target <workspace> --output <directory>
agentic2d asset rebuild --affected <source-or-approved-id> --target <workspace> --output <directory>
agentic2d asset approved validate <workspace> --output <directory>
agentic2d asset approved inspect <approved-id> --workspace <workspace> --output <directory>
```

Promotion validates fingerprints and consequences, generates stable semantic IDs, creates derivatives/definitions/provenance in staging, validates staging, and atomically replaces the promoted set. Failure preserves the previous valid set.

Permitted bounded deterministic processing:

- image crop;
- padding trim/preserve;
- integer nearest-neighbor scale;
- pivot/anchor metadata;
- animation frame ordering;
- audio copy;
- deterministic trim;
- bounded gain;
- explicitly selected supported channel conversion.

No generative alteration or opaque enhancement.

### Group decisions

Support approve-all, individual review, split, merge, reject, group approval plus explicit exceptions. Regeneration must reproduce the same effective individual state.

### Staleness

Source/profile/campaign fingerprint mismatch makes sessions and decisions stale. Never silently retarget. Preserve historical decisions and require explicit refresh/review before promotion.

## Non-goals

Do not implement:

- reference-game integration;
- procedural fallback switching in a game;
- game save/load migration;
- Linux game export closure;
- game-level performance comparison;
- complete commercial library curation;
- profile-bundle import/export;
- remote or multi-user collaboration;
- server/database requirement;
- browser editor, scene editor, or map editor;
- image generation;
- unrestricted taxonomy generation;
- automatic human-gated approval;
- another renderer, audio backend, animation evaluator, or content model;
- dependence on raw physical keyboard events;
- a general shell or unrestricted chat interface;
- runtime dependency on the shared asset home;
- broad guide migration;
- TBPs or issue templates.

## Focus Area 1 — Session and alias contract

Authority: `docs/specs/asset-workbench-session-and-alias-contract.md`.

Schemas:

```text
agentic2d.asset-workbench-session.v1
agentic2d.asset-workbench-alias-map.v1
agentic2d.asset-workbench-recent-items.v1
agentic2d.asset-workbench-status.v1
```

Blocking tests:

- canonical and current-alias navigation;
- stale alias after filter change;
- stale alias after source refresh;
- independent aliases in two sessions;
- fresh aliases after resume;
- no aliases in durable outputs.

## Focus Area 2 — RDP-safe workbench input

Authority: `docs/specs/asset-workbench-input-contract.md`.

Schemas:

```text
agentic2d.asset-workbench-input-state.v1
agentic2d.asset-workbench-input-command.v1
agentic2d.asset-workbench-input-result.v1
```

Required implementation shape:

```text
text entry or mouse/touch selection
→ shared WorkbenchInputCommand
→ validation and alias resolution
→ canonical workbench action
```

Blocking tests:

1. Text stream `"2"` updates the visible buffer without acting before submission.
2. Enter submits choice 2.
3. Clicking Submit submits without requiring Enter delivery.
4. Typing `"12"`, Backspace, then Enter selects choice 1.
5. Invalid `"99"` displays an error, records no decision, and remains editable.
6. Paste `"find chest"` performs bounded local search after explicit submission.
7. Mouse click on choice 2 and submitted text `"2"` produce the same canonical command.
8. A complete candidate flow works using mouse/touch controls only.
9. The same flow works through editable text, Backspace, Enter, Escape, and Tab where supported.
10. Simulated RDP/IME input supplied as text/composition rather than key-down events works.
11. Focus loss/regain never submits or erases accepted decisions.
12. Preview-host restart does not disable the input surface.
13. Partial input never becomes a durable decision.
14. No primary action requires function keys, modifiers, or key chords.

## Focus Area 3 — Decisions and consequences

Authority: `docs/specs/asset-workbench-decision-and-consequence-contract.md`.

Schemas:

```text
agentic2d.asset-review-session.v1
agentic2d.asset-review-decision.v1
agentic2d.asset-review-summary.v1
agentic2d.asset-review-diagnostics.v1
agentic2d.asset-consequence-presentation.v1
```

Blocking tests cover every action, group exceptions, structured crop/frame/pivot/audio corrections, decision supersession, conflicting decisions, stale decisions, visual-only approval, and prohibition of silent gameplay binding.

## Focus Area 4 — Preview host and IPC

Authority:

```text
docs/architecture/asset-workbench-and-preview-host-architecture.md
docs/specs/asset-preview-host-ipc-contract.md
docs/decisions/ADR-0041-asset-preview-host-is-a-separate-restartable-client-around-engine-systems.md
```

IPC schemas:

```text
agentic2d.asset-preview-ipc.hello.v1
agentic2d.asset-preview-ipc.request.v1
agentic2d.asset-preview-ipc.response.v1
agentic2d.asset-preview-ipc.event.v1
agentic2d.asset-preview-scene.v1
agentic2d.asset-preview-capture.v1
```

Required recovery:

```text
record decision
→ terminate preview host
→ restart/reconnect
→ decisions and workbench input remain usable
→ candidate reload succeeds
```

Malformed candidates produce a diagnostic scene without destroying the host or session.

## Focus Area 5 — Deterministic promotion

Authority: `docs/specs/approved-asset-and-deterministic-promotion-contract.md`.

Blocking tests:

- byte/semantic repeatability;
- path independence across asset-home locations;
- deterministic stable-ID collision handling;
- atomic failure preserving prior output;
- changed source blocking stale decisions;
- operational session/input metadata not changing promotion;
- group approval plus exceptions producing expected effective assets;
- affected rebuild modifying only dependencies.

## Focus Area 6 — Evidence and M030 readiness

Authority: `docs/artifacts/asset-workbench-session-and-promotion-review-pack-contract.md`.

Pack:

```text
asset-workbench-review-pack/
├─ manifest.json
├─ index.md
├─ session/
├─ input/
├─ decisions/
├─ visual/
├─ audio/
├─ promotion/
├─ recovery/
└─ diagnostics/
```

Input evidence must include:

```text
editable-command-field.png
text-choice-before-submit.png
text-choice-after-correction.png
invalid-input-recovery.png
submit-button-use.png
mouse-choice-equivalence.json
mouse-only-workflow.json
text-stream-workflow.json
rdp-composition-input.json
focus-recovery.json
```

Also produce `m030-readiness.json` and `.md` containing approved media, roles, provenance, future bindings, fallbacks, blockers, export inputs, and performance considerations.

Proof set:

- one static/stateful visual;
- one animation;
- one environment/presentation group;
- one audio cue/family;
- one rejection;
- one deferral;
- one correction;
- one group approval with exception;
- one visual-only consequence decision.

## Implementation constraints

- Build on M028 profiles, annotations, campaigns, and batches.
- Do not create a second discovery model.
- Primary interaction must work without raw physical key-down events.
- Text-stream, mouse/touch, and canonical command paths share business logic.
- Aliases are ephemeral; canonical IDs are durable.
- Humans choose concepts and consequences rather than edit raw metadata.
- Decisions survive preview crashes and input focus changes.
- Preview owns no decisions or durable input state.
- Use actual engine audiovisual systems.
- One normal preview window persists per session.
- No audio auto-play.
- Headless operation remains available.
- Human-gated semantics cannot be auto-approved.
- Presentation approval remains separate from gameplay binding.
- Promotion is deterministic and atomic.
- Promoted outputs contain no absolute asset-home dependency.
- Do not modify reference-game runtime bindings.
- M028 review stays historical.
- M029 review gates M029 only.
- Implementation agents do not read external guides, `.guide-profile.json`, or `.guide-sync/`.

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
docs/specs/asset-workbench-session-and-alias-contract.md
docs/specs/asset-workbench-input-contract.md
docs/specs/asset-workbench-decision-and-consequence-contract.md
docs/specs/asset-preview-host-ipc-contract.md
docs/specs/approved-asset-and-deterministic-promotion-contract.md
docs/architecture/asset-workbench-and-preview-host-architecture.md
docs/artifacts/asset-discovery-and-campaign-review-pack-artifact-contract.md
docs/artifacts/asset-workbench-session-and-promotion-review-pack-contract.md
docs/artifacts/consumer-authoring-review-pack-artifact-contract.md
docs/engineering/product-cli.md
docs/engineering/command-contract.md
docs/engineering/validation-tiers.md
docs/engineering/constrained-validation-execution.md
docs/engineering/human-review-workflow.md
docs/decisions/ADR-0040-shared-local-asset-home-is-authoring-infrastructure-not-game-authority.md
docs/decisions/ADR-0041-asset-preview-host-is-a-separate-restartable-client-around-engine-systems.md
docs/milestones/MILESTONE-028-shared-asset-library-reusable-discovery-profiles-and-campaign-foundations.md
docs/milestones/MILESTONE-029-choice-driven-asset-workbench-persistent-audiovisual-preview-and-deterministic-promotion.md
```

Read `.review/` only for M029 and M028 historical provenance.

## Likely affected areas

```text
src/Agentic2D.Tools
src/Agentic2D.Engineering
src/Agentic2D.Rendering
src/Agentic2D.Sound
current raylib client/adapter
new or current preview-host executable
workbench UI/input control implementation
animation/content loading
tests/unit and tests/integration
eng/
bounded M028 fixtures
bounded M029 promotion workspace
artifacts/assets/M029
artifacts/validation/m029-smoke
.review/pending and .review/records
directly affected active docs
```

Do not create duplicate renderer/audio/content projects.

## Validation tiers

### Tier 1 — Sessions, aliases, and input

```bash
./eng/build.sh
./eng/test-filter.sh AssetWorkbenchSession
./eng/asset-workbench-session-smoke.sh
./eng/asset-workbench-alias-smoke.sh
./eng/test-filter.sh AssetWorkbenchInput
./eng/asset-workbench-input-smoke.sh
./eng/asset-workbench-rdp-input-smoke.sh
./eng/asset-workbench-mouse-input-smoke.sh
```

### Tier 1 — Decisions and consequences

```bash
./eng/test-filter.sh AssetWorkbenchDecision
./eng/asset-workbench-decision-smoke.sh
./eng/asset-workbench-consequence-smoke.sh
```

### Tier 1 — IPC and recovery

```bash
./eng/test-filter.sh AssetPreviewIpc
./eng/asset-preview-ipc-smoke.sh
./eng/asset-preview-recovery-smoke.sh
```

### Tier 2 — Graphics and audio

```bash
./eng/asset-preview-graphical-smoke.sh
./eng/asset-preview-audio-smoke.sh
```

### Tier 1 — Promotion

```bash
./eng/test-filter.sh AssetPromotion
./eng/asset-promotion-smoke.sh
./eng/asset-affected-rebuild-smoke.sh
```

### Tier 2 — Integrated workbench and regression

```bash
./eng/asset-workbench-smoke.sh
./eng/asset-workbench-review-pack-smoke.sh
./eng/product-validate.sh
./eng/cli-smoke.sh
./eng/m028-provider-smoke.sh
./eng/check.sh
```

### Tier 5 — Human review

```bash
./eng/review-list.sh --milestone M029
./eng/review-show.sh review.m029.choice-driven-workbench-preview-and-promotion
./eng/review-check.sh --milestone M029
```

## Validation execution mode

```text
focused structural/input: direct
graphics/audio: direct in capable environment
aggregate: resumable-sharded
UX/visual/audio/creative/semantic acceptance: blocking human review
```

Suite:

```bash
./eng/m029-smoke.sh
```

Plan:

```bash
./eng/m029-smoke.sh --plan-json
```

Required shards:

```text
session-aliases
rdp-text-input
mouse-touch-input
input-equivalence
guided-decisions
consequence-confirmation
preview-ipc
preview-recovery
graphical-preview
audio-preview
promotion
affected-rebuild
workbench-review-pack
m028-regression
documentation
human-review
integrated
```

Receipt root:

```text
artifacts/validation/m029-smoke/
```

Fingerprint semantic workbench, input command model, decision, preview protocol/implementation, promotion, proof sources, evidence, docs, and review state. Exclude absolute paths, sockets, PIDs, window position, device identity, timestamps, playback timing, text-composition implementation details, partial text buffers, aliases, and external guide state.

Only `./eng/m029-smoke.sh --verify` establishes aggregate success.

## Acceptance criteria

M029 is complete only when:

1. sessions create, resume, and close safely;
2. canonical/current aliases work and stale aliases fail;
3. aliases never enter durable authority;
4. ordinary review requires no raw metadata editing;
5. editable text input works without raw physical key events;
6. text can be corrected before explicit submission;
7. Submit button works when Enter is unavailable;
8. invalid input remains editable and records no decision;
9. paste and composition-style input work;
10. mouse/touch choices produce the same canonical actions as text;
11. a bounded flow completes mouse-only;
12. a bounded flow completes with editable text input;
13. focus loss does not submit or lose accepted decisions;
14. no primary action requires shortcuts or modifiers;
15. every required decision and group action works;
16. history, reversal, conflicts, and staleness are explicit;
17. consequences require confirmation and visual-only approval works;
18. preview host is separate and restartable;
19. one normal window persists across candidate changes;
20. actual engine rendering, animation, sound projection, and adapters are used;
21. comparison, overlays, animation controls, capture, and malformed scene work;
22. audio never auto-plays and A/B/no-device behavior works;
23. host failure does not lose decisions or disable the workbench input surface;
24. proof set includes static visual, animation, environment group, and audio;
25. promotion is deterministic, path-independent, staged, validated, and atomic;
26. stale decisions block promotion;
27. provenance links source/profile/candidate/decision/recipe/output;
28. affected rebuild changes only dependencies;
29. review pack hashes and safe paths validate;
30. M030 readiness identifies approved inputs, future bindings, fallbacks, blockers, and export inputs;
31. M028 regression passes;
32. blocking M029 review is approved;
33. `./eng/m029-smoke.sh --verify` passes.

## Direct documentation impact

Update only directly affected active project truth:

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
docs/engineering/command-contract.md
docs/engineering/validation-tiers.md
```

## Deferred documentation synchronization hints

```text
.guide-sync/pending/2026-07-19-m029-workbench-preview-input-and-promotion-sync.md
.guide-sync/pending/2026-07-19-m029-to-m030-consumer-integration-handoff-sync.md
```

Ordinary implementation agents do not read them.

## Human review

```text
applicability: blocking
classes: UX, visual, creative, semantic, artifact-quality
canonical ID: review.m029.choice-driven-workbench-preview-and-promotion
owning milestone: M029
reviewer: repository user
acceptable completion: approved
waiver: no implicit waiver
request: .review/pending/review.m029.choice-driven-workbench-preview-and-promotion.json
record: .review/records/review.m029.choice-driven-workbench-preview-and-promotion.json
check: ./eng/review-check.sh --milestone M029
```

Evidence includes editable text input, correction before submit, invalid-input recovery, Submit-button use, mouse selection, mouse/text equivalence, mouse-only flow, RDP/composition-style input, focus recovery, numbered navigation, decisions/consequences, persistent-window captures, animation/audio evidence, host restart, promotion/provenance, atomic failure, affected rebuild, M028 regression, M030 readiness, and direct-doc diff summary.

Where practical, perform the UX review through the Android-to-Ubuntu RDP environment that motivated the requirement.

M030 declares its own review and does not reopen M029.

## Constrained-runtime handling

1. Run `./eng/m029-smoke.sh --plan-json`.
2. Run structural and input shards separately.
3. Reuse one managed preview session for graphics/audio shards.
4. Generate input, workbench, recovery, and promotion evidence.
5. Run review list/show/check for M029.
6. Stop and report the canonical ID while pending.
7. After approval, run `human-review`.
8. Run `./eng/m029-smoke.sh --verify`.
9. Report exact verifier output.

The preview host may be a managed child process, but background execution is not itself proof.

## Recovery

- write decisions before relying on preview state;
- partial input never becomes a decision;
- focus changes never submit automatically;
- regenerate aliases after resume;
- preserve decisions across preview failure;
- keep input surface usable after preview restart;
- stage and atomically replace promotion;
- preserve prior valid promotion on failure;
- retain historical decisions and approved definitions until explicitly superseded;
- never silently retarget after source/profile changes;
- if review blocks, retain evidence and do not claim M030 readiness.

## Out-of-scope guide migration

None. Do not modify `.guide-profile.json`, copy guide documents, or require external-guide reading.
