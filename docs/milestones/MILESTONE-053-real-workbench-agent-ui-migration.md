# Milestone 053 — Real Workbench Migration to the Agent-Oriented UI System

## Execution profile

| Field | Decision |
|---|---|
| Lifecycle | **ready** — project-specific authority and planning-seeded obligation ledger provided by this overlay |
| Guide | `carlrabbit/agentic-project-guides` v0.9.1 |
| Repository | `carlrabbit/dotnet-ai-first-2d-game-engine` |
| Implementation mode | `ai-executed-broad` |
| Baseline implementation model | `GPT-5.6 Luna` (from `.guide-profile.json`, planning only) |
| Applicable profiles | repository-wide `base` and `artifact-first-runtime`; `dotnet-library` for `engine-libraries`; `cli-tool` only for changes to the supported product-cli surface, not engineering-only wrappers |
| Readiness | **ready for baseline executor**: substantive target, architecture, compatibility, validation and review decisions are resolved here and in the permanent M053 spec |
| Execution tractability | One coherent integration/migration spanning three existing workbench entry points; implementer creates bounded work packages without compressing obligations |
| Ledger | `.execution/M053.md` supplied as planning-seeded pending registry |
| Active platform | Windows / PowerShell 7; portable headless tests plus actual native Raylib integration on a graphics-capable Windows desktop |
| Inactive platform | Linux native verification deferred by current platform-epoch policy, not counted as passed |
| Validation tiers | 1 focused, 2 repository, 3 native/integration, 5 genuine subjective human review; 4 only if supported distribution changes |
| Completion record | `## Completion Evidence` below; implementer fills on genuine completion, never pre-seeded |

## Goal and boundary

Replace fixed-position GUI implementations of the **real Human Review Workbench, Asset Workbench and generic Asset Preview** with the M052 typed C# composition/session/layout/diagnostic architecture. A user should not encounter screens whose status labels overlap navigation, whose candidate rows use hand-authored hitboxes, or whose media controls escape layout validation. The actual workbench workflows, decision history, persistence, preview materialization, preview-host IPC and audiovisual semantics remain authoritative in their existing application systems.

M052 supplied the library and migrated the Product Shell plus a synthetic UI stress screen. M053 must migrate existing production-facing workbenches, not add more demos. M054 (separate) evaluates agent effectiveness with genuine authoring/editing/regression tasks; M053 implements and proves sound software, not AI productivity claims.

The permanent M053 contract is `docs/specs/real-workbench-agent-ui-migration-contract.md`, which implements the current project direction `docs/architecture/agentic-ui-direction.md`. Neither is replaced by the planning conversation, temporary ZIP instructions or research notes.

## Real user-visible targets

1. **Human Review Workbench**: single-item question, status, count, previous/next, optional real preview, Accept/Reject, pending/saving/failure/Retry, Restart/reset and final close. Include the actual `Status: PENDING` header collision as a regression.
2. **Asset Workbench**: session identity and candidate list, numeric/typed command entry, clipboard/clear/submit, accept/reject/defer/presentation-only, RDP/mouse/touch/keyboard dispatch and stable alias/session generation.
3. **Asset Preview**: one native executable, with distinct image, animation, audio and unsupported diagnostic surfaces. Actual current candidate media and operation controls must be placed inside the new canonical UI projection, not drawn as independent pixel-coordinate interfaces.

M053 includes real native input and headless validation for all targets. The Product Shell and M052 stress experience are unchanged maintained consumers; their existing behavior is a compatibility regression, not a substitute for the real migration.

## Required repository authority

Read only the following plus live relevant source/tests and normal root `AGENTS.md` / `docs/ENGINEERING.md`:

- `docs/specs/real-workbench-agent-ui-migration-contract.md` (**new permanent migration contract; included in package**).
- `docs/specs/agent-oriented-ui-contract.md`.
- `docs/architecture/agentic-ui-direction.md`.
- `docs/specs/simple-human-review-workbench-contract.md` and `docs/engineering/human-review-workflow.md`.
- `docs/specs/asset-preview-review-surface-contract.md`.
- `docs/specs/actual-candidate-preview-and-curation-contract.md` and `docs/specs/asset-preview-host-ipc-contract.md`.
- `docs/architecture/asset-workbench-and-preview-host-architecture.md` plus the current canonical M029 asset-workbench/decision input contract located through `docs/SPECS.md`.
- `docs/engineering/validation-tiers.md`, `docs/engineering/platform-verification.md`, `docs/engineering/command-contract.md`, `eng/platform-verification.json`.

Source locating aids: `RaylibGameWindow.ShowReviewWorkbench`, `AssetWorkbenchRaylibWindow.Run`, `AssetPreviewRaylibWindow.Run`, `ReviewSurfaceLayout`, `Agentic2D.UI/AgentOrientedUi.cs`, `M052UiScreens.cs`, `RaylibAgentUiRenderer`, `M048ActualCandidatePreview`, `M050ModalitySpecificPreviewSuite`. These names are locators, not a closed edit allowlist. Ordinary implementation **must not** read the external guide repository, `.guide-profile.json`, `.guide-sync/` or non-authoritative research to reconstruct requirements.

## Resolved decisions and compatibility

- One strongly typed C# UI model/session/layout/hit-test authority, one shared Raylib draw/media adapter. No XAML, web UI, ASCII layout authoring or second app-specific layout engine. Renderer-owned image placement inside a projected media slot is allowed, independent control/status layout is not.
- Application workflow and persistence remain external to the UI library. UI-local draft/focus/scroll/hover/animation state remains local to UI. Actions are thin revalidated requests into current canonical review/asset/preview operations.
- Preserve real review semantics: one-at-a-time subjective review, async serialized bounded decision queue, current review statuses, optional preview, Retry/failure, Restart via review-reset, and no changes to approved historical records. No new required reviewer interactions, human decisions, comments or alternative approval workflow.
- Preserve real asset semantics: exact candidate/materialization identity, alias-generation and canonical JSONL input, preview IPC v2, transient preview controls and sound/image/animation resource operations. Structured media kind, not IDs, selects controls. Missing media/audio resource errors cannot be hidden by UI feedback.
- Live desktop resizes use actual M052 layout and pointer geometry; default Full HD design but fit available work area. Supported desktop resolution/scales as in the M053 spec. The legacy 1120×720 and 960×620 application canvases are not valid active UI authorities.
- Preserve current historical M048/M050 acceptance and human review ownership. M053 runs current *focused behavioral regressions* and should not require re-certifying every M048 receipt. The larger repository-wide fingerprint/receipt refactor is separately roadmap-tracked; it is not pulled into this migration.
- Plan a distinct **M053-owned human-review set** for the three genuinely different application UX experiences (with image/animation/audio treated as materially distinct preview modes). Use the canonical review workflow; no waivers or automatic approvals. Required native machine checks and valid screenshots precede human assessment.
- Reuse M052 UI API and engineering tooling unless a concrete behavior gap requires a bounded, backward-compatible framework extension. Such an extension must be tested against the M052 Product Shell/Stress consumers; do not replace the framework or silently fork it.

## Non-goals

No M054 agent-productivity evaluation; no second/next-generation renderer; no new editor, XAML/browser stack or hand-authored visual markup; no rewrite of M047 promotion, M048 IPC/materialization, M029 command/decision models, M038 review persistence, or M052 foundation; no bulk history/ledger reconciliation; no change to unrelated game runtime or public distribution; no global repair of old milestone-fingerprint caching. Do not overwrite historical review files or force approvals from older milestones to close M053.

## Acceptance obligations — exact identity and wording

The `AC-*` rows below are planner-owned stable acceptance obligations. Implementer retains exact wording and proves each separately. A combined green validation report is not sufficient evidence for all obligations.

- **AC-01:** The real Human Review Workbench, Asset Workbench, and Asset Preview (image/animation/audio) author their visible application chrome, content and interactive controls through M052 typed UiDefinition/UiSession and the canonical headless layout/draw projection, not a synthetic substitute.
- **AC-02:** No migrated application surface retains an independent structural layout solver, manually positioned control/text chrome, author-maintained hitboxes, or virtual 1120x720/960x620 coordinate scaling; native pixel placement is limited to drawing media inside canonical projected content slots and other explicitly renderer-owned internals.
- **AC-03:** Each workbench has an inspectable, renderer-independent application-state-to-UiDefinition adapter and stable semantic action-to-existing-operation adapter; neither UI definitions, sessions, nor Raylib renderer own domain authority or introduce another general command bus.
- **AC-04:** The Human Review Workbench presents the exact loaded one-at-a-time review item, previous/next navigation, separate status/progress header slots, readable full question and real review/demo launch affordance without unintended overlap, text truncation or invented review steps.
- **AC-05:** Human Review Workbench Accept/Reject preserve canonical approved/changes-requested decisions, independent asynchronous FIFO bounded persistence, immediate local acknowledgement and automatic progression while the render loop remains responsive and never asserts persistence success early.
- **AC-06:** Human Review Workbench queue activity, failure, Retry and final status accurately reflect authoritative persistence; Close is not available until all jobs succeed; failures never become accepted decisions and restarting is not required just to retry a failure.
- **AC-07:** Human Review Workbench Restart drains pending jobs, invokes canonical active-milestone review reset, restores the complete review set and local navigation state, and preserves historical completed review decisions and records outside the active milestone.
- **AC-08:** Human Review Workbench preserves optional launching of exact M048 image/animation/audio candidates and the M052 UI stress experience when applicable, with the existing child-process, review-ID, current prerequisite and reviewer decision semantics.
- **AC-09:** Human Review Workbench preserves the simple subjective-only review contract, bounded question navigation, no comments/ratings/extra required steps, complete milestone review-set ordering and canonical review-run/review-check behavior.
- **AC-10:** Asset Workbench presents actual session identity, candidate/choice list, availability and response feedback through the typed UI, including bounded keyboard/pointer/touch navigation and scrolling for more or longer candidates than previously fit fixed rows.
- **AC-11:** Asset Workbench preserves editable command entry, clipboard paste, clear and explicit submit; ordinary focus changes and editing never emit a canonical command, while Enter submits only through the documented focused/valid command-entry behavior.
- **AC-12:** Asset Workbench pointer and keyboard decisions (accept, reject, defer, presentation-only), candidate choice and command entry emit the existing exact canonical input-command JSONL envelope, source semantics and operation availability checks; UI does not directly mutate decisions or promotion.
- **AC-13:** Asset Workbench preserves session ID, alias generation, stale-input rejection, text-stream/RDP/mouse-touch equivalence and provider-controlled validation across native GUI and non-GUI entry points.
- **AC-14:** Asset Preview keeps one generic executable and uses structured bundle mediaKind to select exclusively image, animation, audio or unsupported diagnostic UiDefinition; arbitrary review/candidate IDs and filenames never determine modality.
- **AC-15:** Each Asset Preview modality presents exact reviewer context (modality, candidate, purpose, variant, corrections, raw/processed meaning and question) with bounded readable wrapping/clipping and no fingerprint-only substitution.
- **AC-16:** Image preview keeps meaningful Source/Processed, isolate/context, overlay and filtering controls, shows the actual current candidate media using the canonical slot geometry, and omits unrelated animation/audio controls.
- **AC-17:** Animation preview keeps actual ordered candidate frame presentation with truthful Play/Pause/Step/Reset state and frame progress, independently controlled presentation time, and no irrelevant image/audio-only controls.
- **AC-18:** Audio preview keeps explicit manual Play Raw/Play Processed/Stop, both duration/variant/correction context and device diagnostics; it never auto-plays, invents successful playback on device failure, or displays unrelated image/animation controls.
- **AC-19:** Real texture/frame/audio-dependent media presentation uses a renderer-independent content-slot/media projection with authoritative resolved placement, clipping and accessibility; Raylib may load/play media inside projected slots but cannot choose UI layout or input hitboxes.
- **AC-20:** Asset Preview preserves M047/M048 exact materialization-subject/bundle identity, preview host IPC v2, restart/recovery, acknowledgement invalidation, and canonical approval guard; it does not create a second preview, curation or persistence state machine.
- **AC-21:** All migrated surfaces use consistent pointer press/release/capture, keyboard focus/activation/edit isolation, visible focus, disabled/busy cues, semantic names/roles, modal blocking and predictable input cancellation without accidental gameplay or domain action leakage.
- **AC-22:** All migrated surfaces are usable with live native resize and DPI/UI scaling on 1280x720, 1920x1080, 2560x1440, 3840x2160 and 3440x1440 viewports; 1920x1080 remains design reference and smaller desktop work areas fit safely.
- **AC-23:** Headless layout deterministically diagnoses accidental overlap, overflow, inaccessible essential controls and unbounded text in real workbench compositions, while correctly permitting intentional clipped scroll/media; status/question/navigation and asset context/control collisions are mechanical regression cases.
- **AC-24:** Retained UI-local state survives compatible application recomposition, resize and modal changes without duplicate actions; presentation animations depend on injected presentation time, are reduced-motion aware and never alter operation or simulation results.
- **AC-25:** Headless artifacts expose versioned real workbench semantic tree, resolved geometry/clips, focus/interaction trace, current operation projection, diagnostics and deterministic fingerprints with equivalent-input reproducibility.
- **AC-26:** Executable positive and deliberately broken negative validation independently proves the GUI quality gate rejects representative bad layouts, wrong hit targets, disabled/stale actions and misleading operation states before native screenshot review.
- **AC-27:** Active Windows Raylib proof launches all three genuine workbench entry points and exact image/animation/audio candidate previews, captures actual frames and exercises representative real pointer/keyboard action paths against projected targets without stale screenshots or mock UIs.
- **AC-28:** Existing supported M029/M037/M038/M047/M048/M050/M052 workflows, review history, CLI/JSONL/IPC schema and published compatibility surfaces remain behaviorally intact; implementation does not replay or modify approved historical human reviews.
- **AC-29:** M053 regression validation tests the actual affected behaviors as normal unit/integration/graphics tests, without gating M053 on wholesale stale M048/M050 historical milestone receipts; any outstanding predecessor acceptance obligations remain separately owned and unweakened.
- **AC-30:** Direct project specifications and engineering/authoring instructions are updated for migrated surfaces, public typed composition/adapters, media-slot ownership, headless inspection, input mappings and command boundaries; broad index archaeology remains a separate synchronization task.
- **AC-31:** M053 owns genuine required human review of the migrated Review Workbench, Asset Workbench and image/animation/audio preview experiences, with launchable current evidence and no synthesized approvals; mechanical criteria are validated before subjective judgment.
- **AC-32:** The implementer runs required active-Windows Tier 1–3 validation and applicable consumer gates, reconciles each original AC/EC against concrete executed evidence and current repository state, preserves durable completion proof and reports only COMPLETE, AWAITING HUMAN REVIEW or BLOCKED as justified.

## Selective evidence cases — distinct proof paths

These `EC-*` cases are separately required where one acceptance obligation spans distinct genuine workflows/behaviors. They must be preserved verbatim in the planning-seeded ledger and satisfied independently. No Cartesian product of every control/resolution is required.

- **EC-01a** (parent **AC-01**): Canonical Review Workbench actually composes/draws and dispatches through M052, not the old fixed-coordinate path.
- **EC-01b** (parent **AC-01**): Asset Workbench and each of the three actual preview modalities use M052 composition and canonical layout for controls and content.
- **EC-04a** (parent **AC-04**): Status, arrows, question count and variable-length question remain disjoint in real headless geometry, including the previously reported PENDING overlap case.
- **EC-04b** (parent **AC-04**): Previous/next and automatic next-undecided transitions preserve exact review order and current status without changing durable state on navigation.
- **EC-05a** (parent **AC-05**): Successful Accept/Reject enqueue and persist canonical decisions asynchronously with immediate feedback and correct progression.
- **EC-05b** (parent **AC-05**): Multiple rapid decisions do not duplicate or reorder writes, and no visible UI state falsely declares persistence successful.
- **EC-06a** (parent **AC-06**): Failed persistence is visible, retryable and blocks close until the queue is resolved, including on final status.
- **EC-07a** (parent **AC-07**): Restart drains, resets and reopens only the active milestone without rewriting an unrelated approved historical review.
- **EC-08a** (parent **AC-08**): Exact M048 image/animation/audio preview launch remains optional and stable under current review registry.
- **EC-08b** (parent **AC-08**): M052 synthetic UI stress launch remains available without writing real review decisions.
- **EC-11a** (parent **AC-11**): Typing, paste, delete and focus change preserve draft without unexpected submit; explicit submit emits once.
- **EC-12a** (parent **AC-12**): Pointer/touch and keyboard choice/decision routes produce matching canonical commands with original source/generation/session fields.
- **EC-13a** (parent **AC-13**): Stale alias generation or unavailable operation cannot be turned into authoritative decision by UI activation.
- **EC-14a** (parent **AC-14**): Structured image, animation and audio dispatch yields distinct correct controls even with misleading review/candidate IDs.
- **EC-14b** (parent **AC-14**): Unknown mediaKind produces explicit diagnostic and no misleading modality controls.
- **EC-16a** (parent **AC-16**): Source/processed images, isolation/filter/overlay controls produce real current-candidate visual state and respect canonical media slot.
- **EC-17a** (parent **AC-17**): Frame order and Play/Pause/Step/Reset alter actual animation state, including time step and frame progress.
- **EC-18a** (parent **AC-18**): Manual raw/processed/stop works on an available audio device; no autoplay or image/animation controls.
- **EC-18b** (parent **AC-18**): Missing audio device or media yields honest diagnostic and cannot claim successful playback.
- **EC-20a** (parent **AC-20**): Preview restart, changed materialization subject and stale acknowledgement preserve the existing decision guard.
- **EC-21a** (parent **AC-21**): Native/headless input targets, focus, modal interception, keyboard edit and disabled-state checks share one resolved geometry.
- **EC-22a** (parent **AC-22**): Full HD, smaller, QHD, 4K, ultrawide and fractional DPI/UI scales retain eligible controls and unclipped critical content.
- **EC-23a** (parent **AC-23**): Injected status/question collision and oversize preview context fail the headless gate with specific actionable element paths.
- **EC-23b** (parent **AC-23**): Intentional media slot clipping and long scroll content pass without false overlap/overflow.
- **EC-24a** (parent **AC-24**): Recomposition/resize retains eligible draft/focus/state but clears stale capture/action and keeps animation separate from operation state.
- **EC-26a** (parent **AC-26**): Positive real workbench screens pass; injected invalid hit/disabled control and false-save completion independently fail.
- **EC-27a** (parent **AC-27**): Live Review Workbench and Asset Workbench accept native pointer/keyboard input through projected control bounds and preserved command/persistence adapters.
- **EC-27b** (parent **AC-27**): Actual image, animation and audio preview executables render current real candidate media/controls in the native desktop with genuine captures.
- **EC-28a** (parent **AC-28**): M038 review/review-check, M029 asset text/mouse/touch JSONL, M048 preview IPC and M050 modality contracts show independently exercised compatible behavior.
- **EC-29a** (parent **AC-29**): An unrelated historical documentation/ledger modification does not alone force all old M048 acceptance shards as an M053 prerequisite.
- **EC-31a** (parent **AC-31**): Human review separately exercises workbench decision/status UX, asset command/choice UX and actual image/animation/audio readability/usability, with pending records left pending until approval.

## Validation gates and target/locus topology

The implementer owns concrete test partitioning, but must implement the actual required behavior and gates below. Implement the canonical resumable `m053-workbenches` suite through the existing `eng/suite.ps1` / engineering-host architecture. Portable shards and native shards may be independently resumed. Suggested behavioral shards are `headless-review`, `headless-asset-workbench`, `headless-modalities`, `headless-layout-quality`, `native-workbenches`, `compatibility`; the exact internal layout is implementation-owned, but every listed gate and EC remains mandatory.

Preferred canonical flow:

```powershell
pwsh ./eng/check.ps1
pwsh ./eng/suite.ps1 m053-workbenches --plan-json
# Run each required shard from the plan in a separate invocation:
pwsh ./eng/suite.ps1 m053-workbenches --shard <actual-id>
pwsh ./eng/suite.ps1 m053-workbenches --verify
```

A fresh all-shard report is not a replacement for individual behavioral evidence. On Windows native graphics/audio unavailable to the execution host, do not forge captures or reinterpret a portable test as native success. Investigate actual interactive desktop, then leave the precise gate blocked if genuinely unavailable. Linux is supported but inactive and may be recorded as deferred per platform policy.

### VAL-01 — Compiled public typed-screen authorship, normalization and real application projection adapters; focused unit tests

**Target/locus:** Tier 1; active Windows .NET; no graphics.

**Coverage intent:** AC-01, AC-02, AC-03, AC-09, AC-14, AC-30; EC-01a, EC-01b, EC-14a, EC-14b.

### VAL-02 — Headless Review Workbench behavior, real persistence queue/reset adapters, negative replay, deterministic generated layout

**Target/locus:** Tier 1/3; active Windows portable, no graphics.

**Coverage intent:** AC-04, AC-05, AC-06, AC-07, AC-08, AC-21, AC-24, AC-25; EC-04a, EC-04b, EC-05a, EC-05b, EC-06a, EC-07a, EC-08a, EC-08b, EC-21a, EC-24a.

### VAL-03 — Headless Asset Workbench canonical JSONL/input/alias generation and negative action tests

**Target/locus:** Tier 1/3; active Windows portable, no graphics.

**Coverage intent:** AC-10, AC-11, AC-12, AC-13; EC-11a, EC-12a, EC-13a.

### VAL-04 — Headless image/animation/audio/unsupported preview, media-slot semantic geometry and IPC state/negative tests

**Target/locus:** Tier 1/3; active Windows portable; audio-device optional for portable portion.

**Coverage intent:** AC-14, AC-15, AC-16, AC-17, AC-18, AC-19, AC-20; EC-14a, EC-14b, EC-16a, EC-17a, EC-18b, EC-20a.

### VAL-05 — Deterministic desktop resolution/DPI/font/clipping matrix, fault-injected quality gate and reproducible evidence

**Target/locus:** Tier 1/3; active Windows portable; actual M052 UiLayoutEngine.

**Coverage intent:** AC-02, AC-21, AC-22, AC-23, AC-24, AC-25, AC-26; EC-04a, EC-21a, EC-22a, EC-23a, EC-23b, EC-24a, EC-26a.

### VAL-06 — Native review/asset workbench and image/animation/audio Raylib real user input, operation, screenshots, media/audio device diagnostics

**Target/locus:** Tier 3; active graphical Windows desktop + Raylib/OpenGL; real audio device for positive playback when available.

**Coverage intent:** AC-05, AC-06, AC-08, AC-10, AC-11, AC-12, AC-16, AC-17, AC-18, AC-19, AC-21, AC-22, AC-27; EC-05a, EC-08a, EC-11a, EC-12a, EC-16a, EC-17a, EC-18a, EC-18b, EC-27a, EC-27b.

### VAL-07 — Focused current compatibility tests across M029/M038/M047/M048/M050/M052 and review/asset authority; compare unchanged schemas and no history mutation

**Target/locus:** Tier 1/2/3; Windows portable + bounded native if affected; focused tests not full old historical verifier.

**Coverage intent:** AC-03, AC-07, AC-08, AC-09, AC-12, AC-13, AC-20, AC-28, AC-29; EC-07a, EC-08a, EC-12a, EC-13a, EC-20a, EC-28a, EC-29a.

### VAL-08 — Canonical repository-wide build/test/format/check and affected consumer/CLI gate when changing supported distribution interface

**Target/locus:** Tier 2; Windows PowerShell 7; Tier 4 only when supported distribution boundary changes.

**Coverage intent:** AC-01, AC-03, AC-25, AC-28, AC-29, AC-30, AC-32; EC-01a, EC-01b, EC-28a.

### VAL-09 — Canonical resumable suite m053-workbenches --plan-json, individually invoked required shards, --verify; receipt/evidence integrity

**Target/locus:** Tier 3; Windows active; separate required native graphical shard from portable shards.

**Coverage intent:** AC-23, AC-25, AC-26, AC-27, AC-29, AC-32; EC-23a, EC-23b, EC-26a, EC-27a, EC-27b, EC-29a.

### VAL-10 — Registered milestone-owned simple required human review of actual migrated review, asset workbench and image/animation/audio candidate preview, followed by review-check

**Target/locus:** Tier 5; current active Windows interactive desktop; human subjective judgment only.

**Coverage intent:** AC-09, AC-15, AC-21, AC-27, AC-31, AC-32; EC-31a.

### VAL-11 — Exact AC/EC identity/wording and executed proof reconciliation, side-effects/history/policy audit, permanent Completion Evidence section

**Target/locus:** Tier 1; planning contract versus ledger versus current repo, current evidence.

**Coverage intent:** AC-02, AC-03, AC-20, AC-28, AC-29, AC-30, AC-31, AC-32; EC-01a, EC-01b, EC-20a, EC-28a, EC-29a, EC-31a.

## M053-owned subjective review experiences

After fresh real Windows Raylib and headless proof, use the canonical review commands to create/register M053-scoped required simple-review questions and launch the real M053 experiences. At least these bounded paths must be visibly inspectable, with independent review identity if subjective experiences materially differ:

- Review Workbench: question/header/status navigation, visible focus, async feedback and readable final state. Suggested ID: `review.m053.real-review-workbench-navigation-and-feedback`.
- Asset Workbench: readable candidate choices, command editing, keyboard/pointer feedback and status. Suggested ID: `review.m053.asset-workbench-choice-and-command-ux`.
- Exact image preview: subject context plus meaningful image controls. Suggested ID: `review.m053.image-candidate-preview-clarity`.
- Exact animation preview: subject context plus frame/playback legibility. Suggested ID: `review.m053.animation-candidate-preview-clarity`.
- Exact audio preview: subject context plus actual manual raw/processed comparison and device status. Suggested ID: `review.m053.audio-candidate-preview-clarity`.

Do not require more than the bounded simple review interactions for any one item; support launching its genuine experience in an existing review-run path. Review IDs are ownership/identity, not permissions to invent application state or approve by automation. Register through the current review workflow and retain pending state until the human decides. A missing audio device prevents claims of audible review even when machine negative diagnostics work.

## Direct documentation / compatibility transition

Implementer updates direct relevant specs/engineering authoring material and indexes once implementation truth exists; include examples of real screen composition and the one-source-of-layout principle, media-slot/renderer ownership, current commands, supported viewport scaling and headless diagnostics. Preserve accurate compatibility notes for M021/M037/M029/M038/M048/M050/M052 and existing current review records. Broader stale historical milestone index synchronization is separate. If a future implementer needs to change old M048/M050 completion contracts, that is a separate decision—not a side effect of M053.

## Implementation and completion contract

- Before edits, verify exact `AC-*` and `EC-*` identity and wording agreement with `.execution/M053.md`, and that every validation gate is mapped. The planning ledger begins entirely pending; concrete work packages and results are implementation-owned.
- Implement in coherent work packages; real Review Workbench migration, Asset Workbench migration, image/animation/audio preview, and headless/native quality and compatibility integration are likely seams but not a planner-imposed task list.
- Generate current criterion-specific evidence, not success-only booleans or claims from old aggregate receipts. For every error path, prove actual failure behavior. Keep native capture and UI state fingerprints linked to a concrete current implementation revision.
- Run all applicable machine gates before requesting human acceptance. If human review alone remains, terminate `AWAITING HUMAN REVIEW`. If native/environment-dependent required proof cannot run, report `BLOCKED` with actual evidence. Only use `COMPLETE` after accepted M053 human review, `review-check`, exact reconciliation, and durable completion record.
- Final completion audit confirms all `AC-*`, `EC-*`, and `VAL-*` sets and source evidence, no manual-layout fallback across all three migrated entry points, no historical review mutations, and current canonical library/graphics/regression gates.

## Completion Evidence

*Planning placeholder only. On genuine completion the implementer writes a compact durable per-criterion/evidence-case -> concrete code/artifact -> executed gate/target -> observed result trace. Do not populate this section as passing in the ready overlay or substitute a mutable `.execution/` ledger for durable history.*
