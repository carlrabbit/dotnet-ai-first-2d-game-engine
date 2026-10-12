# Agent-Oriented UI Direction

## Authority and status

Project-wide architectural direction for the next UI-capability milestone and subsequent workbench adoption. This document fixes boundaries and authoring policy, **not** the public DSL API, concrete widget catalog, or implementation mechanics. Existing implemented UI contracts remain authoritative for existing functionality until an implementation milestone explicitly evolves them.

## Objective

Make graphical interfaces practical for autonomous agents to create, modify, inspect and validate, while preserving headless-first engine semantics and minimizing incidental GUI code and independently managed state.

Building mechanically sound software is ordinary engineering. The distinctive research question is whether the resulting abstractions improve **actual agentic game-development outcomes**. Implement coherent software capabilities before evaluating their agentic effectiveness; do not substitute tiny feasibility demonstrations for complete architecture. Evaluate agent performance with unfamiliar, realistic tasks and comparison against current authoring mechanisms.

## Targets and authoring

- Targets: headless semantic rendering/inspection, the isolated Raylib adapter, and a future next-generation rendering adapter.
- **Primary authoring surface: a small, strongly typed, compositional C# UI DSL** over a renderer-independent UI model. Prefer familiar C# constructs and compile-time types. A separate ASCII/Unicode layout grammar, ASCII-art compiler, XAML, WinForms, WPF and a web/CSS layout engine are not target authoring surfaces.
- Retain a single normalized semantic UI model so authors can inspect and validate the UI independently of the chosen renderer.
- The typed DSL should express composition, containment, anchors, sizing/growth and constraints, text/content, controls, semantic styles, and references to application operations. Layout solver behavior, measurement, overflow policies, hit testing and accessibility fundamentals are deterministic toolkit responsibilities, not agent-generated per-screen algorithms.
- ASCII diagnostics may be generated from the normalized UI model as a **read-only inspection aid** if useful; hand-authored ASCII is not implementation authority.
- No decision is made here about exact method names, syntax, or list of controls; those are M052 planning decisions and must be settled in its ready milestone.

## State and command boundaries

1. Business and application state changes execute through the **existing authoritative semantic commands or explicit headless application operations**. Do not create a second general-purpose command bus or duplicate domain rules in UI callbacks.
2. Complex workflows such as asynchronous review persistence, retry, reset and cancellation belong to application-owned controllers/services that can be exercised without graphics. Their lifecycle and operation capability are projected to the UI.
3. GUI input invokes the existing operation through a thin adapter. Operations revalidate at execution time because a visible enabled state is only a snapshot. UI cannot declare persistence successful before the operation reports completion.
4. Local navigation, focus, hover, pointer capture, scrolling and temporary text input are UI-local state, not simulation commands. UI-local actions should remain lightweight and inspectable.
5. Visual transitions, spinners, feedback, pressed/hover animations and decorative effects are presentation-only. A presentation clock independent of the simulation clock allows animations when gameplay is paused; operation completion is independent of both clocks.
6. Renderer adapters do not own domain workflow decisions, state transitions or layout algorithms. Rendering stays read-only.

## Validation and renderer equivalence

- Equivalent UI definition, inputs, theme/content, viewport and projection produce equivalent headless layout, hit targets and semantic output.
- Explicitly define overflow/unsatisfied constraints, text growth, visibility/focus, clipping, hit testing, event routing, animation sampling and UI scaling in the future UI contract.
- Structured headless evidence should expose bounds, capabilities, focus, interaction results, presentation states and diagnostics. Test meaningful sizes, text lengths and asynchronous state transitions; do not treat screenshot inspection as a substitute for mechanical checks.
- Future Raylib and next-generation rendering adapters consume the same semantic model and presentation patches; no promise of byte-identical raster output is implied.
- Evaluation of agentic value must use actual agents with the documented authoring surface and real game/workbench tasks, including creation, modification and regression repair. Capture correctness, human intervention, framework escape hatches and comparative outcomes. Do not assert improvement merely because the software builds or passes its own tests.

## Scope sequencing

- **M051**: Adopt guide system 0.9.1 and preserve these planning decisions in project authority; no UI implementation.
- **M052**: Implement a sufficiently complete typed-DSL UI foundation, deterministic layout/interaction and presentation runtime, headless evidence and Raylib adapter, with a future-renderer port boundary.
- **M053**: Migrate actual workbench operations and views through the new UI foundation, respecting concurrent asset-preview/review work.
- **M054**: Reproducible comparative agentic-effectiveness evaluation on the real capability; evaluation planning should inform M052 diagnostics before implementation.

Milestone identifiers after M051 are planning reservations, subject to current repository milestone allocation. M052 and later are not implicitly authorized for implementation by this document.
