# ADR-0020 — Animation Produces Typed Presentation Patches

## Status

Proposed for Milestone 017. Accept when implementation is accepted.

## Context

The engine needs sprite and generic property animation without editor-centric object paths or a second runtime mutation system.

## Decision

Animation definitions target a finite registry of typed visual properties using stable visual part IDs.

```text
visual defaults → base clip replacement → optional overlay replacement → animated render projection
```

Use integer runtime ticks, explicit selection keys, one base plus one overlay, once/repeat clips, step/linear sampling, explicit directional clip IDs, and presentation-only markers.

Once clips hold final values. No automatic return or clear occurs. Markers do not issue gameplay commands.

## Consequences

The system is deterministic, headless, agent-readable, and independent of raylib, but explicit directional clips and scalar property channels are verbose and no blend-tree convenience exists.

## Rejected alternatives

Arbitrary property paths, runtime component animation, automatic one-shot fallback, variant merge semantics, unrestricted layers, gameplay method tracks, and raylib-owned animation logic.
