# Tick-Bound Input Frame Contract

## Authority

Authoritative for raw samples, accumulation, semantic frames, pointer state, runtime input queries, and running/pause/step/reset behavior.

## Pipeline

```text
raw samples
→ accumulator
→ binding evaluation
→ semantic InputFrame
→ immutable runtime query
→ behavior intent
```

Collection and mapping never mutate runtime.

## Raw samples

Raw samples carry stable sequence, source/device IDs, device kind, presentation sample index, controls/values, coordinate space, provenance, and diagnostics.

Samples may occur more frequently than runtime ticks.

## Accumulator

Retains current down and analog state, queues press/release edges, and accumulates pointer delta and wheel impulses.

One semantic frame is produced when a runtime tick requests input.

After production, one-shot state is consumed; held/down and latest analog state persist.

## InputFrame

Contains schema/version, tick, frame sequence, source ID, map ID/revision, digital/scalar/vector actions, pointers, provenance, and diagnostics.

Exactly one frame is consumed per applicable tick.

## Pointer

Spaces:

```text
window
logical-viewport
world
```

Outside logical viewport is explicit and not clamped. World conversion uses read-only camera data.

## Digital transitions

```text
inactive → pressed → held → released → inactive
```

A press and release between ticks is retained and represented deterministically according to the accumulator evidence.

## Runtime query

Behaviors query semantic values by action/pointer ID. Missing values return inactive/zero. Raw native state is never exposed.

## Runtime modes

- Running: one frame per tick.
- Paused: retain pending state without consumption.
- Step one: consume one frame.
- Step ten: consume ten; one-shot pending state appears only in the first unless new samples are inserted.
- Complete: consume no further gameplay frames.
- Reset: clear previous, held, queued, analog, pointer, recording, and replay state.
