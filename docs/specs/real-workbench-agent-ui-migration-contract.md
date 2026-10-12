# Real Workbench Agent-Oriented UI Migration Contract — M053

## Authority and precedence

This document is active project-local product authority for M053's replacement of application-owned layout/hitbox code in the Human Review Workbench, Asset Workbench and generic Asset Preview executable. It extends, but does not replace, `docs/specs/agent-oriented-ui-contract.md` and `docs/architecture/agentic-ui-direction.md`.

Established domain/operation contracts remain authoritative within their own areas:

- Human review: `docs/specs/simple-human-review-workbench-contract.md`, `docs/engineering/human-review-workflow.md`, `docs/HUMAN-REVIEW.md`.
- Asset authoring/session/input: current M029 workbench and canonical decision/command specifications, including text-stream and mouse/touch equivalence.
- Exact candidate, decision, preview and IPC: `docs/specs/actual-candidate-preview-and-curation-contract.md`, `docs/specs/asset-preview-host-ipc-contract.md`, `docs/architecture/asset-workbench-and-preview-host-architecture.md` and existing M047 materialization authority.
- Modality: `docs/specs/asset-preview-review-surface-contract.md`.
- Validation/platform: `docs/engineering/validation-tiers.md`, `docs/engineering/platform-verification.md`, `docs/engineering/command-contract.md`.

If a proposed GUI improvement changes established persistence or approval semantics, preserve those semantics and escalate material policy changes to planning; do not silently change them in the UI migration. No new GUI authoring DSL is authorized.

## The real entry points

All are actual product/engineering UI surfaces, not synthetic fixtures:

| Entry point | Existing code (locating aid) | Required new UI consumer |
|---|---|---|
| `review-workbench` / canonical `review-run` | `src/Agentic2D.DebugClient.Raylib/RaylibGameWindow.cs`, `ShowReviewWorkbench` | Real review navigation/status/question/preview/decision/final/restart screens |
| `asset-workbench` | `src/Agentic2D.DebugClient.Raylib/AssetWorkbenchRaylibWindow.cs` | Actual live candidate list, command edit, choice/decision, feedback and clipboard surfaces |
| `asset-preview` | `src/Agentic2D.DebugClient.Raylib/AssetPreviewRaylibWindow.cs` | Exact image/animation/audio/unsupported subject-specific preview and controls |

Existing `M052UiScreens.ProductShell` and `ReviewStress` remain genuine M052 examples and regression baselines but cannot substitute for any M053 real migration target.

## One structural authority for all migrated GUI surfaces

For every active migrated workbench screen:

```text
application/domain projection
  -> typed compositional C# UiDefinition
  -> UiSession + UiLayoutEngine
  -> ordered semantic + physical draw/media commands and hit/focus regions
  -> Raylib renderer + media resource host

Raylib native input
  -> UiSession event dispatch (same canonical bounds)
  -> stable UiActionRequest
  -> thin adapter revalidates current application operation
  -> existing review/asset/preview domain authority
  -> recomposed application projection
```

All application chrome (headings, status, question, controls, tooltips when applicable, alerts, preview context, modal and feedback) must be defined in typed UI composition. Application code may express semantic sizing constraints, grow/flex, padding and scroll policies; it must not calculate rectangles or duplicate the layout engine. Raylib may calculate image texture sampling/letterboxing *within a canonical projected media/content-slot rectangle* but must not invent that rectangle, clip region or interaction hitbox. Intentionally renderer-owned image pixels, media resource lifetimes and low-level native window operations are not banned by the no-manual-layout rule.

The legacy `ReviewSurfaceLayout` fixed-rectangle and character-count wrapping helpers, old `LegacyCamera`/`ToLegacyCoordinates` mapping, `AssetPreviewRaylibWindow.ControlRect`, and asset list `116 + index * 58` row layouts must be removed from **active migrated workbench geometry**. Any remaining legacy adapter is explicitly documented and used only by excluded legacy consumers; no fallback route silently uses it in the three migrated entry points.

No XAML/WPF/WinForms/web/CSS/ASCII-authored GUI. Keep raylib-cs confined to the existing native adapter project. Typed UI definitions and application-state projections can be validated in portable .NET without graphics.

## Recomposition and state ownership

- Application projection owns domain-authoritative state, capability, operation availability and completion. UI-local session owns only focus, hover, capture, scrolling, draft text, selection and visual timing.
- Recompose by stable IDs after domain changes or viewport/scale changes. Preserve eligible UI-local state, but release stale actions, focus, capture and text drafts when their screen/identity no longer applies. Selection must not silently move to a different candidate or review item via index reuse.
- UI action IDs are stable and source-specific. Dispatch always rechecks the **current** projection and domain preconditions; a button that was enabled in an old render is not permission to approve a changed candidate or persist a stale review.
- Business effects and asynchronous queues remain in existing services/controllers; UI is a view and intent adapter, not another worker or command bus.
- Native close/focus-loss/escape and cancellation must not synthesize successful decisions or clear durable history.

## Human Review Workbench

Compose a proper shared layout: milestone/title, previous control, review ordinal, current-status slot, whole question, optional real preview-launch region, operation feedback, canonical decisions and final/restart/retry controls. Use row/column/grid/scroll rather than absolute pixel placement. The observed `Status: PENDING` over the back arrow/question is a mandatory regression fixture with a mechanically checked non-overlap outcome; use dynamic pending/saving/failed/approved status strings and long wrapped questions.

Preserve simple review behavior exactly: one item at a time; < / > navigation; automatic next-undecided after Accept/Reject; historical decisions display read-only when appropriate; no rating/comment/waive/skip sidebar or new interaction burden. The normal milestone review workbench shows all required/blocking items in canonical deterministic order. Preview launch remains optional for accepting/rejecting. Existing M048 exact preview and M052 UI-stress launch mechanisms remain functional. New M053 own review experiences are registered through existing canonical review machinery, not a parallel durable reviewer system.

The existing bounded FIFO asynchronous decision queue still serializes canonical `review record` effects. Show queued/saving/failed/retrying/saved status truthfully and promptly while allowing nonblocking rendering. Do not equate a local selected button with durable acceptance. Retry failures and ensure Close is disabled until all writes succeed. Restart drains, calls canonical active-milestone reset, preserves history/provenance, clears UI-local state and reloads the first undecided item; cannot reset old completed milestone reviews.

## Asset Workbench

Keep the canonical provider command JSONL contract (`agentic2d.asset-workbench-input-command.v1`) and source string semantics, original session ID and alias-generation fields. Use `Ui.List`/rows/scroll and UI text-field editing with stable candidate identity (not just row number), provide understandable status, display long candidates, enable keyboard/pointer/touch selection and command submission. A visual row activation maps to the **current** canonical visible choice/alias through the existing provider adapter; don't reinterpret aliases inside the UI.

Typed commands remain explicit: edit text, clear, clipboard paste, Submit button and focus-gated Enter; focus change, tabbing, clipboard paste and mere text entry never submit. Preserve current decision menu's accept/reject/defer/presentation-only variants and provenance/source distinction (`enter`, `submit-button`, `mouse-touch` where the current CLI contract requires them). Neither the UI nor renderer directly writes approved assets, bypasses candidate staleness, or changes provider-side history.

## One generic Asset Preview: three distinct modalities

Modality is selected solely from exact structured `mediaKind` (image/animation/audio) in the current scene/bundle model. Unknown kind renders a diagnostic-only surface. Preserve one generic preview executable, IPC v2, candidate/variant/correction identity, bundle fingerprint and exact acknowledgement semantics, along with restartable preview-host ownership.

Each actual UI displays structured human context: candidate, modality, purpose/role, chosen variant, typed corrections, raw/base vs processed meaning, and subjective review question. Context and failures use deterministic M052 font metrics with appropriate wrapping/scroll, not `ReviewSurfaceLayout.Wrap` approximations.

- **Image:** actual Source/Processed, candidate media, region isolation/source context, overlays, nearest/smooth. Preserve any relevant filters, and omit animation/audio controls.
- **Animation:** actual ordered frame images, Play/Pause/Step/Reset and frame/frame-count/progress; stepping changes the frame, rather than only its label. Presentation time advances playback independently of simulation time.
- **Audio:** manually initiated raw/processed/stop; raw/processed duration, correction/variant context and honest device/missing-media diagnostics. Never auto-play, misreport success when no audio device, or display image/animation controls.
- **Unsupported:** explicit diagnostic, no plausible-looking modality actions or stale media.

Image/animation drawing uses the canonical projected media-slot bounds and clips. Native resource host may load/unload textures and sounds, select frame buffer or fit texture to the given bounds without altering UI composition/hit zones. Headless evidence includes media identity/slot, content mode, progress/availability and diagnostics; headless must not require a physical audio device or GPU.

Audio-device-dependent positive proof should be run where audio output is available. Missing-device behavior is a required honest negative, never a synthetic passing audible-review claim. Retain existing human judgement on actual audiovisual quality where applicable.

## Desktop and accessibility policy

Design around Full HD `1920×1080`. Validate `1280×720`, `1920×1080`, `2560×1440`, `3840×2160`, `3440×1440`, and UI scales `1.0`, `1.25`, `1.5`, `2.0` with representative OS DPI. Windows should initialize inside the actual usable desktop and be resizable. Allow scroll for long data; essential decision, save/error status and critical context must remain reachable. Smaller/portrait viewports may be negative stress cases producing actionable diagnostics rather than false passing claims.

Focus must be visible and consistent with resolved bounds. Use semantic roles/names and accessible alternative text for media; active/disabled/busy state must remain distinguishable. Keyboard traversal and activation, mouse and compatible touch/remote input must map to exactly the same authored actions. Modal/capture handling must block click-through; no event leakage into gameplay or a second input pipeline.

## Mechanical GUI quality and evidence

Headless quality checks operate on the **actual workbench UiDefinition** and projected application scenarios. The tests must mechanically inspect status/question/header geometry, candidate list hit regions, scroll clips, modality context text, media regions, operation feedback and final-page controls. Use actual declared font advances. Assert semantic actions emitted and *not emitted* under stale/disabled/blocked states; inspect status changes driven by real application operation projections.

Require deliberately broken negative fixtures: status overlaps navigation/question; too-long context overflows without scroll; button hit region diverges from projection; unavailable action is activated; persistence reported successful before confirmation. Ensure they fail with specific diagnostic IDs/paths or executable negative assertions and that correct real screens pass at desktop matrix resolutions. It is unacceptable to generate `allClear=true` without exercised checks or to treat PNG size alone as GUI correctness.

Evidence must be versioned, deterministic for normalized identical input, and carry layout/semantic/focus/interaction/diagnostic/operation/media identity fields and fingerprints. Native integration adds actual capture, graphical-device provenance, observed pointer/keyboard actions and current verification receipts. Use existing Tier/active-platform policies; Linux native is inactive/deferred, not silently passed.

## Regression-testing versus historical milestone receipts

M053 depends on **behavioral** compatibility of prior preview/review/asset-authoring code. Run focused tests against current M029/M038/M047/M048/M050/M052 contracts and native integration when affected. Do not put an unconditional `Verify("m048-smoke")` or `Verify("m050-smoke")` whole historical acceptance gate in the M053 suite, or repeatedly refresh full past-milestone receipts simply because an unrelated file changed.

Do not change the current M048/M050 ready contracts, bypass active obligations, reopen completed human reviews, or rewrite historical approval records. Their currently unfinished closure remains separately governed by their own milestones. The broader engineering receipt dependency/fingerprint refactor is **not** included here; its future direction is indexed in `docs/MILESTONES.md` and non-authoritative research. An M053-focused regression can be green without claiming M048/M050 milestone closure.

## Human review and completion

M053 has its **own** required visual/UX review of the actual migrated surfaces, separate from M048/M050/M052 approvals. Use current canonical `.review/` request/record commands; no direct record writes and no fake approval. Provide real launchable workbench, asset-workbench and each image/animation/audio preview fixture with a bounded reviewer path. Register individual review questions where needed for meaningfully distinct subjective experiences; their execution must conform to the simple-review per-item interaction bound. The actual M053 UI must be part of the judged content, not a synthetic M052 stress screen.

Automated quality, behavior, schema, integration and graphics readiness pass before review. Review-check is a distinct Tier 5 gate. Completed historical reviews remain untouched. Implementer terminates `AWAITING HUMAN REVIEW` if only the new human decision remains, and writes durable final criterion/evidence proof only after genuine completion.
