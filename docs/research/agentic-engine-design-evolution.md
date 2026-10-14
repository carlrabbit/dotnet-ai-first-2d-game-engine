# Agentic Engine Design Evolution — Conversation-Led Reconstruction

## Status

Reconstructed planning context; non-authoritative.

## Question

How did the engine's core design thesis evolve in planning conversations, which ideas became durable project authority, and which early assumptions were superseded?

## Core thesis

The project started from a strong inversion of the usual game-engine model:

```text
humans design and review
agents implement, modify, validate and iterate

therefore the engine should optimize for:
observability
controllability
reproducibility
cheap verification
structured authority
```

This led to the durable framing:

```text
domain-specific compiler
+
deterministic runtime
+
evidence-producing workbench
```

rather than:

```text
editor-first engine
+
automation added later
```

The graphical editor was deliberately treated as optional rather than foundational.

## Headless / CLI / API first

Early planning repeatedly converged on:

- headless operation as a first-class path;
- semantic CLI/API operations rather than GUI automation;
- deterministic ticks, seeds, ordering and replay;
- explicit scenarios and validations;
- machine-readable diagnostics and artifacts;
- stable IDs rather than incidental filenames, hierarchy positions, screen coordinates or display text.

The intent was not "no GUI". It was:

```text
GUI is one client
semantic engine operations remain primary
```

That distinction remains relevant to workbenches and the new UI track.

## Structured evidence over screenshot inference

The project repeatedly rejected the idea that agents should understand game state mainly by looking at screenshots.

Preferred evidence includes:

- entity/component state;
- semantic commands and factual events;
- runtime snapshots;
- projections;
- path/navigation information;
- active operations;
- save identity;
- deterministic counters;
- structured diagnostics;
- screenshots and captures as supplementary perceptual evidence.

The durable split became:

```text
machine-checkable semantic facts
→ structured evidence

visual/audio/UX quality
→ human review
```

Screenshots are useful evidence, but not the semantic oracle.

## Stable identity as a design principle

Stable identity was considered foundational across:

- entities;
- components;
- commands;
- events;
- assets;
- maps;
- controls;
- scenarios;
- saves;
- review subjects;
- validation obligations.

The repeated lesson was that agentic systems become fragile when semantic meaning is reconstructed from display text, current position, filenames, enumeration order or incidental runtime objects.

Later corrective asset/runtime milestones reinforced this principle mechanically.

## Runtime ownership and immutable observation

Planning moved away from ad hoc subsystem-local state toward a model where:

- runtime owns entity identity/lifecycle and authoritative mutable component state;
- systems issue validated semantic mutations;
- immutable snapshots support evaluation and inspection;
- behaviors/evaluators read snapshots and propose intent/mutations;
- rendering and diagnostics consume projections/read models;
- authored maps/content remain static inputs rather than mutable runtime authority.

This became especially important once persistence, spatial behavior and multi-fidelity simulation were introduced.

## Renderer independence

The recurring rendering direction was:

```text
authoritative state
→ semantic / backend-neutral projection
→ renderer adapter
```

Raylib was selected as the pragmatic first real adapter because it was small, direct and useful for proving the agentic runtime loop.

Early discussion considered MonoGame as a stronger long-term .NET substrate and SDL3/Silk.NET as higher-control alternatives. Those were evaluation directions, not current authority.

The durable conclusion is the renderer boundary, not the old ranking of libraries.

Current UI direction strengthens the same pattern:

```text
semantic UI model
→ headless inspection
→ Raylib
→ future renderer
```

## Debug versus packaged representation

Early planning explored distinct development and packaged representations:

Development/debug:

- string/stable semantic IDs;
- rich JSON/diagnostics;
- reflection only where acceptable;
- inspection and scenario evidence;
- verbose metadata.

Packaged/release:

- compact IDs;
- generated dispatch/tables;
- binary or optimized resources;
- less diagnostic overhead;
- no semantic dependence on editor/debug machinery.

The key invariant was:

```text
same semantics
different representation
```

The project should not become two engines.

This idea remains useful even where the current implementation does not yet realize every optimization originally imagined.

## Source generators

Source generators were considered a good fit for deterministic, boring plumbing:

- typed IDs;
- registries;
- dispatch tables;
- schema metadata;
- serializers;
- bindings;
- validation descriptors.

They were explicitly not intended to hide core gameplay semantics or create magical behavior.

Planning implication:

Generation is valuable where it removes repetitive glue while leaving the semantic contract inspectable and testable.

## Scenario-first thinking evolved into capability-specific validation

Early conversations sometimes treated scenarios as the central validation mechanism for everything.

That was refined later.

For a capability-provider repository:

- provider capabilities use unit/integration/contract/golden/CLI/determinism validation;
- bounded dogfood proves realistic consumption;
- full consumer gameplay scenarios and subjective review belong only where the capability actually crosses that boundary.

This prevented the engine provider from accidentally inheriting every consumer-game acceptance burden.

## Superseded or narrowed early assumptions

Treat these as historical exploration rather than current project truth:

- MonoGame as the default eventual runtime choice;
- the idea that every engine capability should primarily be proven through game scenarios;
- early loose ECS/CQRS analogies before current runtime ownership contracts;
- using broad debug-versus-release redesign before concrete performance evidence;
- assuming visual editor concepts should drive authoring structure.

## Durable planning implications

Future planning should preserve:

1. semantic authority independent of graphical clients;
2. stable IDs and deterministic ordering;
3. structured evidence before perceptual inference;
4. renderer/read-model separation;
5. one semantic model across execution/representation variants;
6. explicit provider versus consumer/dogfood boundaries;
7. generated plumbing only when semantics remain visible.

## Related current authority

- `docs/specs/project-thesis.md`
- runtime/entity/component/snapshot contracts
- render-projection contracts
- product CLI contracts
- `docs/architecture/agentic-ui-direction.md`
- validation-tier and platform-verification authority

## Freshness

Reconstructed from historical planning conversations through 2026-10-14.

Revalidate if the engine ceases to be capability-provider/headless-first, a future renderer changes semantic ownership, or packaged/debug semantics intentionally diverge.
