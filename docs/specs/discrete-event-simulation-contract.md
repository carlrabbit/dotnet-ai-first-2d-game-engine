# Discrete-Event Simulation Contract

## Authority

Authoritative for the optional standalone-capable discrete-event subsystem introduced by M033. M031 remains authoritative for world state, semantic time, commands, factual events, activities, reservations, persistence, and fingerprints.

## Model

```text
scheduled trigger
→ due-time queue
→ guarded delivery
→ shared command
→ authoritative mutation
→ factual domain events
→ follow-up triggers
```

A scheduled trigger is future execution input, not a factual event.

## Queue ordering

```text
due simulation instant
priority class
stable sequence
scheduled-trigger ID
```

Equivalent input produces identical order. No wall-clock, task-order, hash-order, or random tie-breaking.

## Trigger state

```text
scheduled
delivered
completed
stale
cancelled
failed
```

Required fields include stable ID, due instant, ordering fields, owner region/activity/entity, kind, expected revisions, causal references, typed payload, status, and outcome.

## Delivery

Revalidate region fidelity, ownership, lifecycle, revisions, target/source/destination, and reservations. Current triggers issue shared commands. Stale/cancelled triggers perform no success mutation.

## Advancement

Required operations: schedule, inspect, advance-to, advance-by, run-next, cancel/invalidate, save, and load. Clock never moves backwards.

## Safety

Bound maximum events, same-instant events, target instant, and related limits. Safety termination is explicit and artifacted.

## Cancellation

Version guards are primary. Queue removal is optional. Old delivery after revision change becomes stale.

## Persistence

Persist queue order, trigger envelopes/status needed for continuation, sequence, and semantic clock. Restore validates types, references, order, and ownership before commit.

## Standalone host

Runs without graphical assemblies/devices and produces canonical artifacts.

## Diagnostics

```text
DES-QUEUE
DES-ORDER
DES-TRIGGER
DES-STALE
DES-CANCEL
DES-LIMIT
DES-PERSISTENCE
DES-OWNERSHIP
```

## Exclusions

No arbitrary direct domain mutation, event sourcing, multithreaded delivery, distributed queue, wall-clock scheduling, or renderer dependency.
