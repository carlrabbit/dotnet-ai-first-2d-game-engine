# ADR-0019 — Input Is Tick-Bound Semantic Data and Replay Uses Resolved Frames

## Status

Proposed for Milestone 016. Accept when implementation is accepted.

## Context

M015 introduced an interactive client while keeping gameplay input outside rendering authority. Direct platform polling in behaviors would couple simulation to raylib, presentation frame rate, and hardware.

Mouse and controller semantics also need design even when physical hardware cannot be tested.

## Decision

Use:

```text
backend-neutral raw samples
→ authored action map
→ accumulator
→ tick-bound semantic InputFrame
→ immutable behavior query
→ normal runtime intents
```

Support keyboard, mouse, and controller through contracts, synthetic sources, validation, and replay.

Do not require new physical raylib mouse/controller integration.

Keep pointer state separate from actions.

Record and replay consumed semantic frames rather than raw device state.

## Consequences

Benefits include deterministic headless tests, hardware-independent replay, replaceable adapters, and explicit pause/step semantics.

Costs include schemas, accumulator semantics, dead-zone/pointer rules, and more artifacts. Physical adapter breadth remains incomplete by design.

## Rejected alternatives

Direct raylib polling in behaviors, digital-only input, pointer as ordinary action, raw samples as replay authority, mandatory controller hardware tests, remapping UI, multiplayer, rollback, or save/load in M016.
