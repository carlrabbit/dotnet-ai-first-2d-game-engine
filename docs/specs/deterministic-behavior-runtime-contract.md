# Deterministic Behavior Runtime Contract

## Authority

Authoritative for compiled behavior registration, scenario activation, lifecycle, context, phase execution, intent emission, and deterministic services.

## Registration and identity

Behavior IDs are stable strings, for example `behavior.player-move-east`. Registration is explicit and code-based. Reflection scanning and runtime compilation are prohibited.

## Context

A behavior receives immutable snapshot/query access, target entity ID, current tick, deterministic random source, and an intent emitter. It must not receive mutable world collections or state setters.

## Activation and lifecycle

Scenario assignments contain stable assignment ID, entity ID, behavior ID, and lifecycle. Supported lifecycles are `once` and `each-tick`. Only one active behavior per entity per phase is allowed.

## Phases

```text
snapshot → behavior execution → intent collection → deterministic ordering → domain resolution → command application → events → assertions
```

All behaviors in a phase read the same snapshot.

## Intent rules

Intents are immutable, have stable IDs, identify assignment and entity, and cannot directly mutate position.

## Random source

Seed comes from scenario source. No global state, wall-clock seed, or `Random.Shared`.

## Diagnostics

`BEHAVIOR0001` unknown behavior; `0002` invalid assignment; `0003` duplicate behavior; `0004` unsupported lifecycle; `0005` missing entity; `0006` unsupported intent; `0007` execution error; `0008` invalid random configuration.

## Determinism

Equivalent scenario, seed, source revision, and state produce equivalent schedules, snapshot-visible data, intents, diagnostics, random values, and events.
## Lifecycle execution

`once` assignments execute only in tick 1. `each-tick` assignments execute once in every behavior phase. Every phase creates a fresh immutable pre-command snapshot; accepted commands affect only later snapshots.
