# Agent-Oriented UI Research Context — Conversation-Led Reconstruction

## Status

Active planning/research context; non-authoritative.

Current implementation direction is authoritative in `docs/architecture/agentic-ui-direction.md`. This document preserves the reasoning that led there and the empirical question the M052–M054 track is intended to answer.

## Question

What problem are we actually trying to solve with the new UI direction, and which parts are software architecture versus research hypotheses?

## Trigger

Workbench development exposed a practical problem:

Conventional GUI implementation creates too much incidental state and code for agents to reliably modify and validate.

A seemingly simple control can depend on:

- domain/application state;
- local UI state;
- async operation state;
- enabled/disabled reason;
- status text;
- hover/press/focus state;
- animation;
- error/retry state;
- persistence completion.

If these concerns are hand-wired separately in each screen, the implementation becomes difficult for both agents and humans to reason about.

## Rejected framing: "just use XAML"

The UI direction intentionally moved away from importing a traditional desktop/web authoring model.

Targets are:

- headless semantic rendering/inspection;
- Raylib;
- a future next-generation renderer.

The desired abstraction should fit agentic game/workbench development directly rather than wrapping XAML, CSS, browser layout or a conventional editor framework.

## Typed compositional C# DSL

The preferred authoring direction became a small strongly typed compositional C# DSL over a renderer-independent semantic UI model.

Why:

- familiar to .NET implementation agents;
- compile-time structure;
- easy connection to typed operations/projections;
- lower syntax/tooling overhead than inventing an external markup language;
- source remains ordinary code and diffable;
- semantics can be normalized and inspected headlessly.

ASCII/Unicode layouts may be useful generated diagnostics, but not authoring authority.

## Separate three kinds of state

The UI discussion clarified three distinct layers.

### 1. Application/domain operation state

Examples:

- save currently executing;
- review persistence in progress;
- validation retry;
- operation succeeded/failed;
- action currently allowed and why.

This belongs to application controllers/services and must be testable without graphics.

### 2. UI interaction state

Examples:

- focus;
- hover;
- pressed state;
- pointer capture;
- scroll offset;
- temporary text editing;
- local navigation.

This belongs to the UI runtime.

### 3. Presentation-only state

Examples:

- spinner phase;
- fade;
- pressed animation;
- hover transition;
- decorative interpolation.

This must never become business authority.

The important rule is:

```text
operation completion drives presentation
presentation never fabricates operation completion
```

## Semantic operations instead of callback-owned logic

UI controls should project and invoke authoritative application operations.

A control needs structured information such as:

- stable operation/action identity;
- enabled/disabled state;
- reason;
- current progress/busy state;
- result/error;
- content/text projection.

Execution revalidates at invocation time because displayed enabled state is only a snapshot.

Do not duplicate business rules in renderer callbacks.

## Deterministic layout and interaction

Agents should not have to author bespoke layout/hit-testing algorithms for every screen.

The toolkit should own deterministic behavior for:

- composition/containment;
- sizing/growth;
- constraints;
- measurement;
- overflow;
- clipping;
- hit testing;
- focus/event routing;
- scaling;
- text growth;
- animation sampling.

The headless representation should expose resulting bounds, capabilities, diagnostics and interaction outcomes.

## Micro-interactions are part of the model, but not the domain

The user explicitly raised micro-interactions/animations as necessary game UI behavior.

The resulting direction is not to remove them. It is to model them as presentation semantics on top of stable UI/application state.

A presentation clock may continue while simulation is paused.

Async operation completion is independent of both simulation time and decorative animation time.

## Generic infrastructure versus context-specific surfaces

M050's asset-preview UX problem provided a useful concrete lesson for the UI track:

```text
shared implementation primitives
do not imply
one union-of-everything visible interface
```

A generic toolkit should make context-specific screens easier to express, not force all capabilities into one generic visible control surface.

## The software is not the experiment

A major planning correction emerged in conversation:

Do not call every uncertain engineering direction an "experiment".

Much of the UI software can be designed and implemented to architectural completeness upfront.

Agents make implementation comparatively cheap; tiny feasibility spikes can create more churn than value.

The distinctive empirical question is:

> Does this UI authoring/inspection model materially improve real agentic game-development outcomes?

Therefore the intended sequence is:

```text
planning/architecture
→ sufficiently complete capability
→ mechanical validation
→ real workbench/game migration
→ comparative agent evaluation
```

This is why the current track separates:

- M052: build the capability;
- M053: use it for real workbench operations;
- M054: evaluate agentic effectiveness.

## Evaluation should test agents, not self-description

A valid evaluation should use unfamiliar, realistic tasks and compare the new surface with the current authoring mechanism.

Useful outcome dimensions include:

- task correctness;
- time/turns/tool calls;
- human intervention;
- regression rate;
- amount of framework escape;
- amount of incidental UI code;
- ability to diagnose failures from headless evidence;
- modification/repair performance, not only greenfield creation.

The UI framework passing its own tests does not establish agentic benefit.

## Open questions

Still appropriate for future planning/evaluation:

- exact minimal widget/control vocabulary;
- how much automatic layout is enough;
- what semantic style/theme system agents handle best;
- how to represent complex repeated/list/tree/table structures;
- best structured headless diff/evidence for UI changes;
- how much animation should be declarative versus imperative;
- how a future renderer consumes the same presentation model;
- whether real agents actually use the abstractions instead of bypassing them.

## Related current authority

- `docs/architecture/agentic-ui-direction.md`
- current application-operation/input/render-projection contracts
- M052–M054 planning reservations

## Freshness

Reconstructed from the October 2026 UI planning conversations.

Revalidate as M052/M053 implementation exposes concrete usability/architecture problems and after M054 comparative evidence exists.
