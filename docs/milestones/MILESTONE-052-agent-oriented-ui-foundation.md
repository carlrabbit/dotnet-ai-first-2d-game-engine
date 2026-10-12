# Milestone 052 — Agent-Oriented Typed UI Foundation, Deterministic Quality Gate, and Raylib Integration

## Execution profile

| Field | Decision |
|---|---|
| Lifecycle | **ready** — planning contract and obligation ledger seeded 2026-10-12 |
| Implementation mode | `ai-executed-broad` |
| Baseline model | `GPT-5.6 Luna` (current project configuration) |
| Baseline executor readiness | **confirmed** — required architecture/semantics/validation resolved below and in the M052 UI contract |
| Decision preservation | **confirmed** — no implementation decision requires the planning chat |
| Execution tractability | **confirmed** — one coherent capability; implementer decomposes into resumable work packages without compressing obligations |
| Guide version | 0.9.1, repository-wide base + artifact-first-runtime; engine-libraries dotnet-library for reusable UI; product-cli profile only if extending supported product CLI |
| Ledger | `.execution/M052.md`, planning-seeded before `ready` |
| Target platform/locus | Windows, PowerShell 7; portable tests and native graphical integration on active Windows; Linux supported but inactive/deferred |
| Validation depths | Tier 1 focused, Tier 2 repository, Tier 3 integration, Tier 5 milestone-scoped human GUI judgment; Tier 4 only if an existing supported distributable is modified |
| Human review | **required/blocking** for genuine visual readability/interaction quality; not for machine-checkable assertions |
| Permanent completion evidence | `## Completion Evidence` in this milestone, filled by implementation after exact obligation/evidence reconciliation |
| Intended target | complete reusable UI foundation, **not** migration of every workbench |

## Goal

Make GUIs practical for AI-authored .NET games and workbenches: the agent describes a screen in ordinary, small, strongly typed C#; the engine provides correct layout, interaction, state projection, presentation, diagnostics and native Raylib rendering. Build a sufficiently complete foundation at once, rather than merely checking the feasibility of a single control. Establish a credible automated GUI quality gate so future workbenches are not dependent on handcrafted coordinates or screenshot-only validation. Deliver functioning software first; the separate M054 evaluation determines whether it measurably improves agent outcomes.

## Contract and target state

The planning-seeded permanent contract is `docs/specs/agent-oriented-ui-contract.md` in this overlay. It is implementation authority, not a discussion proposal. It resolves the C# composition vocabulary, immutable normalized model, retained UI-local session, operation boundary, deterministic layout/measurement, desktop sizing/scaling, focus/input, animation, diagnostics, native integration, and preservation of existing features. `docs/architecture/agentic-ui-direction.md` remains the higher-level direction.

On completion:

1. There is **one** normalized UI model and one authoritative layout/hit-test/session engine. Existing `UiElement`/`UiProjection` (M021) and `UiNode`/`UiHost` (M037) usages may remain as compatible source/API facades, but must not maintain competing authoritative algorithms for overlapping new UI behaviors. Remove or route duplicate engine logic where practical without breaking existing contracts.
2. A normal author writes C# compositional UI, using declared controls and typed size/theme/action concepts. Screens are independently compilable, testable and inspectable outside Raylib.
3. Headless validation mechanically detects unusable layouts, infeasible text/constraints, invalid interaction/focus and inconsistent geometry; emits versioned evidence linked to element identities.
4. Native Raylib shows an actual functional Product Shell menu built through the same model; a separate review-style stress screen exercises rich layout, dynamic operation projections and animation without touching live review persistence.
5. The native UI default uses a **Full HD 1920×1080 design reference**, fitting available desktop work area. Legacy 960×540 and 1120×720 are not product defaults. Layout supports HD, Full HD, QHD, UHD/4K, ultrawide, and UI scaling without fixed-resolution assumptions.
6. The new renderer boundary is portable and read-only. Future next-gen renderer support requires an adapter, not new authored screens. Actual next-gen backend is deferred.

## Required project-local authority

Implementation must begin with this milestone, `.execution/M052.md`, `AGENTS.md` and the directly relevant current repository authority:

- `docs/specs/agent-oriented-ui-contract.md` (new; definitive M052 semantics);
- `docs/architecture/agentic-ui-direction.md` and `docs/architecture/player-and-diagnostics-application-architecture.md`;
- `docs/specs/ui-toolkit-contract.md` and `docs/specs/declarative-ui-and-binding-contract.md` (currently implemented M037/M021 behavior requiring compatibility);
- `docs/specs/render-projection-contract.md` and `docs/specs/raylib-debug-client-contract.md`;
- `docs/specs/simple-human-review-workbench-contract.md` (boundary; do not migrate persistence in M052);
- `docs/ENGINEERING.md`, `docs/engineering/command-contract.md`, `docs/engineering/validation-tiers.md`, `docs/engineering/platform-verification.md` and `eng/platform-verification.json`;
- `docs/HUMAN-REVIEW.md`, `docs/engineering/human-review-workflow.md` if completing required human review.

Relevant observed implementation entry points on planning date: `src/Agentic2D.UI/UI.cs`, `src/Agentic2D.UI/M037ProductShell.cs`, `src/Agentic2D.DebugClient.Raylib/RaylibGameWindow.cs`, `src/Agentic2D.Tools/ReviewSurfaceLayout.cs`, and `src/Agentic2D.DebugClient.Raylib/AssetPreviewRaylibWindow.cs`. These are context, **not** a prescriptive edit allowlist. Inspect current live sources and tests before implementing. No external guide or planning conversation is needed by implementers.

## Resolved architectural decisions

### Authoring, model and state

- One strongly typed C# UI vocabulary from the new contract; no custom ASCII grammar, XAML, CSS, web browser, reflection binding or arbitrary code-evaluation language. New authoring names/roles and behavioral semantics in the contract are fixed; ergonomic method overloads and internal class/file design are executor choices.
- Immutable normalized definitions plus a retained UI-local session. Stable IDs and reconciliation preserve valid focus, typed draft text, scroll positions, pointer capture and selection. Data/application projections are explicit; definitions are recomposed when data changes. No general-purpose reactive binding engine.
- Controls emit typed semantic action requests. UI does not directly execute domain work; existing application operations revalidate when invoked. Async persistence/retry/cancellation remain application owned. Failure/saving/success views reflect confirmed operation state, not assumptions triggered by button clicks.
- Content slots accept read-only game/media preview content with UI engine-owned layout, clips and interaction geometry. No renderer-specific element geometry or application command decisions in Raylib.

### Deterministic layout and quality

- Implement text measurement using declared font-resource metrics, not approximations based on glyph count. Shared font/metric identity is part of deterministic evidence. Diagnostics must distinguish valid intentional overlay/scroll clip from layout defects, and infeasible smaller viewports from a broken supported viewport.
- Enforce stable logical/pixel transform and hit testing. Visibility, modal stacking, focus and input routing use the same resolved geometry and order as headless rendering.
- Versioned machine-readable artifacts capture semantic tree, layout, hit bounds, focus/control state, captured operations and diagnostics with stable IDs and reproducible fingerprints. Required quality checks are true mechanical gates, not warning-only screenshots.

### Desktop sizing (user decision)

- Standard desktop reference: **1920×1080 (Full HD)**; initial native windows fit available monitor work area rather than overflowing titlebar/system chrome. All screens resize.
- Supported desktop validation targets: **1280×720**, **1920×1080**, **2560×1440**, **3840×2160**, **3440×1440**. Include representative 1.0×/1.25×/1.5×/2.0× UI scaling and a 4K/200%-scale comparison to 1080p/100%. Use bounded representative coverage, not a full dimension Cartesian product.
- 640×360 landscape and 360×640 portrait may be negative stress tests; diagnostics, not mobile UI compatibility, are their expected outcome when the desktop UI cannot fit. Do not preserve 960×540/1120×720 as privileged regression sizes.
- World camera/world units are independent of UI scale. OS DPI scaling and UI user scaling must be explicit, testable and not applied twice. Native graphics need not run at 4K if the active host lacks such a display; headless 4K must still be proven.

### Existing surfaces and compatibility

- The existing M037 Product Shell main menu is the **real migration target in M052**. Use the actual application/menu projection and current product semantics, replacing hardcoded native menu layout/selection/hitboxes. No menu simulation substitute is acceptable.
- A separate review-style rich UI stress screen exercises text length, media slot, clipping/scroll, interactive controls, busy/failure/retry projections, modal and animations **without writing or changing durable review state**.
- The current Review Workbench native window initialization must no longer impose 1120×720 as the default desktop window. Until M053 converts its content, a bounded legacy coordinate/layout compatibility path is permissible, but its click regions and visual arrangement must remain correct under that path and the old fixed size may not leak into the new DSL/defaults.
- Existing M021/M037 APIs and all M029/M038/M048/M050 review/workbench/persistence workflows remain functionally compatible. Completed milestone records and approvals are immutable history. M053 owns full workbench transition; M054 owns independent agent-value evaluation.
- Only the Raylib client references raylib-cs. Shared UI, engine runtime, tools and headless validation stay portable/native-free. Renderer projection and animation samples are read-only to adapters.

## Non-goals

No general GUI designer/editor, manually authored ASCII layout grammar, browser/HTML/CSS, XAML, complete accessibility certification, touch/mobile platform release, second native renderer, Windows game export, NuGet packaging, general application command bus, reimplementation of review persistence, migration of all M029/M038/M048/M050 workbench screens, broad historical index/research synchronization, or comparative-agent performance claims. No new game simulation behavior or arbitrary unrelated refactors.

## Acceptance obligations — exact identity and wording

Each `AC-*` row is independently verifiable. Planning seeds precisely these obligations, not implementation tasks.

| ID | Obligation |
|---|---|
| AC-01 | A single normalized, immutable, renderer-independent semantic UI model is the source of truth for new UI composition, layout, hit targets and presentation, with no competing new layout engine. |
| AC-02 | A reusable, compiled, strongly typed C# composition API exposes the agreed containers, content and controls from the M052 UI contract, including stable element/action identity and typed sizing/style semantics, without bespoke ASCII or reflection-driven binding. |
| AC-03 | Definition validation rejects duplicate/invalid IDs, invalid tree/constraint shapes, unsupported structural references and nonfinite sizing before they can generate misleading successful UI evidence. |
| AC-04 | Retained UI-local session state survives compatible screen recomposition by stable identity and releases stale focus/capture/handlers when elements or screens are hidden, disabled, removed or replaced. |
| AC-05 | Deterministic layout supports row/column growth, minimum/preferred/maximum sizing, grid tracks, alignment, margins, padding, overlays, scroll clipping and content slots with a common hit-bound geometry. |
| AC-06 | Layout feasibility analysis emits stable, actionable structural diagnostics for unintentional overlaps, overflow, unsatisfied size constraints and inaccessible essential controls, while allowing explicitly intentional overlay/scroll clipping. |
| AC-07 | Text wrapping/measurement and ellipsis/overflow decisions use a declared deterministic font-metric policy shared with Raylib, and diagnose excessive or unsupported content without character-count heuristics. |
| AC-08 | Native desktop defaults use Full HD 1920×1080 as the design reference, fit actual available desktop work area, remain resizable, and do not retain the legacy 960×540 or 1120×720 sizes as product defaults. |
| AC-09 | The layout and physical/logical pointer transforms correctly handle target desktop resolutions, declared UI scales and OS DPI without double scaling or dependence on world coordinates. |
| AC-10 | Pointer hit testing, press/release/capture, clipping, stacking and modal routing are deterministic, prevent click-through and use the exact same geometry as projection. |
| AC-11 | Keyboard focus traversal, confirm/cancel, text-entry isolation, visible focus and modal trapping/restoration work correctly without gameplay-input leakage. |
| AC-12 | Interactive button/toggle/slider/text/select/list/scroll controls have meaningful values, state transitions and inspectable headless behavior; label/busy/progress/separator stay noninteractive as appropriate. |
| AC-13 | UI activations emit stable semantic action requests through a thin adapter; existing application/domain operations own validation and effects, and controls never assert completed persistence from an unconfirmed click. |
| AC-14 | An application-supplied asynchronous operation-state projection supports truthful busy/success/failure/cancel/retry presentation without UI-owned background job/persistence machinery or a second command bus. |
| AC-15 | Presentation effects have deterministic injected-time sampling, independent of simulation time, with hover/focus/press/busy variants and reduced-motion behavior that does not change operation outcomes. |
| AC-16 | Typed theme/typography/spacing/semantic state tokens and an accessibility baseline provide inspectable roles/names, visible focus, readable critical state, disabled/busy cues and icon alternatives without color-only status. |
| AC-17 | Headless output contains versioned semantic/layout/presentation/interaction/diagnostic artifacts, stable IDs and fingerprints, with reproducible normalized output across equivalent inputs and no native-graphics dependency. |
| AC-18 | The headless GUI quality gate rejects defective supported desktop layouts and invalid interactions while accepting correctly diagnosed undersized negative cases; screenshots alone cannot satisfy this gate. |
| AC-19 | The isolated Raylib adapter consumes the canonical read-only UI projection and presentation samples, renders text/controls/media slots, and supplies native input without owning layout, action semantics or animation authority. |
| AC-20 | The actual Product Shell main menu is composed from its existing application/menu projection through the new UI API and is usable through real Raylib pointer/keyboard navigation without hardcoded native hitboxes. |
| AC-21 | A compiled review-style stress screen uses the public UI API to exercise long content, lists/scroll, modal, busy/failure/retry projection, media slot and animation, without touching canonical review persistence. |
| AC-22 | Existing M021/M037 supported UI entry points and unaffected workbench/review/asset workflows retain their externally observable behavior; transitional compatibility does not create a second authoritative model for new screens. |
| AC-23 | The Review Workbench entry window no longer imposes legacy fixed desktop dimensions and remains functional through its bounded compatibility layout until M053, without rewriting review decisions, queuing, reset or historical records. |
| AC-24 | Renderer adapter interfaces and dependency boundaries permit a future second renderer to consume existing authored UI unchanged; no new native renderer or native dependency in shared libraries is introduced. |
| AC-25 | An independently compiled consumer usage example and current project-authoritative API/diagnostics documentation teach UI authoring, projection, inspection and operation dispatch without private test shortcuts or external guide documents. |
| AC-26 | Bounded automated proof covers representative desktop resolutions/scales, text/constraint extremes, input traces, recomposition, animation-clock transitions, and relevant current product/compatibility regression paths, with concrete artifacts from exercised code. |
| AC-27 | Active-Windows native integration renders interactive Product Shell and stress UI with captured review evidence; unavailable inactive-Linux proof is recorded only as deferred, not reported as passed. |
| AC-28 | The M052-owned required human review judges actual GUI readability, focus/feedback visibility and interaction usability using launchable real Raylib content, and the milestone does not claim COMPLETE without a final acceptable review-check result. |
| AC-29 | Required Tier 1–3 validation and any applicable consumer gate, exact AC/EC reconciliation, and durable criterion-to-proof completion evidence are established without changing historical milestones or fabricating passing results. |

## Selective evidence cases — separate proof paths

These capture distinct behavior paths; their identities and wording are also seeded in `.execution/M052.md`. They do **not** imply an exhaustive test Cartesian product.

| ID | Parent | Independently required evidence case |
|---|---|---|
| EC-04a | AC-04 | A compatible dynamic recompose retains valid focus, scroll, control draft/selection and does not duplicate action subscriptions. |
| EC-04b | AC-04 | Removal/replacement, disable/hide and modal takeover clear invalid focus/capture without stale action delivery or resource leaks. |
| EC-06a | AC-06 | A supported desktop viewport with content/constraint overflow or unintended overlap produces correct failure and offending element/constraint evidence. |
| EC-06b | AC-06 | A declared overlay or clipped scroll content is not incorrectly classified as illegal overlap/overflow. |
| EC-09a | AC-09 | Full HD and 4K/200% representative layout and pointer mapping preserve comparable logical UI semantics. |
| EC-09b | AC-09 | QHD, ultrawide and fractional scaling preserve clipping, hit tests and stable anchoring without double DPI scaling. |
| EC-10a | AC-10 | Native/headless pointer press-release over layered/clipped controls activates only the intended eligible target. |
| EC-10b | AC-10 | Modal blocking, removed/disabled target and pointer-capture cancellation suppress invalid or lower-layer activation. |
| EC-13a | AC-13 | A valid UI action forwards a typed semantic request and the application adapter revalidates current operation availability. |
| EC-13b | AC-13 | A stale-enabled UI snapshot or unsuccessful operation never becomes a successful authoritative domain/persistence state. |
| EC-14a | AC-14 | Application-confirmed async success/working state updates UI busy and completion presentation without blocking the render/session loop. |
| EC-14b | AC-14 | Application-reported error, cancellation and retry are truthfully displayed without lost inputs or duplicate domain requests. |
| EC-17a | AC-17 | Equivalent headless definition/projection/event/time inputs generate stable ordered semantic/geometry/fingerprint evidence across repeated runs. |
| EC-17b | AC-17 | A malformed or unusable screen creates actionable non-success diagnostics with exact offending paths and dimensions. |
| EC-20a | AC-20 | Product Shell pointer selection/activation uses real projected hit targets and current menu availability. |
| EC-20b | AC-20 | Product Shell keyboard focus/traversal/activation uses the same menu projection and routes to existing shell behavior. |
| EC-22a | AC-22 | Legacy M021 UI projection consumers continue producing supported semantic output without public contract regression. |
| EC-22b | AC-22 | Legacy M037 UI/session and unaffected native review/asset workbenches continue their required behavior without historical mutation. |
| EC-27a | AC-27 | Active Windows Raylib Product Shell and stress screen are actually launched/captured from current implementation, not generated mock screenshots. |
| EC-27b | AC-27 | Linux native proof is explicitly classified as inactive/deferred when not executed, never counted as passing Windows or cross-platform evidence. |

## Validation gates and evidence topology

Planning selects the validation *target* and *locus*; implementation chooses concrete tests, suite shards and supporting command internals within these requirements. The new `m052-ui` engineering suite must be integrated with the existing thin-launcher/engineering-host pattern, support bounded `--plan-json`, individual `--shard`, and `--verify`, and produce current fingerprinted receipts. Do not assume success from `--plan-json` or partial shards.

| Gate | Tier; target/locus | Required invocation/actual evidence | Intended obligations/cases |
|---|---|---|---|
| VAL-01 | Tier 1; C# library authoring/normalization; Windows-local portable | Focused unit/compile probes and compiled consumer usage example for valid/invalid DSL, IDs, tree, types; exact source/probe artifacts | AC-01, AC-02, AC-03, AC-25 |
| VAL-02 | Tier 1/3; headless deterministic UI model/layout/text at Windows-local | `pwsh ./eng/suite.ps1 m052-ui --shard headless-layout` (or identified equivalent shard), compare bounded matrix and repeatable evidence; verify errors/intentional clip | AC-05, AC-06, AC-07, AC-08, AC-09, AC-17, AC-18, AC-26; EC-06a/b, EC-09a/b, EC-17a/b |
| VAL-03 | Tier 1/3; scripted headless input/session/controller projections; Windows-local | `pwsh ./eng/suite.ps1 m052-ui --shard headless-interaction` and recorded event traces/operation stubs | AC-04, AC-10, AC-11, AC-12, AC-13, AC-14, AC-15, AC-16, AC-26; EC-04a/b, EC-10a/b, EC-13a/b, EC-14a/b |
| VAL-04 | Tier 3; actual Product Shell, stress screen, Raylib and Windows graphics host | `pwsh ./eng/suite.ps1 m052-ui --shard windows-raylib`; actually launch the native adapter, execute input paths, capture real screenshots and compare to semantic evidence; no fake Raylib screenshots | AC-08, AC-15, AC-16, AC-19, AC-20, AC-21, AC-23, AC-27; EC-20a/b, EC-27a/b |
| VAL-05 | Tier 1/2/3; existing UI and workbench compatibility on active Windows | `pwsh ./eng/suite.ps1 m052-ui --shard compatibility` or equivalent established integration shards, current M021/M037 and review/asset smoke outcomes | AC-01, AC-22, AC-23, AC-24; EC-22a/b |
| VAL-06 | Tier 2; full current repository on active Windows | `pwsh ./eng/build.ps1`, `pwsh ./eng/test.ps1`, `pwsh ./eng/format.ps1 --verify`, and `pwsh ./eng/check.ps1`; if any aggregate is resumable, use planned shard/verify mechanism | AC-22, AC-25, AC-26, AC-29 (regression guard, not generic evidence for UI behavior) |
| VAL-07 | Tier 3; complete M052 suite and current generated receipts on Windows-local | `pwsh ./eng/suite.ps1 m052-ui --plan-json`, then each required `--shard <id>` separately, then `pwsh ./eng/suite.ps1 m052-ui --verify`; preserve artifacts under `artifacts/ui/M052/` and verification receipts | AC-17, AC-18, AC-26, AC-27, AC-29; all independently required EC cases routed to matching shards |
| VAL-08 | Tier 5; active-Windows actual launched Raylib visual/reviewer experience | After valid machine prerequisites: normal M052 review-run; review ID `review.m052.typed-ui-foundation-native-readability-and-interaction`; `pwsh ./eng/review-check.ps1 --milestone M052` establishes acceptance; human decides readability/feel only | AC-16, AC-20, AC-21, AC-28 (human judgment only) |
| VAL-09 | Tier 1; completion topology/project authority/side effects | Exact milestone/ledger AC and EC identity and wording comparison; per-case exercised-evidence audit, direct doc/spec updates, source/dependency boundary audit, completion evidence trace | AC-01, AC-22, AC-24, AC-25, AC-28, AC-29; all AC/EC IDs checked individually |

Shards shown above (`headless-layout`, `headless-interaction`, `windows-raylib`, `compatibility`) are required behavioral partitions. Implementation may subdivide them, but must preserve their independent proof and register an unambiguous current suite plan/verifier. Other relevant M052 code and tests can be arranged freely.

**Tier 4**: M052 changes reusable libraries and local native client, not a supported packaged export by intent; no new distribution gate is presumed. If implementation materially modifies the existing supported Linux game export or another already-supported distributable, it must run the applicable current real-artifact consumer entrypoint gate at the authoritative locus or report the exact missing target. Do not claim a packaged .NET tool or Windows export exists.

**Platform limitations**: Windows/PowerShell 7 is current active proof. Run native integration where graphics capabilities exist. If required active-Windows graphics is unavailable to the implementer, portable headless validation is useful but **not a substitute**; report `BLOCKED` for unavailable machine prerequisites rather than inventing evidence. Supported but inactive Linux-specific proof may be recorded as deferred in `eng/platform-verification.json`. Native 4K display is not required to validate 4K headless geometry.

**Human review**: A required M052 simple human review must show a launchable actual GUI with at most two deliberate reviewer interactions to reach each subjective question. Use the existing milestone-scoped review infrastructure. Do not ask the reviewer to determine mechanically decidable fit, bounds, focus routing or persistence correctness. A valid result may be `AWAITING HUMAN REVIEW` until the owner approves. Never fabricate approval or alter completed historical review records.

## Completion and change boundaries

Implementation owns API internals, module split, tests, concrete work packages, validation command implementation and all evidence execution. It may edit new and existing source and directly relevant docs as required; the listed files are not a closed edit list. Direct documentation should index the new UI contract, authoring recipe, supported resolutions/scaling and `m052-ui` suite, and truthfully distinguish legacy M037/M021 behavior from new M052 behavior. Defer broad stale milestone/README archaeology and M053 workbench indexing to the existing synchronization flow rather than claiming those migrations happened.

Source quality is not enough to declare M052 complete: all `AC-*` obligations, every selected `EC-*` case, required suite verifier and active-Windows graphical evidence must pass, followed by an acceptable milestone-scoped human review decision. The executor must preserve the planning-seeded ledger rows/wording; derive bounded work packages without collapsing them, attach target-specific results, and make a fresh completion audit. Only then populate `## Completion Evidence` below with a compact criterion/evidence-case -> source/artifact -> gate/actual-result table. If human judgment alone remains pending, terminal result is `AWAITING HUMAN REVIEW`; if a material new architectural choice appears, return to planning instead of inventing policy.

## Completion Evidence

*Planning placeholder, not a passing claim. Implementation fills durable criterion-by-criterion and evidence-case-by-evidence-case proof only after current execution, gate verification and required human approval. Do not alter planning acceptance wording when recording historical completion.*
