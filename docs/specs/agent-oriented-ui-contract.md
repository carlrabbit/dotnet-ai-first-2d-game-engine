# Agent-Oriented UI Contract — M052 Foundation

## Authority and applicability

This document is the project-wide product contract for the typed C# UI foundation introduced by M052. The M052 ready milestone owns sequencing, evidence, and review obligations. `docs/architecture/agentic-ui-direction.md` owns the higher-level architectural boundaries. `docs/specs/ui-toolkit-contract.md` and `docs/specs/declarative-ui-and-binding-contract.md` describe existing M037/M021 surfaces and remain valid for legacy consumers until explicitly transitioned; where new M052 APIs are used, this contract owns their semantics. A compatibility facade may preserve legacy callers but may not create a second independent layout/input engine.

## Purpose and primary API

An agent authors a working interface in strongly typed, compositional C#, not in a custom ASCII grammar, XAML, JSON UI documents, a web/CSS runtime, or screen-specific geometry. Core UI objects remain backend-neutral and testable without native graphics. Required C# authoring vocabulary (concept names are stable public concepts; ergonomic overloads and concrete data types are implementation choices within these semantics):

- Composition: `Ui.Screen`, `Ui.Row`, `Ui.Column`, `Ui.Grid`, `Ui.Overlay`, `Ui.Margin`, `Ui.Scroll`, `Ui.ContentSlot`.
- Content: `Ui.Text`, `Ui.Title`, `Ui.Separator`, `Ui.Image` (resolved asset reference; may be rendered as an explicit placeholder when unresolved), `Ui.Progress`, `Ui.Busy`.
- Interaction: `Ui.Button`, `Ui.Toggle`, `Ui.Slider`, `Ui.TextField`, `Ui.Select`, `Ui.List`, `Ui.Modal`.
- Typed layout and appearance: immutable sizing/track/constraint values, alignment, padding/gap, theme roles/tokens, state and semantic action IDs. Ordinary screen code must not need pixel coordinates, manual input hitboxes, or Raylib types.

The reusable authoring surface includes a compiled example with conventional nested C# calls, stable element IDs and explicit action IDs. Fluent syntax or convenience builders are optional but must normalize to the same model; they are not a second DSL. Invalid constructs fail at compilation or normalization with meaningful diagnostics. No runtime reflection binding/expression language or general command bus is introduced.

## Definition, session, and operation boundaries

1. A UI definition is immutable after normalization: a tree of stable, unique element IDs; typed node kinds; semantic content; layout constraints; focus/role information; theme references; and application action references. Reject duplicate IDs, invalid layout specifications, unresolved required structural references, cycles, and unsupported input/operation declarations.
2. A `UiSession` or equivalent retained, headless controller owns **only** local interaction state (focus, hover, pressed/captured pointer, selection, editable text draft, scroll offsets, modal navigation) keyed by stable IDs. Screen recomposition updates semantic content and available actions without abandoning valid local state. Removed/hidden/disabled elements release focus/capture and detach resources. No stale click or leaked handler after replacement.
3. Application services/controllers own business state, persistence, async work, retries/cancellation, and results. The UI receives immutable projections of their status (idle/working/succeeded/failed/cancelled as applicable) and emits semantic action requests (action ID, source ID, supplied control value). A thin host adapter dispatches requests to existing application operations; it must revalidate availability at execution time. A disabled snapshot is not authorization. UI never marks persistence successful before application confirmation.
4. Ordinary UI interaction must not advance a game simulation clock or route gameplay input when captured by UI. Local navigation/input do not become simulation commands. A distinct injected presentation clock drives visual effects, including while the simulation is paused.
5. A content slot is a typed rectangular semantic surface for game/media/preview content. The native adapter may draw a read-only slot payload, but the UI engine retains sole authority for slot layout, clip and hit bounds. No domain/media renderer owns UI geometry.

## Deterministic layout

Logical UI layout uses a top-left origin, right/down positive, stable ordering and explicit viewport dimensions; physical pixels are produced by an explicit viewport/scale transform. UI layout is independent of the world camera/world-units contract.

- Containers: row/column intrinsic measurement and weighted growth; grid fixed/content/grow tracks; margins, padding, gaps, alignment; overlay with declared layering; scroll view with bounded viewport, scroll range, clipping and content offsets; modal overlay/focus layer.
- Sizes: finite nonnegative fixed, content/auto, grow weight, minimum and maximum limits. Reject invalid constraints and nonfinite values. When there is no feasible arrangement, return structured unsatisfied-constraint diagnostics; do not silently squeeze below minimum or mark a clipped essential control valid.
- Default ordinary row/column siblings do not overlap. Intentional overlay overlap and intentional viewport clipping are legal **only** where the relevant node explicitly declares overlay/clipping semantics. Hidden nodes do not occupy layout or receive input. Disabled nodes may retain layout but do not activate.
- Text measurement and wrapping use deterministic font-resource metrics matching the declared Raylib font configuration (same asset/metric policy and fallback). No constant-width-per-character guess for actual measured layout. Respect line breaks, Unicode scalar/glyph fallback, and max-width/height policies. Font-size and UI-scale changes invalidate measurement as needed.
- The renderer cannot make its own independent layout or hit regions. Hit testing and clipping use the same resolved geometry, stacking order and scale transform as visual projection.

## Viewport and scaling policy

**Default desktop design reference: Full HD 1920×1080 client viewport at 100% UI scale.** No existing 960×540 or 1120×720 dimensions are product defaults or mandatory regression resolutions. Initial native window prefers Full HD but must fit its available desktop work area, including decorations and usable bounds; screens remain resizable. An explicit viewport is injectable for offscreen/headless tests, regardless of monitor size.

The supported desktop layout target has a minimum **1280×720** viewport. Headless layout/evidence must cover 1280×720, 1920×1080, 2560×1440, 3840×2160, and 3440×1440, with effective UI scales 1.0, 1.25, 1.5, and 2.0 using a deliberately bounded representative matrix rather than full Cartesian expansion. At least one resolution must use 200% UI scaling. A 640×360 compact viewport and 360×640 portrait viewport are optional *negative/stress* targets: a clear minimum-size/constraint failure is correct for desktop-only interfaces. This milestone does not claim mobile/touch-platform support.

Keep physical framebuffer dimensions, logical viewport, effective UI scale and (if present) OS DPI adjustment explicit. The transformation from physical pointer position to logical UI position is the mathematical inverse of layout-to-physical rendering; avoid double application of DPI scaling. UI scaling must not change game-world coordinates. At 4K/200% vs 1080p/100%, comparable content should have equivalent proportions, not necessarily identical pixel values. A larger viewport must not cause fixed-size UI to float arbitrarily far away or leave oversized unusable whitespace; responsive sizing and sensible max content width are allowed.

## Interaction and accessibility semantics

- Pointer activation requires enabled/visible hit target and a consistent press/release policy (pressed on target, released appropriately); pointer capture ends after release/cancel, disable, removal or modal takeover. Overlays/modal/back-to-front ordering prevent click-through.
- Focus traversal is deterministic and restricted to current visible enabled focusable controls. Confirm/cancel and arrow-key behavior are control-appropriate. Text entry captures printable input and relevant editing keys and does not leak to gameplay. Scrolling, clipping and keyboard visibility must agree.
- Buttons, toggles, sliders, select/dropdowns, editable fields, lists and scroll views have actual behavior and inspectable selected/current values; busy/progress and labels are noninteractive unless explicitly declared otherwise. A modal traps focus and prevents lower-layer input, and can restore a valid previous focus when dismissed.
- Accessibility baseline: semantic roles and names, visible keyboard focus, non-color-only critical state, textual alternatives for meaningful icons/images, disabled and busy semantics, sufficient primary target size at supported desktop scales, and reduced-motion setting. This is not an accessibility-certification claim.

## Presentation, styles and animation

Theme roles/typography/spacing/interaction variants are semantic, typed references. Required styling includes normal, hovered, focused, pressed, disabled, selected, busy, error, and success-reported variants where applicable. Colors are presentation data, never sole critical status.

Use a small finite set of transitions (hover, focus, press, busy pulse/spinner, appearance/disappearance) with deterministic sampling from injected presentation time. Animations must not change business state, activation geometry, or operation completion. A reduced-motion flag replaces motion with static feedback and avoids flashing. A paused simulation does not pause an active UI spinner. All rendered effects are sampled from UI presentation state, not hidden Raylib-only logic.

## Normalized output and diagnostic artifacts

Headless execution of a definition/projection/session returns one canonical normalized UI model and read-only, stable-order inspection artifacts, with versioned schemas and stable element paths. At minimum:

- structural tree with IDs, roles, text/content references, capabilities, action IDs and relevant operation-state projection;
- measured/resolved logical and physical bounds, viewport, scale, layer, visible region, clip region, scroll extent, focused/captured/hovered element, current local control values, hit targets and presentation-clock sample;
- event trace for scripted pointer/keyboard/text/resize/recomposition/operation-result actions, including accepted/ignored input and emitted action requests;
- diagnostic list with stable codes, severity, offending IDs/paths, constraint context and measured/available values; distinguish valid intentional overlay/clipping from illegal overlap, overflow, missing content/metrics and inaccessible essential controls;
- reproducible fingerprint for equivalent definitions, content/metrics, viewport/scale, projections and injected input/time. Avoid paths, clock-now, nondeterministic traversal order and native handles in semantic equality.

A headless validation gate fails on structural errors, unintentional overlap/clipping, invalid hit targets, unhandled impossible layouts, or infeasible essential content at *supported desktop targets*. For undersized negative targets, the gate passes only when it correctly detects and reports the unsatisfied constraint. Exact pixel equality across different renderer backends is not required.

## Native integration and portability

`Agentic2D.UI` and headless evidence may not depend on raylib-cs or native graphics. The isolated `src/Agentic2D.DebugClient.Raylib` adapter consumes the common resolved read-only draw/presentation projection; it may manage textures/fonts/native input resources and windows, not UI geometry, controls, operation lifecycles or domain semantics. Define a backend-neutral draw-command/batched-presentation boundary so the future next-generation renderer can implement an adapter without changing authored screens. Only Raylib is implemented by M052.

At least one real Product Shell menu flows from the existing `ProductMenus`/`ProductShellSession` projection through the DSL/session/layout/headless evidence/Raylib, with actual pointer and keyboard navigation. Its old Raylib-specific hardcoded hitboxes may not be used as the new UI authority. At least one review-style **synthetic stress experience** uses the same public API and covers long text, preview/media slot, modal, list/scroll, busy and failure/retry projection; it must not emulate review persistence or write `.review/` decisions. Existing M029/M038/M048/M050 workflows and persistence stay behaviorally unchanged for M053 migration.

Eliminate fixed-window initialization defaults in Product Shell and Review Workbench entry points. For still-legacy workbench content before M053, a bounded compatibility layout/transformation may be used; don't accidentally claim its internals have been migrated or break its review interaction/hit targets. Historic milestone/test records remain untouched; no reapproval of past human reviews.

## Evidence, future work and restrictions

A real compiled consumer example and documented authoring recipe must be usable without changing the core toolkit. Tier 1–3 checks and current-Windows Raylib GUI validation are implementation proof, not evidence that agents are more effective. M053 owns migrating production Review Workbench, Asset Workbench, and structured image/animation/audio preview screens onto typed composition and canonical resolved layout while retaining existing persistence and provider operations. Application adapters translate semantic UI requests into existing operation APIs; Raylib draws media only inside projected content slots. M053 defines production input mappings and evidence in `docs/specs/real-workbench-agent-ui-migration-contract.md`. M054 owns comparative unfamiliar agent tasks and measurement of intervention, correctness and escape-hatch frequency. No new general-purpose command bus, web GUI, editor/toolkit designer, mobile distribution, Windows game export or compiled next-gen renderer in M052.

## Authoring and inspecting a screen

The public API is in `Agentic2D.UI`. Define stable IDs, action IDs and typed controls in ordinary C#, normalize once, create a headless session for an explicit viewport, and pass its read-only draw projection to a renderer adapter:

```csharp
var operation = new UiOperationProjection("world.save", AgentUiOperationStatus.Working, "Saving");
var definition = UiDefinition.Normalize("world-settings",
    Ui.Screen("settings",
        Ui.Margin("settings.gutter", UiInsets.All(24),
            Ui.Column("settings.form",
                Ui.Title("settings.title", "World settings"),
                Ui.Toggle("settings.pause", "Pause on focus loss", "settings.pause", true),
                Ui.TextField("settings.name", "World name", "settings.name", "North meadow"),
                Ui.Button("settings.save", "Save settings", "settings.save"),
                Ui.Busy("settings.status", "Save state", operation)))));
var viewport = new UiViewport(1920, 1080, UiScale: 1, OsDpiScale: 1);
var session = new UiSession(definition, viewport);
var semanticEvidence = session.ToEvidence();
var rendererCommands = UiDrawProjection.Create(session);
```

Use `UiSession.Dispatch` for pointer, keyboard, text, scroll, resize/recomposition and injected presentation-time events. Read semantic requests with `DrainActions`; route them through the existing application operation and recompose with its confirmed `UiOperationProjection`. Control activation is only a request. `UiLayoutEngine.Resolve` can be used by headless tests directly with a `UiViewport`.

`UiLayoutResult` is schema `agentic2d.ui.layout.v1`; `UiSession.ToEvidence()` is schema `agentic2d.ui.evidence.v1`. Both retain ordered element identities, semantic roles, logical and physical rectangles, visible clips, hit capability, focus/capture, local drafts and values, operation projection, diagnostics and a deterministic fingerprint. Diagnostics include `UI2001` for unintended interactive overlap, `UI2002` for a viewport below 1280×720, `UI2003` for text height overflow, `UI2004` for container overflow, `UI2005` for invalid grid references at resolution, and `UI2006` for an unsatisfied minimum size. An intentionally small negative viewport is valid evidence only when its error is reported; intentional overlay and scroll clips remain explicit in the normalized tree.

The public, independently compiled usage example is `consumers/examples/Agentic2D.UiConsumer/`. Run `pwsh ./eng/suite.ps1 m052-ui --plan-json`, execute each listed shard separately, then `pwsh ./eng/suite.ps1 m052-ui --verify`. The suite preserves structural JSON under `artifacts/ui/M052/`; actual Raylib PNG captures are human review evidence only.

The desktop reference is Full HD (1920×1080). Headless target coverage includes 1280×720, 1920×1080, QHD, UHD/4K and 3440×1440 with representative 1.0×, 1.25×, 1.5×, 2.0× UI scales and explicit OS DPI. Logical pointer conversion divides physical coordinates once by the effective UI and OS DPI scales; game-world units do not enter the transform.
